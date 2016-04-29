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
	/// Fix the Restrictions field for both LexEntry and LexSense to be MultiString (AStr)
	/// instead of MultiUnicode (AUni).
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DataMigration7000069 : IDataMigration
	{
		public void PerformMigration(IDomainObjectDTORepository repoDto)
		{
			DataMigrationServices.CheckVersionNumber(repoDto, 7000068);

			UpdateTags(repoDto);

			DataMigrationServices.IncrementVersionNumber(repoDto);
		}

		// We update every instance of a Restriction in a LexEntry or a LexSense to change from AUni
		// to AStr.
		private void UpdateTags(IDomainObjectDTORepository repoDto)
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
	}
}
