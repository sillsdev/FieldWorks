// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Collections.Generic;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Migrate data from 7000056 to 7000057.
	///
	/// Data migration to change Irregularly Inflected Form variant types to class LexEntryInflType (for LT-7581).
	/// </summary>
	/// <remarks>
	/// Actually, this DM will try to delete the old one and
	/// </remarks>
	/// ----------------------------------------------------------------------------------------
	internal class DataMigration7000057 : IDataMigration
	{
		#region IDataMigration Members

		public void PerformMigration(IDomainObjectDTORepository domainObjectDtoRepository)
		{
			DataMigrationServices.CheckVersionNumber(domainObjectDtoRepository, 7000056);

			IList<string> irregularlyInflectedFormVariantTypeSystemGuids = new List<string>() {
				LexEntryTypeTags.kguidLexTypIrregInflectionVar.ToString(),
				LexEntryTypeTags.kguidLexTypPluralVar.ToString(),
				LexEntryTypeTags.kguidLexTypPastVar.ToString() };

			// first change the class of all the known systemGuids
			foreach (var systemGuid in irregularlyInflectedFormVariantTypeSystemGuids)
			{
				DomainObjectDTO dtoVariantType;
				// LT-13312 Note some projects may not have these guids.
				if (domainObjectDtoRepository.TryGetValue(systemGuid, out dtoVariantType))
				{
					ChangeClassOfOwnerAndChildren(domainObjectDtoRepository, dtoVariantType,
						LexEntryTypeTags.kClassName, LexEntryInflTypeTags.kClassName);
				}
			}
			DataMigrationServices.IncrementVersionNumber(domainObjectDtoRepository);
		}

		void ChangeClassOfOwnerAndChildren(IDomainObjectDTORepository dtoRepo, DomainObjectDTO dtoToChange, string oldClassname, string newClassname)
		{
			// bail out if we've already changed the class name (assume we've already changed its children too).
			if (!TryChangeOwnerClass(dtoRepo, dtoToChange, oldClassname, newClassname))
				return;
			foreach (var dtoChild in dtoRepo.GetDirectlyOwnedDTOs(dtoToChange.Guid))
			{
				ChangeClassOfOwnerAndChildren(dtoRepo, dtoChild, oldClassname, newClassname);
			}
		}

		bool TryChangeOwnerClass(IDomainObjectDTORepository dtoRepo, DomainObjectDTO dtoToChange, string oldClassname, string newClassname)
		{
			XElement dtoToChangeElt = XElement.Parse(dtoToChange.Xml);
			if (dtoToChangeElt.Attribute("class").Value != oldClassname)
				return false;
			dtoToChangeElt.Attribute("class").Value = newClassname;
			dtoToChange.Classname = newClassname;
			// next go through all the children of these known system variant types and change all of their children's classes.

			DataMigrationServices.UpdateDTO(dtoRepo, dtoToChange, dtoToChangeElt.ToString(), oldClassname);
			return true;
		}


		#endregion
	}
}
