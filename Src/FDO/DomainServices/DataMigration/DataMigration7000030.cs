// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DataMigration7000030.cs
// Responsibility: MacLean
//
// <remarks>
// </remarks>

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Migrate data from 7000029 to 7000030.
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
	internal class DataMigration7000030 : IDataMigration
	{
		#region IDataMigration Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// FWR-2461 Data Migration: for links to files in TsStrings
		///--turn paths into relative paths to the LinkedFiledRootDir where possible
		///--create a CmFile for each link found in a TsString owned by a CmFolder)
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
			DataMigrationServices.CheckVersionNumber(domainObjectDtoRepository, 7000029);

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

			//-------------------------------------------------
			var langProjectGuid = langProjElement.Attribute("guid").Value;
			var filePathsInTsStringsElement = AddFilePathsInTsStringsElement(langProjElement);
			DomainObjectDTO cmFolderDto;
			var cmFolderXElement = MakeCmFolder(domainObjectDtoRepository, langProjectGuid, filePathsInTsStringsElement, CmFolderTags.LocalFilePathsInTsStrings,
				out cmFolderDto);
			UpdateDto(domainObjectDtoRepository, langProjDto, langProjElement);
			//--------------------------------------------------
			var cmFolderGuid = cmFolderXElement.Attribute("guid").Value;

			var filePathsInTsStrings = ProcessExternalLinksRelativePaths(domainObjectDtoRepository, linkedFilesRootDir);

			foreach (var filePath in filePathsInTsStrings)
			{
				MakeCmFile(domainObjectDtoRepository, cmFolderGuid, cmFolderXElement, filePath);
			}

			//Now that all the CmFile references have been added to the CmFolder update it in the repository.
			UpdateDto(domainObjectDtoRepository, cmFolderDto, cmFolderXElement);

			DataMigrationServices.IncrementVersionNumber(domainObjectDtoRepository);
		}

		// Create a FilePathsInTsStrings element in the LangProject. The objsur will be added
		// when the CmFolder is created.
		//In this migration a typical result (after later code adds the CmFolder) would be:
		//<FilePathsInTsStrings>
		//    <objsur guid="3e615e09-3b86-4fae-adfe-5fc2473214b6" t="o" />
		//</FilePathsInTsStrings>
		private XElement AddFilePathsInTsStringsElement(XElement langProject)
		{
			var newElement = new XElement("FilePathsInTsStrings");
			langProject.Add(newElement);
			return newElement;
		}

		// Creates a CmFolder whose owning guid is the langProjectGuid passed as a child of the element passed
		// and gives it the name passed. In this migration a typical result (after later code adds the Files) would be:
		// <rt guid="3e615e09-3b86-4fae-adfe-5fc2473214b6" class="CmFolder" ownerguid="b8bdad3d-9006-46f0-83e8-ae1d1726f2ad">
		//    <Name>
		//        <AUni ws="pt">File paths in TsStrings</AUni>
		//    </Name>
		//    <Files>
		//        <objsur guid="c4f3760e-049e-49af-ac57-73c686100700" t="o" />
		//        <objsur guid="c4f4760e-049e-49af-ac57-73c686100700" t="o" />
		//    </Files>
		//</rt>
		private XElement MakeCmFolder(IDomainObjectDTORepository domainObjectDtoRepository, string langProjectGuid,
			XElement filePathsInTsStringsCmFolder, string name, out DomainObjectDTO folderDto)
		{
			string cmFolderGuid;
			cmFolderGuid = Guid.NewGuid().ToString();
			var cmFolderXML = new XElement("rt",
										 new XAttribute("guid", cmFolderGuid),
										 new XAttribute("class", "CmFolder"),
										 new XAttribute("ownerguid", langProjectGuid),
										 MakeMultiUnicode("Name", name),
										 new XElement("Files"));
			folderDto = new DomainObjectDTO(cmFolderGuid, "CmFolder", cmFolderXML.ToString());
			domainObjectDtoRepository.Add(folderDto);
			filePathsInTsStringsCmFolder.Add(MakeOwningSurrogate(cmFolderGuid));
			return cmFolderXML;
		}


		// Make a new CmFile element for the specified path, and an objsur child of langCmFolder to own it.
		// In this migration a typical result would be:
		//    <rt guid="c4f4760e-049e-49af-ac57-73c686100700" class="CmFile" ownerguid="3e615e09-3b86-4fae-adfe-5fc2473214b6">
		//    <InternalPath>
		//        <Uni>C:\FwWW\DistFiles\AudioVisual\NotInLinkedFilesPath.WMV</Uni>
		//    </InternalPath>
		//    </rt>
		private void MakeCmFile(IDomainObjectDTORepository domainObjectDtoRepository, string langCmFolderGuid,
			XElement langCmFolder, string path)
		{
			string cmFileGuid;
			cmFileGuid = Guid.NewGuid().ToString();
			var cmFileXElement = new XElement("rt",
										 new XAttribute("guid", cmFileGuid),
										 new XAttribute("class", "CmFile"),
										 new XAttribute("ownerguid", langCmFolderGuid),
										 MakeUnicode("InternalPath", path));
			var dtoConfirmed = new DomainObjectDTO(cmFileGuid, "CmFile", cmFileXElement.ToString());
			domainObjectDtoRepository.Add(dtoConfirmed);
			langCmFolder.Element("Files").Add(MakeOwningSurrogate(cmFileGuid));
		}


		/// <summary>
		/// Make an XElement that represents a single unicode attribute with the specified name,
		/// and a single English alternative.
		/// </summary>
		private XElement MakeUnicode(string name, string value)
		{
			return new XElement(name, new XElement("Uni", new XAttribute("ws", "en"), new XText(value)));
		}


		/// <summary>
		/// Make an XElement that represents a single multiunicode attribute with the specified name,
		/// and a single English alternative.
		/// </summary>
		private XElement MakeMultiUnicode(string name, string value)
		{
			return new XElement(name, new XElement("AUni", new XAttribute("ws", "en"), new XText(value)));
		}

		private XElement MakeOwningSurrogate(string confirmedGuid)
		{
			return new XElement("objsur", new XAttribute("guid", confirmedGuid), new XAttribute("t", "o"));
		}

		private static readonly byte[] externalLinkTag = Encoding.UTF8.GetBytes("externalLink");

		private List<string> ProcessExternalLinksRelativePaths(IDomainObjectDTORepository dtoRepos, String linkedFilesRootDir)
		{
			var filePathsInTsStrings = new List<string>();
			foreach (var dto in dtoRepos.AllInstancesWithValidClasses())  //Get the Elements for all CmObjects
			{
				if (dto.XmlBytes.IndexOfSubArray(externalLinkTag) < 0)
					continue;
				var dtoXML = XElement.Parse(dto.Xml);
				foreach (var runXMLElement in dtoXML.XPathSelectElements("//Run"))
				{
					var externalLinkAttributeForThisRun = runXMLElement.Attribute("externalLink");
					if (externalLinkAttributeForThisRun != null)
					{
						try
						{

						//Convert the file paths which should be stored as relative paths
						if (Path.IsPathRooted(FileUtils.ChangeWindowsPathIfLinux(externalLinkAttributeForThisRun.Value)))
						{
							var filePath = FileUtils.ChangeWindowsPathIfLinux(externalLinkAttributeForThisRun.Value);
							//Check the path and if it is a rooted path which is relative to the LinkedFilesRootDir
							//then we will have to confirm that is was changed to a relative path after the migration.
							var fileAsRelativePath = DirectoryFinderRelativePaths.GetRelativeLinkedFilesPath(filePath,
																											 linkedFilesRootDir);
							//Save the file paths so they can be turned into CmFiles
							filePathsInTsStrings.Add(fileAsRelativePath);
							//If these two strings do not match then a full path was converted to a LinkedFiles relative path
							//so replace the path in the CmFile object.
							if (!String.Equals(fileAsRelativePath, filePath))
							{
								runXMLElement.Attribute("externalLink").Value = fileAsRelativePath;
								UpdateDto(dtoRepos, dto, dtoXML);
							}
						}
						else
						{
							//if the file path was already relative we want to save it and turn it into a CmFiles
							if (FileUtils.IsFileUriOrPath(externalLinkAttributeForThisRun.Value) && FileUtils.IsFilePathValid(externalLinkAttributeForThisRun.Value))
								filePathsInTsStrings.Add(externalLinkAttributeForThisRun.Value);
						}
						}
						catch (ArgumentException)
						{
							Logger.WriteEvent("Invalid path for external link - no CmFile created: " + externalLinkAttributeForThisRun);
						}
					}
				}
			}
			return filePathsInTsStrings;
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
