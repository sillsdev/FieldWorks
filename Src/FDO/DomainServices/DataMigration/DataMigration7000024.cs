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
// File: DataMigration7000024.cs
// Responsibility: Bush
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Linq;
using System.Xml.XPath;
using SIL.FieldWorks.FDO.DomainImpl;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Migrate data from 7000023 to 7000024.
	///
	/// FWR-262 Combine Sense and Sense Status lists into the Sense list.
	/// </summary>
	///
	/// <remarks>
	/// This migration needs to:
	///
	/// 1. Change the owning property from LangProject_AnalysisStatus to LangProject_Status
	/// 2. Move the 'Tentative' CmPossibility item from LexDb_Status list to LangProject_Status list
	/// 3. Any references from LexSense_Status to the 'Confirmed' item in the LexDb_Status list should be switched to the 'Confirmed' item in the LangProject_Status list. (If someone has renamed or deleted the 'Confirmed' item in LangProject_Status, then move the 'Confirmed' item from LexDb_Status list to LangProject_Status list)
	/// 4. Any references from LexSense_Status to the 'Approved' item in the LexDb_Status list should be switched to the 'Confirmed' item in the LangProject_Status list.
	/// 5. If there are any CmPossibility items in the LexDb_Status list other than 'Confirmed', 'Approved', and Tentative', these should be moved to the LangProject_Status list.
	/// 6. Delete the CmPossibilityList in LexDb_Status
	/// 7. Delete the LexDb_Status owning property.
	/// </remarks>
	/// ----------------------------------------------------------------------------------------
	internal class DataMigration7000024 : IDataMigration
	{
		#region IDataMigration Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Combine Sense and Sense Status lists into the Sense list.
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
			DataMigrationServices.CheckVersionNumber(domainObjectDtoRepository, 7000023);

			// 1. Change the owning property from LangProject_AnalysisStatus to LangProject_Status

			string statusConfirmedGuid = "";
			IEnumerable<XElement> langPossList = null;
			var langProjDto = domainObjectDtoRepository.AllInstancesSansSubclasses("LangProject").First();
			var langProjElement = XElement.Parse(langProjDto.Xml);
			var statusElement = langProjElement.Element("AnalysisStatus");
			if (statusElement != null)
				statusElement.Name= "Status";
			UpdateDto(domainObjectDtoRepository,langProjDto,langProjElement);

			var langPossListGuid = GetGuidOfObjSurChild(statusElement);

			var langPossListDto = domainObjectDtoRepository.GetDTO(langPossListGuid);
			var langPossListElement = XElement.Parse(langPossListDto.Xml);

			var langPossListPossibilities = langPossListElement.Element("Possibilities");
			if (langPossListPossibilities == null)
			{
				langPossListElement.Add(XElement.Parse("<Possibilities></Possibilities>"));
				UpdateDto(domainObjectDtoRepository,langPossListDto,langPossListElement);
				// Put the 'Confirmed' status into the list.
				statusConfirmedGuid = MakeStatus(domainObjectDtoRepository, langPossListGuid, langPossListElement,
					"Confirmed", "Conf");
				langPossList = GetPossibilitiesInList(domainObjectDtoRepository, langPossListElement);
				UpdateDto(domainObjectDtoRepository,langPossListDto,langPossListElement);
			}
			else
			{
				// Get the actual elements that represent the possibility items in the AnalysisStatus possibility list.
				langPossList = GetPossibilitiesInList(domainObjectDtoRepository, langPossListElement);

				// 1a.  Verify the 'Confirmed' status exists in the list.
				statusConfirmedGuid = IsStatusInList(langPossList, "Confirmed");
				if (statusConfirmedGuid == null) //if not, add it
				{
					statusConfirmedGuid = MakeStatus(domainObjectDtoRepository, langPossListGuid, langPossListElement,
						"Confirmed", "Conf");
				}
				UpdateDto(domainObjectDtoRepository,langPossListDto,langPossListElement);
			}

			// 1b.  If any rnGeneric records point to the 'Approved' status, point them to 'Confirnmed'.
			var approvedGuid = IsStatusInList(langPossList, "Approved");
			if (approvedGuid != null)
				ReassignStatuses(domainObjectDtoRepository, approvedGuid, statusConfirmedGuid);

			// 2. Get the pointers to the Lexdb Status list (sense status)
			var lexDbElement = langProjElement.Element("LexDb");
			if (lexDbElement != null)
			{
				var lexDto = domainObjectDtoRepository.AllInstancesSansSubclasses("LexDb").First();
				var lexElement = XElement.Parse(lexDto.Xml);
				var lexPossListGuid = GetGuidOfObjSurChild(lexElement.Element("Status"));

				var lexPossListDto = domainObjectDtoRepository.GetDTO(lexPossListGuid);
				var lexPossListElement = XElement.Parse(lexPossListDto.Xml);

				var lexPossList = GetPossibilitiesInList(domainObjectDtoRepository, lexPossListElement).ToList();

				var statusMap = new Dictionary<string, string>();

				// figure out the status substitutions we need to do.
				var langPossibilitiesElt = langPossListElement.Element("Possibilities");
				var lexPossibilitiesElt = lexPossListElement.Element("Possibilities");

				foreach (var lexStatus in lexPossList)
				{
					var nameElt = lexStatus.XPathSelectElement("Name/AUni[@ws='en']");
					var name = nameElt == null ? "" : nameElt.Value; // may have no English name
					if (name == "Approved") name = "Confirmed";
					var lexStatusGuid = lexStatus.Attribute("guid").Value;
					var langStatusGuid = IsStatusInList(langPossList, name);
					if (langStatusGuid == null)
					{
						// Move the old status (LexDb) to the surviving list(LangProj).
						var oldRef = FindObjSurWithGuid(lexPossibilitiesElt, lexStatusGuid);
						oldRef.Remove();
						langPossibilitiesElt.Add(oldRef);
						lexStatus.SetAttributeValue("ownerguid", langPossListGuid);
						UpdateDto(domainObjectDtoRepository, lexStatus);
						// Don't need to put anything in statusMap, we don't need to change refs
						// to a status that is being moved rather than deleted.
					}
					else
					{
						statusMap[lexStatusGuid.ToLowerInvariant()] = langStatusGuid;
						domainObjectDtoRepository.Remove(domainObjectDtoRepository.GetDTO(lexStatusGuid));
					}
				}
				UpdateDto(domainObjectDtoRepository, langPossListDto, langPossListElement);

				// We need to go through tha collection to point all statuses to the LangProj Status list
				foreach (var lexRecDto in domainObjectDtoRepository.AllInstancesSansSubclasses("LexSense").ToArray())
				{
					var lexSenseElement = XElement.Parse(lexRecDto.Xml);
					if (lexSenseElement.Element("Status") != null)
					{
						var statusObjSur = lexSenseElement.Element("Status").Element("objsur");
						var oldStatus = statusObjSur.Attribute("guid").Value;
						string newStatus;
						if (statusMap.TryGetValue(oldStatus.ToLowerInvariant(), out newStatus))
						{
							// We need to update this one.
							statusObjSur.SetAttributeValue("guid", newStatus);
							UpdateDto(domainObjectDtoRepository, lexRecDto, lexSenseElement);
						}
					}
				}
				if (approvedGuid != null)
				{
					// Delete the DTO for the 'Approved' item, and also the objsur that points at it
					// in the possibility list. Hopefully we already fixed all the refs to it.
					domainObjectDtoRepository.Remove(domainObjectDtoRepository.GetDTO(approvedGuid));
					var oldRef = FindObjSurWithGuid(langPossibilitiesElt, approvedGuid);
					oldRef.Remove();
					UpdateDto(domainObjectDtoRepository, langPossListDto, langPossListElement);
				}
				// 6. Delete the CmPossibilityList in LexDb_Status
				domainObjectDtoRepository.Remove(lexPossListDto);
				// 7. Delete the LexDb_Status owning property.
				lexElement.Element("Status").Remove();
				UpdateDto(domainObjectDtoRepository, lexDto, lexElement);
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
		/// <summary>
		/// Given that the element has been changed to represent the desired new state of some DTO,
		/// save the change. This overload finds the DTO from the guid attribute on the element.
		/// </summary>
		private void UpdateDto(IDomainObjectDTORepository domainObjectDtoRepository, XElement element)
		{
			DomainObjectDTO dto = domainObjectDtoRepository.GetDTO(element.Attribute("guid").Value);
			dto.Xml = element.ToString();
			domainObjectDtoRepository.Update(dto);
		}

		/// <summary>
		/// Given an XElement that has a list of objsur elements as children, find the one with the specified guid.
		/// </summary>
		private XElement FindObjSurWithGuid(XElement element, string guid)
		{
			return (from XElement elt in element.Nodes() where elt.Attribute("guid").Value == guid select elt).
				First();
		}

		private string MakeStatus(IDomainObjectDTORepository domainObjectDtoRepository, string langPossListGuid,
			XElement langPossListElement, string name, string abbr)
		{
			string statusConfirmedGuid;
			statusConfirmedGuid = Guid.NewGuid().ToString();
			var confirmed = new XElement("rt",
										 new XAttribute("class", "CmPossibility"),
										 new XAttribute("guid", statusConfirmedGuid),
										 new XAttribute("ownerguid", langPossListGuid),
										 MakeMultiUnicode("Name", name),
										 MakeMultiUnicode("Abbreviation", abbr));
			var dtoConfirmed = new DomainObjectDTO(statusConfirmedGuid, "CmPossibility", confirmed.ToString());
			domainObjectDtoRepository.Add(dtoConfirmed);
			langPossListElement.Element("Possibilities").Add(MakeOwningSurrogate(statusConfirmedGuid));
			return statusConfirmedGuid;
		}

		private IEnumerable<XElement> GetPossibilitiesInList(IDomainObjectDTORepository domainObjectDtoRepository, XElement langPossListElement)
		{
			return from elt in langPossListElement.XPathSelectElements("Possibilities/objsur")
				   select XElement.Parse(
				domainObjectDtoRepository.GetDTO(elt.Attribute("guid").Value).Xml);
		}

		private string GetGuidOfObjSurChild(XElement statusElement)
		{
			return statusElement.Element("objsur").Attribute("guid").Value;
		}

		private XElement MakeOwningSurrogate(string confirmedGuid)
		{
			return new XElement("objsur", new XAttribute("guid", confirmedGuid), new XAttribute("t", "o"));
		}

		/// <summary>
		/// Make an XElement that represents a single multiunicode attribute with the specified name,
		/// and a single English alternative.
		/// </summary>
		private XElement MakeMultiUnicode(string name, string value)
		{
			return new XElement(name, new XElement("AUni", new XAttribute("ws", "en"), new XText(value)));
		}

		/// <summary>
		/// If there is an XElement in the list which is a CmPossibility which has a 'Name'
		/// multistring with the English alternative equal to status, return its guid. If not, return null.
		/// </summary>
		private string IsStatusInList(IEnumerable<XElement> list, string status)
		{
			var xpath = "Name/AUni[@ws='en']";
			foreach (XElement elt in list)
			{
				XElement nameElt = elt.XPathSelectElement(xpath);
				if (nameElt != null && nameElt.Value == status)
				{
					// this is the one we want. Get its guid.
					return elt.Attribute("guid").Value;
				}
			}
			return null;
		}

/// <summary>
		/// Change any RnGenericRecord whose status is oldStatus to be newStatus.
		/// </summary>
		private void ReassignStatuses(IDomainObjectDTORepository domainObjectDtoRepository, string oldStatus, string newStatus)
		{
			// We need to go through tha collection because we need to change all status 'Appproved' to 'Confirmed'
			foreach (var RnRecDto in domainObjectDtoRepository.AllInstancesSansSubclasses("RnGenericRec").ToArray())
			{
				var rnElement = XElement.Parse(RnRecDto.Xml);
				if (!UpdateStatusInElement(rnElement, oldStatus, newStatus))
					continue;
				RnRecDto.Xml = rnElement.ToString();
				domainObjectDtoRepository.Update(RnRecDto);
			}
		}

		// If the element has a child named 'status' which contains an objsur with a guid attribute that
		// is equal to oldStatus, change it to newStatus and return true. Otherwise return false.
		private bool UpdateStatusInElement(XElement element, string oldStatus, string newStatus)
		{
			var statusSurrogateElt = element.XPathSelectElement("Status/objsur");
			if (statusSurrogateElt == null)
				return false;
			var rnStatusGuid = (string) statusSurrogateElt.Attribute("guid");
			if (rnStatusGuid != oldStatus)
				return false;
			statusSurrogateElt.SetAttributeValue("guid", newStatus);
			return true;
		}

		#endregion
	}
}
