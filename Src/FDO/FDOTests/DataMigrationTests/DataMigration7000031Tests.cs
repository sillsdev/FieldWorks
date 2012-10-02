using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;
using SIL.FieldWorks.FDO.Infrastructure;
using System.IO;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// <summary>
	/// Test framework for migration from version 7000030 to 7000031.
	/// </summary>
	[TestFixture]
	public sealed class DataMigrationTests7000031 : DataMigrationTestsBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000030 to 7000031.
		/// Remove all UserViews
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000031Test()
		{
			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000031Tests.xml");

			IFwMetaDataCacheManaged mockMdc = DataMigrationTests7000020.SetupMdc();

			IDomainObjectDTORepository repoDto = new DomainObjectDtoRepository(7000030, dtos, mockMdc,
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
			m_dataMigrationManager.PerformMigration(repoDto, 7000031);

			// Verification Phase
			Assert.AreEqual(7000031, repoDto.CurrentModelVersion, "Wrong updated version.");
			cObjects = repoDto.AllInstances().Count();
			Assert.AreEqual(1, cObjects, "After migrating, should be 1 object");
			cLangProject = repoDto.AllInstancesSansSubclasses("LangProject").Count();
			Assert.AreEqual(1, cLangProject, "After migrating, should be 1 LangProject object");
			cUserView = repoDto.AllInstancesSansSubclasses("UserView").Count();
			Assert.AreEqual(0, cUserView, "After migrating, should be 12 UserView objects");
			cUserViewRec = repoDto.AllInstancesSansSubclasses("UserViewRec").Count();
			Assert.AreEqual(0, cUserViewRec, "After migrating, should be 3 UserViewRec objects");
			cUserViewField = repoDto.AllInstancesSansSubclasses("UserViewField").Count();
			Assert.AreEqual(0, cUserViewField, "After migrating, should be 15 UserViewField objects");
		}
	}
}