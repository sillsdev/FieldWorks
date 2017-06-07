// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// 1) Re-run the corrected DM69 code for adding the default LexEntryRefType
	/// 2) Clean out any VariantEntryTypes on LexEntryRefs which are complex forms and clean out any ComplexFormTypes on
	///	   refs that are variants.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DataMigration7000070 : IDataMigration
	{
		public void PerformMigration(IDomainObjectDTORepository repoDto)
		{
			DataMigrationServices.CheckVersionNumber(repoDto, 7000069);

			CleanOutBadRefTypes(repoDto);
			RenameDuplicateCustomListsAndFixBadLists(repoDto);

			DataMigrationServices.IncrementVersionNumber(repoDto);
		}

		private void RenameDuplicateCustomListsAndFixBadLists(IDomainObjectDTORepository repoDto)
		{
			var allLists = repoDto.AllInstancesWithSubclasses("CmPossibilityList");
			var namesAndLists = new Dictionary<Tuple<string, string>, DomainObjectDTO>();
			var duplicates = new List<Tuple<DomainObjectDTO, DomainObjectDTO>>();
			foreach (var list in allLists)
			{
				var listElement = XElement.Parse(list.Xml);
				var name = listElement.Elements("Name");
				var listTitles = name.Elements("AUni");
				// Grab the elements which had bad numbers put in them and fix them saving the dto
				var displayOption = listElement.Elements("DisplayOption").FirstOrDefault();
				if (displayOption != null)
				{
					PossNameType option;
					if (!Enum.TryParse(displayOption.Attribute("val").Value, out option) || !Enum.IsDefined(typeof(PossNameType), option))
					{
						displayOption.SetAttributeValue("val", "0");
						DataMigrationServices.UpdateDTO(repoDto, list, listElement.ToString());
					}
				}
				var preventChoiceAbove = listElement.Elements("PreventChoiceAboveLevel").FirstOrDefault();
				if (preventChoiceAbove != null)
				{
					int preventChoiceAboveVal = -1;
					if (!int.TryParse(preventChoiceAbove.Attribute("val").Value, out preventChoiceAboveVal) || preventChoiceAboveVal < 0)
					{
						preventChoiceAbove.SetAttributeValue("val", "0");
						DataMigrationServices.UpdateDTO(repoDto, list, listElement.ToString());
					}
				}
				foreach (var listTitle in listTitles)
				{
					var wsAttrValue = listTitle.Attribute("ws");
					if (wsAttrValue == null)
						continue;
					var key = new Tuple<string, string>(wsAttrValue.Value, listTitle.Value);

					if (string.IsNullOrEmpty(key.Item2)) // If there is no actual name for a writing system ignore it
						continue;
					if (namesAndLists.ContainsKey(key))
					{
						duplicates.Add(new Tuple<DomainObjectDTO, DomainObjectDTO>(namesAndLists[key], list));
					}
					else
					{
						namesAndLists.Add(key, list);
					}
				}
			}
			// This code assumes that one of the duplicates is a custom list which has no ownerguid, the other created by a DM must be owned by LexDb
			foreach (var duplicate in duplicates)
			{
				var listOne = XElement.Parse(duplicate.Item1.Xml);
				var listTwo = XElement.Parse(duplicate.Item2.Xml);
				if (listOne.Attribute("ownerguid") != null)
				{
					AppendCustomToNamesAndUpdate(repoDto, duplicate.Item2, listTwo);
				}
				else
				{
					AppendCustomToNamesAndUpdate(repoDto, duplicate.Item1, listOne);
				}
			}
		}

		private void AppendCustomToNamesAndUpdate(IDomainObjectDTORepository repoDto, DomainObjectDTO dto, XElement dtoXml)
		{
			var names = dtoXml.Elements("Name");
			foreach (var titleElement in names.Select(name => name.Element("AUni")).Where(titleElement => titleElement != null))
			{
				titleElement.Value = titleElement.Value + "-Custom";
			}
			DataMigrationServices.UpdateDTO(repoDto, dto, dtoXml.ToString());
		}

		private void CleanOutBadRefTypes(IDomainObjectDTORepository repoDto)
		{
			const string unspecComplexEntryTypeGuid = "fec038ed-6a8c-4fa5-bc96-a4f515a98c50";
			const string unspecVariantEntryTypeGuid = "3942addb-99fd-43e9-ab7d-99025ceb0d4e";

			foreach (var dto in repoDto.AllInstancesWithSubclasses("LexEntryRef"))
			{
				var data = XElement.Parse(dto.Xml);
				var refTypeElt = data.Element("RefType");
				var varientEntryTypeElt = data.Element("VariantEntryTypes");
				var complexEntryTypeElt = data.Element("ComplexEntryTypes");
				if (refTypeElt.FirstAttribute.Value == LexEntryRefTags.krtComplexForm.ToString() && varientEntryTypeElt != null)
				{
					varientEntryTypeElt.Remove();
					DataMigrationServices.UpdateDTO(repoDto, dto, data.ToString());
				}
				else if (refTypeElt.FirstAttribute.Value == LexEntryRefTags.krtVariant.ToString() && complexEntryTypeElt != null)
				{
					complexEntryTypeElt.Remove();
					DataMigrationServices.UpdateDTO(repoDto, dto, data.ToString());
				}
				// Re-do the DM69 bit correctly if necessary
				if (refTypeElt.FirstAttribute.Value == LexEntryRefTags.krtComplexForm.ToString() && complexEntryTypeElt == null)
				{
					DataMigration7000069.AddRefType(data, repoDto, dto, "ComplexEntryTypes", unspecComplexEntryTypeGuid, false);
				}
				else if (refTypeElt.FirstAttribute.Value == LexEntryRefTags.krtVariant.ToString() && varientEntryTypeElt == null)
				{
					DataMigration7000069.AddRefType(data, repoDto, dto, "VariantEntryTypes", unspecVariantEntryTypeGuid, false);
				}
			}
		}
	}
}