// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DataMigration7000002.cs
// Responsibility: FW team
//
// <remarks>
// </remarks>

using System.Collections.Generic;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Migrate data from 7000001 to 7000002.
	///
	/// Changes all StTxtParas that are owned by Scripture to be ScrTxtParas
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DataMigration7000002 : IDataMigration
	{
		#region IDataMigration Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Changes all StTxtParas that are owned by Scripture to be ScrTxtParas
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
			DataMigrationServices.CheckVersionNumber(domainObjectDtoRepository, 7000001);

			var parasToChangeClasses = new List<DomainObjectDTO>();
			foreach (var stTxtPara in domainObjectDtoRepository.AllInstancesSansSubclasses("StTxtPara"))
			{
				var paraOwner = domainObjectDtoRepository.GetOwningDTO(stTxtPara);
				if (paraOwner.Classname != "StText" && paraOwner.Classname != "StFootnote")
				{
					continue; // Paragraph is not owned by an StText or StFootnote (shouldn't ever happen)
				}

				var textOwner = domainObjectDtoRepository.GetOwningDTO(paraOwner);
				if (textOwner.Classname != "ScrBook" && textOwner.Classname != "ScrSection")
				{
					continue; // StText is not part of Scripture, don't change.
				}
				// Its one of the paragraphs that we care about. We just need to change the
				// class in the xml to be a ScrTxtPara and change the objectsByClass map.
				DataMigrationServices.ChangeToSubClass(stTxtPara, "StTxtPara", "ScrTxtPara");
				parasToChangeClasses.Add(stTxtPara);
			}

			// Udate the repository
			var startingStructure = new ClassStructureInfo("StPara", "StTxtPara");
			var endingStructure = new ClassStructureInfo("StTxtPara", "ScrTxtPara");
			foreach (var changedPara in parasToChangeClasses)
				domainObjectDtoRepository.Update(changedPara,
					startingStructure,
					endingStructure);

			DataMigrationServices.IncrementVersionNumber(domainObjectDtoRepository);
		}

		#endregion
	}
}
