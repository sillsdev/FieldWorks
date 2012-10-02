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
// File: DataMigration7000017.cs
// Responsibility: mcconnel
//
// <remarks>
// This implements FWR-645.
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Migrates from 7000016 to 7000017
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DataMigration7000017 : IDataMigration
	{
		#region IDataMigration Members

		/// <summary>
		/// Check to see if there is any data in RnGenericRec.Weather fields.  If not, delete
		/// the field and list. If there is, convert the Weather field into a custom field and
		/// the Weather list into a custom list.
		/// </summary>
		public void PerformMigration(IDomainObjectDTORepository repoDTO)
		{
			var collectOverlaysToRemove = new List<DomainObjectDTO>();
			bool fWeatherUsed = IsWeatherUsed(repoDTO, collectOverlaysToRemove);
			if (fWeatherUsed)
				ConvertWeatherToCustomListAndField(repoDTO);
			else
			{
				DeleteWeatherListAndField(repoDTO);
				RemoveUnwantedOverlays(repoDTO, collectOverlaysToRemove);
			}

			DataMigrationServices.IncrementVersionNumber(repoDTO);
		}

		private void RemoveUnwantedOverlays(IDomainObjectDTORepository repoDTO, List<DomainObjectDTO> collectOverlaysToRemove)
		{
			DomainObjectDTO dtoLP = GetDtoLangProj(repoDTO);
			foreach (var dto in collectOverlaysToRemove)
			{
				RemoveOverlayElement(dtoLP, dto.Guid);
				repoDTO.Remove(dto);
			}
		}

		/// <summary>
		/// The Weather list is never used, so delete it (and remove any empty Weather elements
		/// from the RnGenericRec elements).
		/// </summary>
		private void DeleteWeatherListAndField(IDomainObjectDTORepository repoDTO)
		{
			// Remove the Weather list.
			DomainObjectDTO dtoLP = GetDtoLangProj(repoDTO);
			string sWeatherListGuid = RemoveWeatherConditionsElement(dtoLP).ToLowerInvariant();
			repoDTO.Update(dtoLP);
			DomainObjectDTO dtoDeadList = null;
			foreach (var dto in repoDTO.AllInstancesWithSubclasses("CmPossibilityList"))
			{
				if (dto.Guid.ToLowerInvariant() == sWeatherListGuid)
				{
					dtoDeadList = dto;
					break;
				}
			}
			List<DomainObjectDTO> rgdtoDead = new List<DomainObjectDTO>();
			GatherDeadObjects(repoDTO, dtoDeadList, rgdtoDead);
			foreach (var dto in rgdtoDead)
				repoDTO.Remove(dto);

			// Remove any empty Weather elements in the RnGenericRec objects.
			foreach (var dto in repoDTO.AllInstancesWithSubclasses("RnGenericRec"))
			{
				string sXml = dto.Xml;
				int idx = sXml.IndexOf("<Weather");
				if (idx > 0)
				{
					dto.Xml = RemoveEmptyWeather(sXml, idx);
					repoDTO.Update(dto);
				}
			}
		}

		private DomainObjectDTO GetDtoLangProj(IDomainObjectDTORepository repoDTO)
		{
			DomainObjectDTO dtoLP = null;
			foreach (var dto in repoDTO.AllInstancesWithSubclasses("LangProject"))
			{
				dtoLP = dto;
				break;
			}
			return dtoLP;
		}

		private string RemoveEmptyWeather(string sXml, int idx)
		{
			string sWeather = sXml.Substring(idx);
			int idxLim = sWeather.IndexOf('>') + 1;
			if (sWeather.Substring(idxLim - 2).StartsWith("/>"))
				return sXml.Remove(idx, idxLim);
			idxLim = sWeather.IndexOf("</Weather>") + 10;
			sWeather = sWeather.Substring(0, idxLim);
			if (!sWeather.Contains("<objsur "))
				return sXml.Remove(idx, idxLim);
			return sXml;
		}

		private void GatherDeadObjects(IDomainObjectDTORepository repoDTO, DomainObjectDTO dtoDead,
			List<DomainObjectDTO> rgdtoDead)
		{
			rgdtoDead.Add(dtoDead);
			foreach (var dto in repoDTO.GetDirectlyOwnedDTOs(dtoDead.Guid))
				GatherDeadObjects(repoDTO, dto, rgdtoDead);
		}

		private string RemoveWeatherConditionsElement(DomainObjectDTO dtoLP)
		{
			string sLpXml = dtoLP.Xml;
			int idx = sLpXml.IndexOf("<WeatherConditions>");
			int idxEnd = sLpXml.IndexOf("</WeatherConditions>");
			int cch = (idxEnd + 20) - idx;
			string sWeatherConditions = sLpXml.Substring(idx, cch);
			dtoLP.Xml = sLpXml.Remove(idx, cch);
			return ExtractFirstGuid(sWeatherConditions, 0, " guid=\"");
		}

		private void RemoveOverlayElement(DomainObjectDTO dtoLP, string overlayGuid)
		{
			string sLpXml = dtoLP.Xml;
			int idx = sLpXml.IndexOf("<Overlays>");
			int idxEnd = sLpXml.IndexOf("</Overlays>");
			var target = overlayGuid.ToLowerInvariant();
			if (idx > 0 && idxEnd > idx)
			{
					for (int ich = idx; ; )
					{
						var sobjsur = "<objsur";
						int ichSurr = sLpXml.IndexOf(sobjsur, ich, idxEnd - ich);
						if (ichSurr < 0)
						{
							Debug.Fail("did not find expected surrogate to remove");
							break;
						}
						var sguid = "guid=\"";
						int ichGuid = sLpXml.IndexOf(sguid, ichSurr, idxEnd - ichSurr);
						if (ichGuid < 0)
						{
							Debug.Fail("objsur without guid!");
							break; // Assert? surrogate without guid??
						}
						ichGuid += sguid.Length;
						int ichEndGuid = sLpXml.IndexOf("\"", ichGuid, idxEnd - ichGuid);
						if (target == sLpXml.Substring(ichGuid, ichEndGuid - ichGuid).ToLowerInvariant())
						{
							var endSurr = "/>";
							int ichEnd = sLpXml.IndexOf(endSurr, ichEndGuid, idxEnd - ichEndGuid);
							if (ichEnd < 0)
							{
								Debug.Fail("incomplete objsur");
								break; // assert?
							}
							ichEnd += endSurr.Length;
							dtoLP.Xml = sLpXml.Remove(ichSurr, ichEnd - ichSurr);
							return;
						}
						ich = ichEndGuid;
					}
			}
		}

		private string ExtractFirstGuid(string sElement, int startIndex, string sAttrTag)
		{
			int idx = sElement.IndexOf(sAttrTag, startIndex);
			if (idx <= 0)
				return null;
			string sGuid = sElement.Substring(idx + sAttrTag.Length);
			int idxEnd = sGuid.IndexOf('"');
			return sGuid.Substring(0, idxEnd);
		}

		/// <summary>
		/// The weather list is used, so convert it to a custom (unowned) list, create a new
		/// custom field for RnGenericRec elements, and convert any Weather elements to that
		/// new custom field.
		/// </summary>
		private void ConvertWeatherToCustomListAndField(IDomainObjectDTORepository repoDTO)
		{
			// Change the Weather list to being unowned.
			DomainObjectDTO dtoLP = null;
			foreach (var dto in repoDTO.AllInstancesWithSubclasses("LangProject"))
			{
				dtoLP = dto;
				break;
			}
			string sWeatherListGuid = RemoveWeatherConditionsElement(dtoLP).ToLowerInvariant();
			repoDTO.Update(dtoLP);
			DomainObjectDTO dtoWeatherList = null;
			foreach (var dto in repoDTO.AllInstancesWithSubclasses("CmPossibilityList"))
			{
				if (dto.Guid.ToLowerInvariant() == sWeatherListGuid)
				{
					dtoWeatherList = dto;
					break;
				}
			}
			dtoWeatherList.Xml = RemoveOwnerGuid(dtoWeatherList.Xml);
			repoDTO.Update(dtoWeatherList);

			// Create the custom field.
			string fieldName = "Weather";
			while (repoDTO.IsFieldNameUsed("RnGenericRec", fieldName))
				fieldName = fieldName + "A";
			repoDTO.CreateCustomField("RnGenericRec", fieldName, SIL.CoreImpl.CellarPropertyType.ReferenceCollection,
				CmPossibilityTags.kClassId, "originally a standard part of Data Notebook records",
				WritingSystemServices.kwsAnals, new Guid(sWeatherListGuid));

			string customStart = String.Format("<Custom name=\"{0}\">", fieldName);

			// Remove any empty Weather elements in the RnGenericRec objects, and convert
			// nonempty ones to custom elements.
			foreach (var dto in repoDTO.AllInstancesWithSubclasses("RnGenericRec"))
			{
				string sXml = dto.Xml;
				int idx = sXml.IndexOf("<Weather");
				if (idx > 0)
				{
					string sXmlT = RemoveEmptyWeather(sXml, idx);
					if (sXmlT == sXml)
					{
						sXmlT = sXml.Replace("<Weather>", customStart);
						sXmlT = sXmlT.Replace("</Weather>", "</Custom>");
					}
					dto.Xml = sXmlT;
					repoDTO.Update(dto);
				}
			}
		}

		private string RemoveOwnerGuid(string sXml)
		{
			int idx = sXml.IndexOf(" ownerguid=\"");
			int idxLim = sXml.IndexOf('"', idx + 13) + 1;
			return sXml.Remove(idx, idxLim - idx);
		}

		private bool IsWeatherUsed(IDomainObjectDTORepository repoDTO, List<DomainObjectDTO> collectOverlaysToRemove)
		{
			DomainObjectDTO dtoLP = GetDtoLangProj(repoDTO);
			string sLpXml = dtoLP.Xml;
			int idxW = sLpXml.IndexOf("<WeatherConditions>");
			var sguidWeather = ExtractFirstGuid(sLpXml, idxW, " guid=\"");
			DomainObjectDTO dtoWeather = repoDTO.GetDTO(sguidWeather);
			var weatherItems = new HashSet<string>();
			CollectItems(repoDTO, dtoWeather, weatherItems);
			foreach (var dto in repoDTO.AllInstancesWithSubclasses("RnGenericRec"))
			{
				string sXml = dto.Xml;
				int idx = sXml.IndexOf("<Weather>");
				if (idx > 0)
				{
					int idxEnd = sXml.IndexOf("</Weather>");
					if (idxEnd > idx)
					{
						string s = sXml.Substring(idx, idxEnd - idx);
						if (s.Contains("<objsur "))
						{
							return true;
						}
					}
				}
				bool dummy = false;
				if (StringContainsRefToItemInList("PhraseTags", weatherItems, sXml, ref dummy)) return true;
			}
			foreach (var dto in repoDTO.AllInstancesSansSubclasses("CmOverlay"))
			{
				string sXml = dto.Xml;
				bool hasOtherItems = false;
				bool hasWeatherRef = StringContainsRefToItemInList("PossItems", weatherItems, sXml, ref hasOtherItems);
				var weatherListSet = new HashSet<string>();
				weatherListSet.Add(sguidWeather.ToLowerInvariant());
				hasWeatherRef |= StringContainsRefToItemInList("PossList", weatherListSet, sXml, ref hasOtherItems);
				if (hasWeatherRef)
				{
					if (hasOtherItems)
						return true; // an overlay with a mixture of weather and non-weather items is a problem
					// One with only weather refs (and not used, since we know nothing is tagged to weather)
					// will be deleted.
					collectOverlaysToRemove.Add(dto);
				}
			}
			return false;
		}

		private bool StringContainsRefToItemInList(string elementName, HashSet<string> weatherItems, string sXml, ref bool hasOtherItems)
		{
			int idx;
			idx = sXml.IndexOf("<" + elementName + ">");
			bool result = false;
			if (idx > 0)
			{
				int idxEnd = sXml.IndexOf("</" + elementName + ">");
				if (idxEnd > idx)
				{
					string s = sXml.Substring(idx, idxEnd - idx);
					for(int ich = 0; ;)
					{
						var sguid = "guid=\"";
						int ichGuid = s.IndexOf(sguid, ich);
						if (ichGuid < 0)
							break;
						ichGuid += sguid.Length;
						int ichEnd = s.IndexOf("\"", ichGuid);
						var guid = s.Substring(ichGuid, ichEnd - ichGuid).ToLowerInvariant();
						if (weatherItems.Contains(guid))
							result = true;
						else
							hasOtherItems = true;
						ich = ichEnd;
					}
				}
			}
			return result;
		}

		/// <summary>
		/// Add to weatherItems the guids of all the things owned directly or indirectly by dtoRoot.
		/// Does not include the root itself.
		/// </summary>
		private void CollectItems(IDomainObjectDTORepository repoDTO, DomainObjectDTO dtoRoot, HashSet<string> guidCollector)
		{
			foreach (var dto in repoDTO.GetDirectlyOwnedDTOs(dtoRoot.Guid))
			{
				guidCollector.Add(dto.Guid);
				CollectItems(repoDTO, dto, guidCollector);
			}
		}

		#endregion
	}
}
