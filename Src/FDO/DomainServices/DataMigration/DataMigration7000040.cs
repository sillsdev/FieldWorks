// Copyright (c) 2011-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DataMigration7000037.cs
// Responsibility: mcconnel

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
