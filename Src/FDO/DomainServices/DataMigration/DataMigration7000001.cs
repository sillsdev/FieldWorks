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
// File: DataMigration7000001.cs
// Responsibility: FW Team
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
	/// Migrate data from 7000000 to 7000001.
	///
	/// The change was to remove the WordformInventory class and data instance, which makes all
	/// WfiWordforms it used to own be ownerless.
	///
	/// There are no known references to the WordformInventory, other than:
	///		1. Its previous owner, which was the WordformInventory property of LangProject, and
	///		2. The 'Owner' of all WfiWordforms
	/// </summary>
	/// <remarks>
	/// This migration needs to:
	///		1. Delete the WordformInventory property of LangProject,
	///		2. Delete the WordformInventory instance 'rt' element, and
	///		3. Remove the three owning attributes from all WfiWordforms
	///			('rt' element attrs: ownerguid, owningflid, and owningord)
	/// </remarks>
	/// ----------------------------------------------------------------------------------------
	internal class DataMigration7000001 : IDataMigration
	{
		#region Implementation of IDataMigration

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Perform one increment migration step.
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
			DataMigrationServices.CheckVersionNumber(domainObjectDtoRepository, 7000000);

			DataMigrationServices.Delint(domainObjectDtoRepository);

			var lpDto = domainObjectDtoRepository.AllInstancesSansSubclasses("LangProject").First();
			// 1. Remove property from LangProg.
			var lpElement = XElement.Parse(lpDto.Xml);
			lpElement.Descendants("WordformInventory").Remove();
			DataMigrationServices.UpdateDTO(domainObjectDtoRepository, lpDto, lpElement.ToString());

			// 2. Remove three owner-related attributes from each WfiWordform.
			// (There may be zero, or more, instances to upgrade.)
			foreach (var wordformDto in domainObjectDtoRepository.AllInstancesSansSubclasses("WfiWordform"))
			{
				var wfElement = XElement.Parse(wordformDto.Xml);
				wfElement.Attribute("ownerguid").Remove();
				var flidAttr = wfElement.Attribute("owningflid");
				if (flidAttr != null)
					flidAttr.Remove();
				var ordAttr = wfElement.Attribute("owningord");
				if (ordAttr != null)
					ordAttr.Remove();
				DataMigrationServices.UpdateDTO(domainObjectDtoRepository, wordformDto, wfElement.ToString());
			}

			// 3. Remove WordformInventory instance.
			domainObjectDtoRepository.Remove(
				domainObjectDtoRepository.AllInstancesSansSubclasses("WordformInventory").First());

			// There are no references to the WFI, so don't fret about leaving dangling refs to it.

			DataMigrationServices.IncrementVersionNumber(domainObjectDtoRepository);
		}

		#endregion
	}
}
