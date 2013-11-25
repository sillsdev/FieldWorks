// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DataMigration7000031.cs
// Responsibility: FW Team
//
// <remarks>
// </remarks>

using System.Collections.Generic;
using System.Xml.Linq;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Migrate data from 7000030 to 7000031.
	///
	/// FWR-955 Data Migration : for adding index for UserView
	/// </summary>
	///
	/// <remarks>
	/// This migration needs to:
	/// Remove TE views so that they will have to be recreated (in the correct order)
	/// Add index to UserView
	///
	/// </remarks>
	/// ----------------------------------------------------------------------------------------
	internal class DataMigration7000031 : IDataMigration
	{
		#region IDataMigration Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Delete all UserViews
		/// </summary>
		/// <param name="repoDto">
		/// Repository of all CmObject DTOs available for one migration step.
		/// </param>
		/// <remarks>
		/// The method must remove DTOs from the repository.
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
		public void PerformMigration(IDomainObjectDTORepository repoDto)
		{
			DataMigrationServices.CheckVersionNumber(repoDto, 7000030);

			List<DomainObjectDTO> viewsToDelete = new List<DomainObjectDTO>();
			foreach (DomainObjectDTO dtoView in repoDto.AllInstancesSansSubclasses("UserView"))
			{
				XElement xeView = XElement.Parse(dtoView.Xml);
				XElement xeApp = xeView.Element("App");
				if (xeApp == null)
					continue;
				XAttribute val = xeApp.Attribute("val");
				if (val == null)
					continue;
				viewsToDelete.Add(dtoView);
			}
			foreach (var dto in viewsToDelete)
				DataMigrationServices.RemoveIncludingOwnedObjects(repoDto, dto, false);
			viewsToDelete.Clear();

			DataMigrationServices.IncrementVersionNumber(repoDto);
		}
		#endregion
	}
}
