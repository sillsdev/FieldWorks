// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DataMigration7000003.cs
// Responsibility: shaneyfelt
//
// <remarks>
// </remarks>

using System.Collections.Generic;
using System.Xml.Linq;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Migrate data from 7000002 to 7000003.
	///
	/// Changes all StFootnotes to ScrFootnotes
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DataMigration7000003 : IDataMigration
	{
		#region IDataMigration Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Changes all StFootnotes to ScrFootnotes
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
			DataMigrationServices.CheckVersionNumber(domainObjectDtoRepository, 7000002);

			var footnotesToChangeClasses = new List<DomainObjectDTO>();
			foreach (var footnote in domainObjectDtoRepository.AllInstancesSansSubclasses("StFootnote"))
			{
				XElement footnoteEl = XElement.Parse(footnote.Xml);
				footnoteEl.Attribute("class").Value = "ScrFootnote";

				// Add the empty ScrFootnote element to the ScrFootnote
				footnoteEl.Add(new XElement("ScrFootnote"));

				// Update the object XML
				footnote.Xml = footnoteEl.ToString();
				footnote.Classname = "ScrFootnote";
				footnotesToChangeClasses.Add(footnote);
			}

			// Udate the repository
			var startingStructure = new ClassStructureInfo("StText", "StFootnote");
			var endingStructure = new ClassStructureInfo("StFootnote", "ScrFootnote");
			foreach (var changedPara in footnotesToChangeClasses)
				domainObjectDtoRepository.Update(changedPara,
					startingStructure,
					endingStructure);

			DataMigrationServices.IncrementVersionNumber(domainObjectDtoRepository);
		}

		#endregion
	}
}
