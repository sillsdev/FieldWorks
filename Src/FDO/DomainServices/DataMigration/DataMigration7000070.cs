// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;

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

			DataMigrationServices.IncrementVersionNumber(repoDto);
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
