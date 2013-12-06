// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DataMigration7000037.cs
// Responsibility: mcconnel
// ---------------------------------------------------------------------------------------------
using System.Linq;
using System.Xml.Linq;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Migrate data from 7000036 to 7000037.  This fixes a data conversion problem for
	/// externalLink attributes in Run elements coming from FieldWorks 6.0 into FieldWorks 7.0.
	/// See FWR-782 and FWR-3364 for motivation.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DataMigration7000040 : IDataMigration
	{

		#region IDataMigration Members

		public void PerformMigration(IDomainObjectDTORepository domainObjectDtoRepository)
		{
			DataMigrationServices.CheckVersionNumber(domainObjectDtoRepository, 7000039);

			foreach (var dto in domainObjectDtoRepository.AllInstancesSansSubclasses("LexEntryRef"))
			{
				var xElt = XElement.Parse(dto.Xml);
				var primaryLexemes = xElt.Element("PrimaryLexemes");
				if (primaryLexemes == null || primaryLexemes.Elements().Count() == 0)
					continue;
				var newElt = new XElement("ShowComplexFormsIn");
				foreach (var child in primaryLexemes.Elements())
				{
					newElt.Add(new XElement(child)); // clone all the objsur elements.
				}
				xElt.Add(newElt);
				dto.Xml = xElt.ToString();
				domainObjectDtoRepository.Update(dto);
			}

			DataMigrationServices.IncrementVersionNumber(domainObjectDtoRepository);
		}

		#endregion
	}
}
