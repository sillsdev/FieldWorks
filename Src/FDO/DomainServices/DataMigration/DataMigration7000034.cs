// Copyright (c) 2011-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DataMigration7000034.cs
// Responsibility: FW Team

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Migrate data from 7000033 to 7000034.
	///
	/// FWR-3210 Crash when trying to access PicturesOC for converted FW 6.0 project - contains
	///			 CmFile objects
	/// </summary>
	///
	/// <remarks>
	/// This migration fixes duplicate CmFile objects that were created by the FW 6.0 TE code
	/// and put into the LangProject.PicturesOC collection which should only contain CmFolders.
	///
	/// </remarks>
	/// ----------------------------------------------------------------------------------------
	internal class DataMigration7000034 : IDataMigration
	{
		#region IDataMigration Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Looks for CmFile objects included in LangProject. If found, will look for CmFile in
		/// correct location and replace reference on CmPicture with that new CmFile.
		/// </summary>
		/// <param name="repoDto">
		/// Repository of all CmObject DTOs available for one migration step.
		/// </param>
		/// <remarks>
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
			DataMigrationServices.CheckVersionNumber(repoDto, 7000033);
			var langProj = repoDto.AllInstancesSansSubclasses("LangProject").First();
			var langProjElement = XElement.Parse(langProj.Xml);
			var pictures = langProjElement.Element("Pictures");

			if (pictures != null && pictures.Elements().Count() > 1)
			{
				DomainObjectDTO folder = null;
				bool foundFiles = false;
				foreach (var x in pictures.Elements())
				{
					var xObj = repoDto.GetDTO(x.Attribute("guid").Value);
					if (xObj.Classname == "CmFolder")
					{
						// empty folders can just be removed
						var xObjElement = XElement.Parse(xObj.Xml);
						if (xObjElement.Element("Files") == null)
							DataMigrationServices.RemoveIncludingOwnedObjects(repoDto, xObj, true);
						else if (folder == null)
							folder = xObj;
						else
							MoveFileReferences(repoDto, langProj, xObj, folder);
					}
					else
						foundFiles = true;
				}

				if (folder != null && foundFiles)
					RemoveInvalidFiles(repoDto, langProj, folder);

			}
			DataMigrationServices.IncrementVersionNumber(repoDto);
		}

		private void MoveFileReferences(IDomainObjectDTORepository repoDto, DomainObjectDTO langProj,
			DomainObjectDTO srcFolder, DomainObjectDTO destFolder)
		{
			var srcFolderElement = XElement.Parse(srcFolder.Xml);
			var destFolderElement = XElement.Parse(destFolder.Xml);
			var destFileRefs = destFolderElement.Element("Files");
			foreach (var fileRef in srcFolderElement.Element("Files").Elements())
			{
				destFileRefs.Add(fileRef);
				var guid = fileRef.Attribute("guid").Value;
				var file = repoDto.GetDTO(guid);
				var fileElement = XElement.Parse(file.Xml);
				fileElement.Attribute("ownerguid").SetValue(destFolder.Guid);
				DataMigrationServices.UpdateDTO(repoDto, file, fileElement.ToString());
			}

			RemoveReferenceFromPictures(repoDto, langProj, srcFolder.Guid);
			repoDto.Remove(srcFolder);
			DataMigrationServices.UpdateDTO(repoDto, destFolder, destFolderElement.ToString());
		}

		private void RemoveReferenceFromPictures(IDomainObjectDTORepository repoDto, DomainObjectDTO langProj,
			string guid)
		{
			var langProjElement = XElement.Parse(langProj.Xml);
			foreach (var x in langProjElement.Element("Pictures").Elements())
			{
				if (x.Attribute("guid").Value == guid)
				{
					x.Remove();
					break;
				}
			}
			DataMigrationServices.UpdateDTO(repoDto, langProj, langProjElement.ToString());
		}

		private void RemoveInvalidFiles(IDomainObjectDTORepository repoDto,
			DomainObjectDTO langProj, DomainObjectDTO folder)
		{
			var langProjElement = XElement.Parse(langProj.Xml);
			var pictures = langProjElement.Element("Pictures");
			var fileMap = CreateFilePathToGuidMap(repoDto, folder);
			var pictureMap = CreateFileGuidToPictureMap(repoDto);
			foreach (var x in pictures.Elements())
			{
				var xObj = repoDto.GetDTO(x.Attribute("guid").Value);
				if (xObj.Classname == "CmFile")
				{
					string replacementFileGuid;
					string filePath = GetFilePath(xObj);
					if (filePath != null &&
						fileMap.TryGetValue(filePath.ToLowerInvariant(), out replacementFileGuid))
					{
						UpdatePictureReferences(repoDto, xObj, replacementFileGuid, pictureMap);
						DataMigrationServices.RemoveIncludingOwnedObjects(repoDto, xObj, true);
					}
					else if (!pictureMap.ContainsKey(xObj.Guid))
						DataMigrationServices.RemoveIncludingOwnedObjects(repoDto, xObj, true);
					else
					{
						MoveFileToFolder(repoDto, folder, xObj);
						RemoveReferenceFromPictures(repoDto, langProj, xObj.Guid);
					}
				}
			}
		}

		private void MoveFileToFolder(IDomainObjectDTORepository repoDto, DomainObjectDTO folder, DomainObjectDTO fileToMove)
		{
			// Create surogate for file and add it to the folder
			var surrogate = DataMigrationServices.CreateOwningObjSurElement(fileToMove.Guid);
			var folderElement = XElement.Parse(folder.Xml);
			var filesElement = folderElement.Element("Files");
			filesElement.Add(surrogate);
			DataMigrationServices.UpdateDTO(repoDto, folder, folderElement.ToString());

			// Change owner of file
			var fileElement = XElement.Parse(fileToMove.Xml);
			fileElement.Attribute("ownerguid").SetValue(folder.Guid);
			DataMigrationServices.UpdateDTO(repoDto, fileToMove, fileElement.ToString());
		}

		private void UpdatePictureReferences(IDomainObjectDTORepository repoDto, DomainObjectDTO file,
			string replacementFileGuid,
			Dictionary<string, List<DomainObjectDTO>> pictureMap)
		{
			List<DomainObjectDTO> pictures;
			if(pictureMap.TryGetValue(file.Guid, out pictures))
			{
				foreach (var picture in pictures)
				{
					var pictureElement = XElement.Parse(picture.Xml);
					var objSurrogateElement = pictureElement.Element("PictureFile").Element("objsur");
					objSurrogateElement.Attribute("guid").Value = replacementFileGuid;
					DataMigrationServices.UpdateDTO(repoDto, picture, pictureElement.ToString());
				}
			}
		}

		private Dictionary<string, List<DomainObjectDTO>> CreateFileGuidToPictureMap(IDomainObjectDTORepository repoDto)
		{
			var map = new Dictionary<string, List<DomainObjectDTO>>();
			foreach (var picture in repoDto.AllInstancesSansSubclasses("CmPicture"))
			{
				// all TE pictures are unowned, so no need to look at those with owners
				if (repoDto.GetOwningDTO(picture) == null)
				{
					var pictureElement = XElement.Parse(picture.Xml);
					var pictureFileElement = pictureElement.Element("PictureFile");
					// FWR-3385: not sure how this could happen, but it has occurred in
					// real data
					if (pictureFileElement == null)
						continue;
					var objSurrogateElement = pictureFileElement.Element("objsur");
					var fileGuid = objSurrogateElement.Attribute("guid").Value;

					List<DomainObjectDTO> list;
					if (!map.TryGetValue(fileGuid, out list))
					{
						list = new List<DomainObjectDTO>();
						map[fileGuid] = list;
					}
					list.Add(picture);
				}
			}
			return map;
		}

		private Dictionary<string, string> CreateFilePathToGuidMap(IDomainObjectDTORepository repoDto, DomainObjectDTO folder)
		{
			var folderElement = XElement.Parse(folder.Xml);
			var map = new Dictionary<string, string>();
			foreach (var file in folderElement.Element("Files").Elements())
			{
				var fileGuid = file.Attribute("guid").Value;
				string filePath = GetFilePath(repoDto.GetDTO(fileGuid));
				if (filePath != null)
					map[filePath.ToLowerInvariant()] = fileGuid;
			}
			return map;
		}

		private string GetFilePath(DomainObjectDTO file)
		{
			var fileElement = XElement.Parse(file.Xml);
			var pathElement = fileElement.Element("InternalPath");
			if (pathElement != null)
				pathElement = pathElement.Element("Uni");
			return pathElement != null ? pathElement.Value : null;
		}

		#endregion
	}
}
