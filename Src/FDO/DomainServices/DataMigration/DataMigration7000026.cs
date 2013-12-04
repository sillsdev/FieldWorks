// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DataMigration7000026.cs
// Responsibility: Bush
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System.Xml.Linq;
using System.Linq;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Migrate data from 7000025 to 7000026.
	///
	/// FWR-2299 Check for and deal with other occurences of "External Link(s)"
	/// and change them to  LinkedFiles.
	/// </summary>
	///
	/// <remarks>
	/// This migration needs to:
	///
	/// Change the property LangProject.ExtLinkRootDir to LangProject.LinkedFilesRootDir
	///
	/// </remarks>
	/// ----------------------------------------------------------------------------------------
	internal class DataMigration7000026 : IDataMigration
	{
		#region IDataMigration Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Change the property LangProject.ExtLinkRootDir to LangProject.LinkedFilesRootDir
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
			DataMigrationServices.CheckVersionNumber(domainObjectDtoRepository, 7000025);

			// 1. Change the property LangProject.ExtLinkRootDir to LangProject.LinkedFilesRootDir

			var langProjDto = domainObjectDtoRepository.AllInstancesSansSubclasses("LangProject").First();
			var langProjElement = XElement.Parse(langProjDto.Xml);
			var ExtLinkRootDirElement = langProjElement.Element("ExtLinkRootDir");
			if (ExtLinkRootDirElement != null)
				ExtLinkRootDirElement.Name = "LinkedFilesRootDir";
			UpdateDto(domainObjectDtoRepository, langProjDto, langProjElement);


			DataMigrationServices.IncrementVersionNumber(domainObjectDtoRepository);
		}

		/// <summary>
		/// Given that the element has been changed to represent the desired new state of the DTO,
		/// save the change.
		/// </summary>
		private void UpdateDto(IDomainObjectDTORepository domainObjectDtoRepository, DomainObjectDTO dto, XElement element)
		{
			dto.Xml = element.ToString();
			domainObjectDtoRepository.Update(dto);
		}
		#endregion
	}
}
