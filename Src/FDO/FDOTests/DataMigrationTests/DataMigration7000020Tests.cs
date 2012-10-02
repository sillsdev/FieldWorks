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
// File: DataMigration7000020Tests.cs
// Responsibility: mcconnel
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System.Collections.Generic;
using System.IO;
using System.Linq;

using NUnit.Framework;

using SIL.FieldWorks.FDO.DomainServices.DataMigration;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test framework for migration from version 7000019 to 7000020.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public sealed class DataMigrationTests7000020 : DataMigrationTestsBase
	{
		/// <summary>
		/// Test the migration from version 7000019 to 7000020.
		/// </summary>
		[Test]
		public void DataMigration7000020Test()
		{
			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000020.xml");

			IFwMetaDataCacheManaged mockMdc = SetupMdc();

			IDomainObjectDTORepository repoDto = new DomainObjectDtoRepository(7000019, dtos, mockMdc,
				Path.GetTempPath());

			// Initial check that data was read properly.
			var cObjects = repoDto.AllInstances().Count();
			Assert.AreEqual(378, cObjects, "Before migrating, should be 378 objects");
			var cLangProject = repoDto.AllInstancesSansSubclasses("LangProject").Count();
			Assert.AreEqual(1, cLangProject, "Before migrating, should be 1 LangProject object");
			var cUserView = repoDto.AllInstancesSansSubclasses("UserView").Count();
			Assert.AreEqual(17, cUserView, "Before migrating, should be 17 UserView objects");
			var cUserViewRec = repoDto.AllInstancesSansSubclasses("UserViewRec").Count();
			Assert.AreEqual(31, cUserViewRec, "Before migrating, should be 31 UserViewRec objects");
			var cUserViewField = repoDto.AllInstancesSansSubclasses("UserViewField").Count();
			Assert.AreEqual(329, cUserViewField, "Before migrating, should be 329 UserViewField objects");

			// Do the migration.
			m_dataMigrationManager.PerformMigration(repoDto, 7000020, new DummyProgressDlg());

			// Verification Phase
			Assert.AreEqual(7000020, repoDto.CurrentModelVersion, "Wrong updated version.");
			cObjects = repoDto.AllInstances().Count();
			Assert.AreEqual(31, cObjects, "After migrating, should be 31 objects");
			cLangProject = repoDto.AllInstancesSansSubclasses("LangProject").Count();
			Assert.AreEqual(1, cLangProject, "After migrating, should be 1 LangProject object");
			cUserView = repoDto.AllInstancesSansSubclasses("UserView").Count();
			Assert.AreEqual(12, cUserView, "After migrating, should be 12 UserView objects");
			cUserViewRec = repoDto.AllInstancesSansSubclasses("UserViewRec").Count();
			Assert.AreEqual(3, cUserViewRec, "After migrating, should be 3 UserViewRec objects");
			cUserViewField = repoDto.AllInstancesSansSubclasses("UserViewField").Count();
			Assert.AreEqual(15, cUserViewField, "After migrating, should be 15 UserViewField objects");
		}

		internal static MockMDCForDataMigration SetupMdc()
		{
			var mockMdc = new MockMDCForDataMigration();
			mockMdc.AddClass(0, "CmObject", null, new List<string> { "CmProject", "UserView", "UserViewRec", "UserViewField" });
			mockMdc.AddClass(1, "CmProject", "CmObject", new List<string> { "LangProject" });
			mockMdc.AddClass(6001, "LangProject", "CmProject", new List<string>());
			mockMdc.AddClass(18, "UserView", "CmObject", new List<string>());
			mockMdc.AddClass(19, "UserViewRec", "CmObject", new List<string>());
			mockMdc.AddClass(20, "UserViewField", "CmObject", new List<string>());
			return mockMdc;
		}
	}
}
