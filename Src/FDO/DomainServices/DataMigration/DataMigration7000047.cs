// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DataMigration7000047.cs
// Responsibility: FW team
// ---------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Migrate data from 7000046 to 7000047.
	///
	/// Cleans up legacy ChkRendering objects with null SurfaceForms.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DataMigration7000047 : IDataMigration
	{
		#region IDataMigration Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Cleans up legacy ChkRendering objects with null SurfaceForms.
		/// </summary>
		/// <param name="repoDto">
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
		public void PerformMigration(IDomainObjectDTORepository repoDto)
		{
			DataMigrationServices.CheckVersionNumber(repoDto, 7000046);

			Dictionary<Guid, DomainObjectDTO> mapOfRenderingsToChk = new Dictionary<Guid, DomainObjectDTO>();
			HashSet<DomainObjectDTO> renderingsToDelete = new HashSet<DomainObjectDTO>();

			foreach (DomainObjectDTO dto in repoDto.AllInstances())
			{
				XElement data = XElement.Parse(dto.Xml);
				XAttribute classAttr = data.Attribute("class");
				if (classAttr.Value == "ChkTerm")
				{
					XElement renderings = data.Element("Renderings");
					if (renderings != null)
						foreach (XElement r in renderings.Elements())
							mapOfRenderingsToChk[new Guid(r.Attribute("guid").Value)] = dto;
				}
				else if (classAttr.Value == "ChkRendering")
				{
					XElement surfaceForm = data.Element("SurfaceForm");
					if (surfaceForm == null || !surfaceForm.HasElements)
						renderingsToDelete.Add(dto);
				}
			}

			foreach (DomainObjectDTO rendering in renderingsToDelete)
			{
				DomainObjectDTO chkTerm = mapOfRenderingsToChk[new Guid(rendering.Guid)];
				XElement termData = XElement.Parse(chkTerm.Xml);
				XElement renderings = termData.Element("Renderings");
				XElement bogusRendering = renderings.Elements().First(e => e.Attribute("guid").Value.Equals(rendering.Guid, StringComparison.OrdinalIgnoreCase));
				bogusRendering.Remove();
				DataMigrationServices.UpdateDTO(repoDto, chkTerm, termData.ToString());
				repoDto.Remove(rendering);
			}

			DataMigrationServices.IncrementVersionNumber(repoDto);
		}
		#endregion
	}
}