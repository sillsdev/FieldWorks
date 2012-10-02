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
// File: DataMigration7000027.cs
// Responsibility: FW team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System.Linq;
using System.Xml.Linq;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Migrate data from 7000027 to 7000028.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DataMigration7000028 : IDataMigration
	{
		#region IDataMigration Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes LexEntries be unowned.
		/// </summary>
		/// <param name="domainObjectDtoRepository">
		/// Repository of all CmObject DTOs available for one migration step.
		/// </param>
		/// <remarks>
		/// The method must add/remove/update the DTOs to the repository,
		/// as it adds/removes objects as part of it work.
		///
		/// Implementors of this interface should ensure the Repository's
		/// starting model version number is correct for the step.
		/// Implementors must also increment the Repository's model version number
		/// at the end of its migration work.
		///
		/// The method also should normally modify the xml string(s)
		/// of relevant DTOs, since that string will be used by the main
		/// data migration calling client (ie. BEP).
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public void PerformMigration(IDomainObjectDTORepository domainObjectDtoRepository)
		{
			DataMigrationServices.CheckVersionNumber(domainObjectDtoRepository, 7000027);

			var lexDbDto = domainObjectDtoRepository.AllInstancesSansSubclasses("LexDb").First();
			var lexDb = XElement.Parse(lexDbDto.Xml);
			var entries = lexDb.Element("Entries");
			if (entries != null)
			{
				entries.Remove();
				DataMigrationServices.UpdateDTO(domainObjectDtoRepository, lexDbDto, lexDb.ToString());
			}

			foreach (var entryDto in domainObjectDtoRepository.AllInstancesSansSubclasses("LexEntry"))
			{
				XElement entry = XElement.Parse(entryDto.Xml);
				var ownerAttr = entry.Attribute("ownerguid");
				if (ownerAttr != null)
				{
					ownerAttr.Remove();
					DataMigrationServices.UpdateDTO(domainObjectDtoRepository, entryDto, entry.ToString());
				}
			}

			DataMigrationServices.IncrementVersionNumber(domainObjectDtoRepository);
		}

		#endregion
	}
}
