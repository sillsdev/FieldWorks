// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using NUnit.Framework;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test framework for migration from version 7000011 to 7000012.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public sealed class DataMigrationTests7000012 : DataMigrationTestsBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000011 to 7000012.
		///
		/// This test makes sure the unused fields in UserViewField are deleted but leaves the
		/// remaining fields.
		///
		/// All the fields are:
		///		Label
		///		HelpString
		///		Type
		///		Flid
		///		Visibility
		///		Required
		///		Style
		///		SubfieldOf
		///		Details
		///		IsCustomField
		///		PossList
		///		WritingSystem
		///		WsSelector
		///
		/// The ones to be deleted are:
		///		Details
		///		Visibility
		///		SubfieldOf
		///		IsCustomField
		///		PossList
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000012_Unneeded_UserViewField_Removed_Test()
		{
			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000012_UserViewField.xml");

			// Set up mock MDC.
			var mockMDC = new MockMDCForDataMigration();
			mockMDC.AddClass(1, "CmObject", null, new List<string> { "UserViewField" });
			mockMDC.AddClass(2, "UserViewField", "CmObject", new List<string>());
			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000011, dtos, mockMDC, null, FwDirectoryFinder.FdoDirectories);

			Assert.AreEqual(40, dtoRepos.AllInstancesWithSubclasses("UserViewField").Count());

			// Collect the UserViewField values.
			var userViewFieldDtos = new Dictionary<string, DomainObjectDTO>();
			foreach (var uvfDto in dtoRepos.AllInstancesWithSubclasses("UserViewField"))
				userViewFieldDtos.Add(uvfDto.Guid.ToUpper(), uvfDto);

			m_dataMigrationManager.PerformMigration(dtoRepos, 7000012, new DummyProgressDlg());

			// Counts for fields that should not have been removed.
			int countRequired = 0;
			int countStyle = 0;
			int countHelpString = 0;
			int countFlid = 0;
			int countType = 0;

			foreach (var userViewField in dtoRepos.AllInstancesSansSubclasses("UserViewField"))
			{
				var uvfElement = XElement.Parse(userViewField.Xml);

				// Confirm that needed fields are still present.
				// Increment counter for fields occurring in UserViewField (but some may not always be present).
				if (uvfElement.XPathSelectElement("UserViewField/Required") != null)
					countRequired++;
				if (uvfElement.XPathSelectElement("UserViewField/Style") != null)
					countStyle++;
				if (uvfElement.XPathSelectElement("UserViewField/HelpString") != null)
					countHelpString++;
				if (uvfElement.XPathSelectElement("UserViewField/Flid") != null)
					countFlid++;
				if (uvfElement.XPathSelectElement("UserViewField/Type") != null)
					countType++;

				// Try to get the unused elements in UserViewField (should be gone)
				Assert.IsNull(uvfElement.XPathSelectElement("UserViewField/Details"));
				Assert.IsNull(uvfElement.XPathSelectElement("UserViewField/Visibility"));
				Assert.IsNull(uvfElement.XPathSelectElement("UserViewField/SubfieldOf"));
				Assert.IsNull(uvfElement.XPathSelectElement("UserViewField/IsCustomField"));
			}

			// Expectations for occurrences of fields that should not have been removed from XML file
			Assert.AreEqual(1, countStyle, "Unexpected number of Style fields");
			Assert.AreEqual(3, countRequired, "Unexpected number of Required fields");
			Assert.AreEqual(36, countHelpString, "Unexpected number of HelpString fields");
			Assert.AreEqual(40, countFlid, "Unexpected number of Flid fields");
			Assert.AreEqual(40, countType, "Unexpected number of Type fields");
		}
	}
}
