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
using System.Xml.Linq;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Migrate data from 7000026 to 7000027.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DataMigration7000027 : IDataMigration
	{
		#region IDataMigration Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the ParaContainingOrc property for all ScrFootnotes
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
			DataMigrationServices.CheckVersionNumber(domainObjectDtoRepository, 7000026);

			foreach (var scrTxtPara in domainObjectDtoRepository.AllInstancesSansSubclasses("ScrTxtPara"))
			{
				XElement para = XElement.Parse(scrTxtPara.Xml);
				XElement contents = para.Element("Contents");
				if (contents == null)
					continue;
				XElement str = contents.Element("Str");
				if (str == null)
					continue;

				foreach (XElement run in str.Elements("Run"))
				{
					XAttribute linkAttr = run.Attribute("ownlink");
					if (linkAttr == null)
						continue; // Run doesn't contain a link
					DomainObjectDTO linkObj;
					// skip links to missing footnotes - user will have to clean these up later.
					if (!domainObjectDtoRepository.TryGetValue(linkAttr.Value, out linkObj))
						continue;
					XElement footnote = XElement.Parse(linkObj.Xml);
					if (footnote.Attribute("class").Value != "ScrFootnote")
						continue; // Link is not for a footnote

					if (footnote.Element("ParaContainingOrc") == null)
					{
						// ParaContainingOrc property is not present in the footnote, so it needs
						// to be added.
						XElement paraContainingOrcElm = XElement.Parse("<ParaContainingOrc><objsur guid=\"" +
							scrTxtPara.Guid + "\" t=\"r\" /></ParaContainingOrc>");
						footnote.Add(paraContainingOrcElm);

						DataMigrationServices.UpdateDTO(domainObjectDtoRepository, linkObj, footnote.ToString());
					}
				}
			}

			DataMigrationServices.IncrementVersionNumber(domainObjectDtoRepository);
		}

		#endregion
	}
}
