using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// <summary>
	/// Services for data migration tests.
	/// </summary>
	internal static class DataMigrationTestServices
	{
		internal static void CheckDtoRemoved(IDomainObjectDTORepository dtoRepos, DomainObjectDTO goner)
		{
			DomainObjectDTO dto;
			if (dtoRepos.TryGetValue(goner.Guid, out dto))
			{
				Assert.Fail("Still has deleted (or zombie) DTO.");
			}
			Assert.IsTrue(((DomainObjectDtoRepository)dtoRepos).Goners.Contains(goner));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Parses a project file and generates a set of DTO objects contained in the project.
		/// It looks in the FDO\FDOTests\TestData directory for the specified project file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal static HashSet<DomainObjectDTO> ParseProjectFile(string filename)
		{
			var testDataPath = Path.Combine(DirectoryFinder.FwSourceDirectory, "FDO/FDOTests/TestData");
			var lpElement = XElement.Load(Path.Combine(testDataPath, filename));
			return new HashSet<DomainObjectDTO>(
				from elem in lpElement.Elements("rt")
				select new DomainObjectDTO(elem.Attribute("guid").Value, elem.Attribute("class").Value,
										   elem.ToString()));
		}
	}
}
