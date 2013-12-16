// Copyright (c) 2011-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DataMigration7000036.cs
// Responsibility: FW team

using System.Xml.Linq;
using System;
using SIL.FieldWorks.FDO.DomainImpl;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Migrate data from 7000035 to 7000036.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DataMigration7000036 : IDataMigration
	{
		#region IDataMigration Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add DateModified to StText
		/// </summary>
		/// <param name="domainObjectDtoRepository">
		/// Repository of all CmObject DTOs available for one migration step.
		/// </param>
		/// ------------------------------------------------------------------------------------
		public void PerformMigration(IDomainObjectDTORepository domainObjectDtoRepository)
		{
			DataMigrationServices.CheckVersionNumber(domainObjectDtoRepository, 7000035);

			foreach (var stTextDTO in domainObjectDtoRepository.AllInstancesWithSubclasses("StText"))
			{
				XElement stText = XElement.Parse(stTextDTO.Xml);
				if (stText.Element("DateModified") != null)
					continue; // Already has a DateModified property (probably an StJounalText

				XElement dateModified = new XElement("DateModified", null);
				XAttribute value = new XAttribute("val", ReadWriteServices.FormatDateTime(DateTime.Now));
				dateModified.Add(value);
				stText.Add(dateModified);
				DataMigrationServices.UpdateDTO(domainObjectDtoRepository, stTextDTO, stText.ToString());
			}

			DataMigrationServices.IncrementVersionNumber(domainObjectDtoRepository);
		}

		#endregion
	}
}
