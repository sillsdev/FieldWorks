// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DataMigration7000027.cs
// Responsibility: FW team
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
	/// Migrate data from 7000058 to 7000059.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DataMigration7000059 : IDataMigration
	{
		#region IDataMigration Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes Texts (IText) be unowned.
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
			DataMigrationServices.CheckVersionNumber(domainObjectDtoRepository, 7000058);

			// Remove LangProject Texts property
			var lpDto = domainObjectDtoRepository.AllInstancesSansSubclasses("LangProject").First();
			var lp = XElement.Parse(lpDto.Xml);
			var texts = lp.Element("Texts");
			if (texts != null)
			{
				texts.Remove();
				DataMigrationServices.UpdateDTO(domainObjectDtoRepository, lpDto, lp.ToString());
			}

			// Change RnGenericRec Text property from owned to reference
			foreach (var rnRecDto in domainObjectDtoRepository.AllInstancesSansSubclasses("RnGenericRec"))
			{
				var rec = XElement.Parse(rnRecDto.Xml);
				var textElt = rec.Element("Text");
				if (textElt != null)
				{
					var objsurElt = textElt.Element("objsur");
					if (objsurElt != null && objsurElt.Attribute("t") != null)
					{
						objsurElt.Attribute("t").SetValue("r");
						DataMigrationServices.UpdateDTO(domainObjectDtoRepository, rnRecDto, rec.ToString());
					}
				}
			}

			// Remove owner from all Texts
			foreach (var textDto in domainObjectDtoRepository.AllInstancesSansSubclasses("Text"))
			{
				XElement text = XElement.Parse(textDto.Xml);
				var ownerAttr = text.Attribute("ownerguid");
				if (ownerAttr != null)
				{
					ownerAttr.Remove();
					DataMigrationServices.UpdateDTO(domainObjectDtoRepository, textDto, text.ToString());
				}
			}

			DataMigrationServices.IncrementVersionNumber(domainObjectDtoRepository);
		}

		#endregion
	}
}
