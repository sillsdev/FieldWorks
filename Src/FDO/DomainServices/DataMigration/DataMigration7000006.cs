// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DataMigration7000006.cs
// Responsibility: Bush
//
// <remarks>
// </remarks>

using System.Xml.Linq;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Migrate data from 7000005 to 7000006.
	///
	/// Change model for Data Notebook to combine RnEvent and RnAnalysis into RnGenericRec
	/// </summary>
	///
	/// <remarks>
	/// This migration needs to:
	///		1. Select the CmProject classes.
	///		2. Delete the 'Name' attribute.
	/// </remarks>
	/// ----------------------------------------------------------------------------------------
	internal class DataMigration7000006 : IDataMigration
	{
		#region IDataMigration Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Change model to remove the Name field from CmProject
		/// </summary>
		/// <param name="domainObjectDtoRepository">
		/// Repository of all CmObject DTOs available for one migration step.
		/// </param>
		/// <remarks>
		/// The method must add/remove/update the DTOs to the repository,
		/// as it adds/removes objects as part of its work.
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
			DataMigrationServices.CheckVersionNumber(domainObjectDtoRepository, 7000005);

			// 1. Select the CmProject classes.
			// 2. Delete the 'Name' attribute.
			foreach (var cmProjDto in domainObjectDtoRepository.AllInstancesSansSubclasses("LangProject"))
			{
				var rtElement = XElement.Parse(cmProjDto.Xml);
				var ProjElement = rtElement.Element("CmProject");
				var nmElement = ProjElement.Element("Name");
				if (nmElement != null)
					nmElement.Remove();

				DataMigrationServices.UpdateDTO(domainObjectDtoRepository, cmProjDto, rtElement.ToString());
			}
			DataMigrationServices.IncrementVersionNumber(domainObjectDtoRepository);
		}
		#endregion
	}
}
