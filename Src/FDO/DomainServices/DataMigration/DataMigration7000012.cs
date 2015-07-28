// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Migrates from 7000011 to 7000012
	/// </summary>
	/// ------------------------------------------------------------------------------------
	internal class DataMigration7000012 : IDataMigration
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deletes unused fields in UserViewField.
		/// </summary>
		/// <param name="domainObjectDtoRepository">Repository of all CmObject DTOs available for
		/// one migration step.</param>
		/// ------------------------------------------------------------------------------------
		public void PerformMigration(IDomainObjectDTORepository domainObjectDtoRepository)
		{
			DataMigrationServices.CheckVersionNumber(domainObjectDtoRepository, 7000011);

			// 1) Select the UserViewField classes.
			// 2) Delete the following attributes: Details, Visibility, IsCustomField, SubfieldOf, PossList
			foreach (var uvfDto in domainObjectDtoRepository.AllInstancesSansSubclasses("UserViewField"))
			{
				XElement rtElement = XElement.Parse(uvfDto.Xml);
				XElement uvfElement = rtElement.Element("UserViewField");
				RemoveField(uvfDto, uvfElement, "Details");
				RemoveField(uvfDto, uvfElement, "Visibility");
				RemoveField(uvfDto, uvfElement, "SubfieldOf");
				RemoveField(uvfDto, uvfElement, "IsCustomField");
				DataMigrationServices.UpdateDTO(domainObjectDtoRepository, uvfDto, rtElement.ToString());
			}

			DataMigrationServices.IncrementVersionNumber(domainObjectDtoRepository);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes the specified field.
		/// </summary>
		/// <param name="dto">The domain transfer object that has a field that may need to be deleted.</param>
		/// <param name="objElement">Name of the object containing fieldToDelete.</param>
		/// <param name="fieldToDelete">The name of the field to delete.</param>
		/// ------------------------------------------------------------------------------------
		private void RemoveField(DomainObjectDTO dto, XElement objElement, string fieldToDelete)
		{
			XElement rmElement = objElement.Element(fieldToDelete);
			if (rmElement != null)
				rmElement.Remove();
		}
	}
}
