// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// 1) Fix the Restrictions field for both LexEntry and LexSense to be MultiString (AStr)
	/// instead of MultiUnicode (AUni).
	///
	/// 2) Add a MultiString (AUni) ReverseName field to LexEntryType, both Variant Type and
	/// Complex Form Type. The migration fills in the 'en' ws contents with Name.Value + " of".
	/// Swap Abbreviation and ReverseAbbr fields. By swapping the fields the "of" will be switched
	/// to the other reference.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DataMigration7000069 : IDataMigration
	{
		public void PerformMigration(IDomainObjectDTORepository repoDto)
		{
			DataMigrationServices.CheckVersionNumber(repoDto, 7000068);

			UpdateRestrictions(repoDto);
			AddReverseNameAndSwapAbbreviationFields(repoDto);

			DataMigrationServices.IncrementVersionNumber(repoDto);
		}

		// We update every instance of a Restriction in a LexEntry or a LexSense to change from AUni
		// to AStr.
		private void UpdateRestrictions(IDomainObjectDTORepository repoDto)
		{
			foreach (var dto in repoDto.AllInstancesSansSubclasses("LexEntry").Union(repoDto.AllInstancesSansSubclasses("LexSense")))
			{
				var changed = false;
				XElement data = XElement.Parse(dto.Xml);
				foreach (var elt in data.XPathSelectElements("//Restrictions/AUni"))
				{
					elt.Name = "AStr";
					var restrictionData = elt.Value;
					elt.Value = string.Empty;
					var wsAttr = elt.Attribute("ws");
					var runElt = new XElement("Run");
					runElt.Value = restrictionData;
					runElt.SetAttributeValue("ws", wsAttr.Value);
					elt.Add(runElt);
					changed = true;
				}
				if (changed)
				{
					DataMigrationServices.UpdateDTO(repoDto, dto, data.ToString());
				}
			}
		}

		private void AddReverseNameAndSwapAbbreviationFields(IDomainObjectDTORepository repoDto)
		{
			foreach (var dto in repoDto.AllInstancesSansSubclasses("LexEntryType"))
			{
				XElement data = XElement.Parse(dto.Xml);

				var nameElt = data.Element("Name");
				if (nameElt != null)
				{
					var nameAUniElt = nameElt.Elements("AUni").FirstOrDefault(elt => elt.Attribute("ws").Value == "en");
					if (nameAUniElt != null)
					{
						var revNameElt = new XElement("ReverseName");
						var auni = new XElement("AUni");
						auni.SetAttributeValue("ws", "en");
						auni.Value = nameAUniElt.Value + " of";
						revNameElt.Add(auni);
						data.Add(revNameElt);
					}
				}

				var abbrevElt = data.Element("Abbreviation");
				var revAbbrElt = data.Element("ReverseAbbr");
				if(abbrevElt != null)
					abbrevElt.Name = "ReverseAbbr";
				if(revAbbrElt != null)
					revAbbrElt.Name = "Abbreviation";

				DataMigrationServices.UpdateDTO(repoDto, dto, data.ToString());
			}
		}
	}
}
