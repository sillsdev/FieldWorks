// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using System.Xml.Linq;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Migrate data from 7000050 to 7000051.
	///
	/// Change the LangProject Guid to really be unique between different projects,
	/// so Lift Bridge is happy.
	/// </summary>
	/// <remarks>
	/// Actually, this DM will try to delete the old one and
	/// </remarks>
	/// ----------------------------------------------------------------------------------------
	internal class DataMigration7000051 : IDataMigration
	{
		#region IDataMigration Members

		public void PerformMigration(IDomainObjectDTORepository domainObjectDtoRepository)
		{
			DataMigrationServices.CheckVersionNumber(domainObjectDtoRepository, 7000050);

			var newGuidValue = Guid.NewGuid().ToString().ToLowerInvariant();
			const string className = "LangProject";
			var lpDto = domainObjectDtoRepository.AllInstancesSansSubclasses(className).First();
			var ownedDtos = domainObjectDtoRepository.GetDirectlyOwnedDTOs(lpDto.Guid).ToList();
			var data = lpDto.Xml;
			domainObjectDtoRepository.Remove(lpDto); // It is pretty hard to change an immutable Guid identifier in BEP-land, so nuke it, and make a new one.

			var lpElement = XElement.Parse(data);
			lpElement.Attribute("guid").Value = newGuidValue;
			var newLpDto = new DomainObjectDTO(newGuidValue, className, lpElement.ToString());
			domainObjectDtoRepository.Add(newLpDto);

			// Change ownerguid attr for each owned item to new guid.
			foreach (var ownedDto in ownedDtos)
			{
				var ownedElement = XElement.Parse(ownedDto.Xml);
				ownedElement.Attribute("ownerguid").Value = newGuidValue;
				ownedDto.Xml = ownedElement.ToString();
				domainObjectDtoRepository.Update(ownedDto);
			}

			DataMigrationServices.IncrementVersionNumber(domainObjectDtoRepository);
		}
		#endregion
	}
}
