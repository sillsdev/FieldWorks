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
// File: DataMigration7000007.cs
// Responsibility: Bush
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;
using System.Xml.XPath;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Migrate data from 7000006 to 7000007.
	///
	/// 1) Remove ScrImportSet.ImportSettings
	/// 2) Remove StFootnote.DisplayFootnoteReference and DisplayFootnoteMarker
	/// 3) Remove StPara.StyleName
	/// </summary>
	///
	/// <remarks>
	/// This migration needs to:
	///		1. Select the CmProject classes.
	///		2. Delete the 'Name' attribute.
	/// </remarks>
	/// ----------------------------------------------------------------------------------------
	internal class DataMigration7000007 : IDataMigration
	{
		#region IDataMigration Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 1) Remove ScrImportSet.ImportSettings
		/// 2) Remove StFootnote.DisplayFootnoteReference and DisplayFootnoteMarker
		/// 3) Remove StPara.StyleName
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
			DataMigrationServices.CheckVersionNumber(domainObjectDtoRepository, 7000006);

			// 1) Select the ScrImportSet classes.
			// 2) Delete the 'ImportSettings' attribute.
			foreach (var ScriptureDto in domainObjectDtoRepository.AllInstancesSansSubclasses("ScrImportSet"))
			{
				var rtElement = XElement.Parse(ScriptureDto.Xml);
				var ClassDto = rtElement.Element(ScriptureDto.Classname);
				var ProjElement = rtElement.Element("ScrImportSet");
				var rmElement = ProjElement.Element("ImportSettings");
				if (rmElement!= null)
					rmElement.Remove();

				DataMigrationServices.UpdateDTO(domainObjectDtoRepository, ScriptureDto, rtElement.ToString());
			}

			// 3) Select the StFootnote classes.
			// 4) Delete the 'DisplayFootnoteReference' and 'DisplayFootnoteMarker' attributes.

			foreach (var FootnoteDto in domainObjectDtoRepository.AllInstancesSansSubclasses("StFootnote"))
			{
				var rt2Element = XElement.Parse(FootnoteDto.Xml);
				var ClassDto = rt2Element.Element(FootnoteDto.Classname);
				var ProjElement = rt2Element.Element("StFootnote");
				var rm2Element = ProjElement.Element("DisplayFootnoteReference");
				if (rm2Element!= null)
					rm2Element.Remove();
				var rm2Element2 = ProjElement.Element("DisplayFootnoteMarker");
				if (rm2Element2!= null)
					rm2Element2.Remove();

				DataMigrationServices.UpdateDTO(domainObjectDtoRepository, FootnoteDto, rt2Element.ToString());
			}

			// 3a) Select the ScrFootnote classes (StFootnote could be imbedded inside it).
			// 4a) Delete the 'DisplayFootnoteReference' and 'DisplayFootnoteMarker' attributes.

			foreach (var FootNoteaDto in domainObjectDtoRepository.AllInstancesSansSubclasses("ScrFootnote"))
			{
				var rtaElement = XElement.Parse(FootNoteaDto.Xml);
				var ClassDto = rtaElement.Element(FootNoteaDto.Classname);
				var ProjElement = rtaElement.Element("StFootnote");
				var rm2aElement = ProjElement.Element("DisplayFootnoteReference");
				if (rm2aElement!= null)
					rm2aElement.Remove();
				var rm2aElement2 = ProjElement.Element("DisplayFootnoteMarker");
				if (rm2aElement2!= null)
					rm2aElement2.Remove();

				DataMigrationServices.UpdateDTO(domainObjectDtoRepository, FootNoteaDto, rtaElement.ToString());
			}

			// 5) Select the StPara classes.
			// 6) Delete the 'StyleName' attribute.
			foreach (var StParaDto in domainObjectDtoRepository.AllInstancesSansSubclasses("StPara"))
			{
				var rt3Element = XElement.Parse(StParaDto.Xml);
				var ClassDto = rt3Element.Element(StParaDto.Classname);
				var ProjElement = rt3Element.Element("StPara");
				var rm3Element = ProjElement.Element("StyleName");
				if (rm3Element!= null)
					rm3Element.Remove();

				DataMigrationServices.UpdateDTO(domainObjectDtoRepository, StParaDto, rt3Element.ToString());
			}

			// 5a) Select the StTxtPara classes (StPara could be imbedded inside it).
			// 6a) Delete the 'StyleName' attribute.
			foreach (var StParaaDto in domainObjectDtoRepository.AllInstancesSansSubclasses("StTxtPara"))
			{
				var rt3aElement = XElement.Parse(StParaaDto.Xml);
				var ClassaDto = rt3aElement.Element(StParaaDto.Classname);
				var ProjaElement = rt3aElement.Element("StPara");
				var rm3aElement = ProjaElement.Element("StyleName");
				if (rm3aElement!= null)
					rm3aElement.Remove();

				DataMigrationServices.UpdateDTO(domainObjectDtoRepository, StParaaDto, rt3aElement.ToString());
			}

			// 5b) Select the ScrTxtPara classes (StPara could be imbedded inside it).
			// 6b) Delete the 'StyleName' attribute.
			foreach (var StParabDto in domainObjectDtoRepository.AllInstancesSansSubclasses("ScrTxtPara"))
			{
				var rt3bElement = XElement.Parse(StParabDto.Xml);
				var ClassbDto = rt3bElement.Element(StParabDto.Classname);
				var ProjbElement = rt3bElement.Element("StPara");
				var rm3bElement = ProjbElement.Element("StyleName");
				if (rm3bElement!= null)
					rm3bElement.Remove();

				DataMigrationServices.UpdateDTO(domainObjectDtoRepository, StParabDto, rt3bElement.ToString());
			}

			DataMigrationServices.IncrementVersionNumber(domainObjectDtoRepository);
		}
		#endregion
	}
}
