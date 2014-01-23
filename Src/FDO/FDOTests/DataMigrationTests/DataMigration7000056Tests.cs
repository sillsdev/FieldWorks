// Copyright (c) 2011-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DataMigrationTests7000056.cs
// Responsibility: FW team

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// <summary>
	/// Test framework for migration from version 7000055 to 7000056.
	/// </summary>
	[TestFixture]
	public sealed class DataMigrationTests7000056 : DataMigrationTestsBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000051 to 7000052.
		/// Copy MorphRA forms into WfiMorphBundle forms
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000056Test()
		{
			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000056.xml");
			// Set up mock MDC.
			var mockMDC = new MockMDCForDataMigration();
			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000055, dtos, mockMDC, null, FwDirectoryFinder.FdoDirectories);

			m_dataMigrationManager.PerformMigration(dtoRepos, 7000056, new DummyProgressDlg());
			Assert.AreEqual(7000056, dtoRepos.CurrentModelVersion, "Wrong updated version.");

			// check that PhPhonData has the PhonRuleFeats possibility list
			{
				var dtosList = dtoRepos.AllInstancesSansSubclasses("PhPhonData");
				DomainObjectDTO dtoPhPhonDataTest = dtosList.First();
				CheckPhPhonData(dtoPhPhonDataTest, dtoRepos);
			}

			// In the extremely unlikely event that there is no PhPhonData yet, check that we add it
			{
				dtos = new HashSet<DomainObjectDTO>();

				var sb = new StringBuilder();
				// Add WfiMorphBundle that already has a form.
				const string sGuid_wmbLangProj = "00b35f9f-86ce-4f07-bde7-b65c28503641";

				sb.AppendFormat("<rt class=\"LangProj\" guid=\"{0}\">", sGuid_wmbLangProj);
				sb.Append("</rt>");
				var dtoLangProj = new DomainObjectDTO(sGuid_wmbLangProj, "LangProj", sb.ToString());
				dtos.Add(dtoLangProj);
				sb.Length = 0;

				mockMDC = new MockMDCForDataMigration();
				dtoRepos = new DomainObjectDtoRepository(7000055, dtos, mockMDC, null, FwDirectoryFinder.FdoDirectories);
				m_dataMigrationManager.PerformMigration(dtoRepos, 7000056, new DummyProgressDlg());
				Assert.AreEqual(7000056, dtoRepos.CurrentModelVersion, "Wrong updated version.");

				var dtosList = dtoRepos.AllInstancesSansSubclasses("LangProj");
				DomainObjectDTO dtoLangProjTest = dtosList.First();
				var eltWmbLangProjTest = XElement.Parse(dtoLangProjTest.Xml);
				// get phon rule feats
				var eltPhonologicalDataTest = eltWmbLangProjTest.Element("PhonologicalData");
				Assert.IsNotNull(eltPhonologicalDataTest);
				var eltObjsurTest = eltPhonologicalDataTest.Element("objsur");
				Assert.IsNotNull(eltObjsurTest);
				// get possibility list itself
				var guidPhPhonDataTest = eltObjsurTest.Attribute("guid").Value;
				Assert.IsNotNull(guidPhPhonDataTest);
				DomainObjectDTO dtoPhPhonDataTest;
				dtoRepos.TryGetValue(guidPhPhonDataTest, out dtoPhPhonDataTest);
				Assert.IsNotNull(dtoPhPhonDataTest);
				CheckPhPhonData(dtoPhPhonDataTest, dtoRepos);

			}
		}

		private static void CheckPhPhonData(DomainObjectDTO dtoPhPhonDataTest, IDomainObjectDTORepository dtoRepos)
		{
			var eltWmbPhPhonDataTest = XElement.Parse(dtoPhPhonDataTest.Xml);
			// get phon rule feats
			var eltPhonRuleFeatsTest = eltWmbPhPhonDataTest.Element("PhonRuleFeats");
			Assert.IsNotNull(eltPhonRuleFeatsTest);
			var eltObjsurTest = eltPhonRuleFeatsTest.Element("objsur");
			Assert.IsNotNull(eltObjsurTest);
			// get possibility list itself
			var guidPossibilityListTest = eltObjsurTest.Attribute("guid").Value;
			Assert.IsNotNull(guidPossibilityListTest);
			DomainObjectDTO dtoCmPossiblityTest;
			dtoRepos.TryGetValue(guidPossibilityListTest, out dtoCmPossiblityTest);
			Assert.IsNotNull(dtoCmPossiblityTest);
			var eltWmbCmPossibilityListTest = XElement.Parse(dtoCmPossiblityTest.Xml);
			Assert.IsNotNull(eltWmbCmPossibilityListTest);
			var attrCmPossiblityListClassTest = eltWmbCmPossibilityListTest.Attribute("class").Value;
			Assert.AreEqual("CmPossibilityList", attrCmPossiblityListClassTest);
			var attrCmPossiblityListOwnerGuidTest = eltWmbCmPossibilityListTest.Attribute("ownerguid").Value;
			Assert.AreEqual(dtoPhPhonDataTest.Guid, attrCmPossiblityListOwnerGuidTest);
		}
	}
}