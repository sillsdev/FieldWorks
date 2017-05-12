// Copyright (c) 2011-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DataMigration7000035.cs
// Responsibility: FW team

using System.Xml.Linq;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Migrate data from 7000034 to 7000035.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DataMigration7000035 : IDataMigration
	{
		#region IDataMigration Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Change any bogus non-owning footnote links in the vernacular (but NOT in the BT)
		/// to be owning links
		/// </summary>
		/// <param name="domainObjectDtoRepository">
		/// Repository of all CmObject DTOs available for one migration step.
		/// </param>
		/// ------------------------------------------------------------------------------------
		public void PerformMigration(IDomainObjectDTORepository domainObjectDtoRepository)
		{
			DataMigrationServices.CheckVersionNumber(domainObjectDtoRepository, 7000034);

			foreach (var scrTxtPara in domainObjectDtoRepository.AllInstancesSansSubclasses("ScrTxtPara"))
			{
				XElement para = XElement.Parse(scrTxtPara.Xml);
				XElement contents = para.Element("Contents");
				if (contents == null)
					continue;
				XElement str = contents.Element("Str");
				if (str == null)
					continue;

				foreach (XElement run in str.Elements("Run"))
				{
					XAttribute linkAttr = run.Attribute("link");
					if (linkAttr == null)
						continue; // Run doesn't contain an unowned hot-link
					XAttribute ownedLinkAttr = new XAttribute("ownlink", linkAttr.Value);
					run.Add(ownedLinkAttr);
					linkAttr.Remove();
					DataMigrationServices.UpdateDTO(domainObjectDtoRepository, scrTxtPara, para.ToString());
				}
			}

			DataMigrationServices.IncrementVersionNumber(domainObjectDtoRepository);
		}

		#endregion
	}
}
