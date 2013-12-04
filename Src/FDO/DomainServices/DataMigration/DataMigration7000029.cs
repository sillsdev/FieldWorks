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
using System;
using System.IO;
using System.Xml.Linq;
using System.Linq;
using System.Xml.XPath;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Migrate data from 7000028 to 7000029.
	///
	/// FWR-2460 Data Migration : for media, pictures, sound files
	/// </summary>
	///
	/// <remarks>
	/// This migration needs to:
	///
	/// Change any CmFile filePaths to relative paths which are actually relative to the LinkedFilesRootDir
	///
	/// </remarks>
	/// ----------------------------------------------------------------------------------------
	internal class DataMigration7000029 : IDataMigration
	{
		#region IDataMigration Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Change any CmFile filePaths to relative paths which are actually relative to the LinkedFilesRootDir
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
			DataMigrationServices.CheckVersionNumber(domainObjectDtoRepository, 7000028);

			var langProjDto = domainObjectDtoRepository.AllInstancesSansSubclasses("LangProject").First();
			var langProjElement = XElement.Parse(langProjDto.Xml);
			var linkedFilesRootDirElement = langProjElement.Element("LinkedFilesRootDir");
			string persistedLinkedFilesRootDir;
			if (linkedFilesRootDirElement == null)
			{
				persistedLinkedFilesRootDir = Path.Combine(domainObjectDtoRepository.ProjectFolder, DirectoryFinder.ksLinkedFilesDir);
			}
			else
			{
				persistedLinkedFilesRootDir = linkedFilesRootDirElement.Value;
			}
			var linkedFilesRootDir = DirectoryFinderRelativePaths.GetLinkedFilesFullPathFromRelativePath(persistedLinkedFilesRootDir,
				domainObjectDtoRepository.ProjectFolder);
			//Get the Elements  for class="CmFile"
			var CmFileDtosBeforeMigration = domainObjectDtoRepository.AllInstancesSansSubclasses("CmFile");

			foreach (var fileDto in CmFileDtosBeforeMigration)
			{
				XElement cmFileXML = XElement.Parse(fileDto.Xml);
				var filePath = cmFileXML.XPathSelectElement("InternalPath").XPathSelectElement("Uni").Value;
				var fileAsRelativePath = DirectoryFinderRelativePaths.GetRelativeLinkedFilesPath(filePath,
																								 linkedFilesRootDir);
				//If these two strings do not match then a full path was converted to a LinkedFiles relative path
				//so replace the path in the CmFile object.
				// (If fileAsRelativePath is null, we could not make sense of the path at all; it may be corrupt.
				// Certainly we can't improve it!)
				if (fileAsRelativePath != null && !String.Equals(fileAsRelativePath, filePath))
				{
					cmFileXML.XPathSelectElement("InternalPath").XPathSelectElement("Uni").Value = fileAsRelativePath;
					UpdateDto(domainObjectDtoRepository, fileDto, cmFileXML);
				}
			}

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
