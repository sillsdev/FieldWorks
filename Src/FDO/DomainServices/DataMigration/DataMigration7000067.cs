using System.Linq;
using System.Xml.Linq;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Migrate data from 7000066 to 7000067.
	///
	/// Remove all atributes from "Uni" element.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal sealed class DataMigration7000067 : IDataMigration
	{
		/// <summary>
		/// Remove all attributes from the "Uni"> element.
		/// </summary>
		/// <param name="domainObjectDtoRepository"></param>
		public void PerformMigration(IDomainObjectDTORepository domainObjectDtoRepository)
		{
			DataMigrationServices.CheckVersionNumber(domainObjectDtoRepository, 7000066);

			foreach (var dto in domainObjectDtoRepository.AllInstances())
			{
				var element = XElement.Parse(dto.Xml);
				var uniElementsWithAttrs = element.Elements().Elements("Uni").Where(uniElement => uniElement.HasAttributes).ToList();
				if (uniElementsWithAttrs.Count == 0)
					continue;
				foreach (var uniElementWithAttrs in uniElementsWithAttrs)
				{
					uniElementWithAttrs.Attributes().Remove();
					dto.Xml = element.ToString();
					domainObjectDtoRepository.Update(dto);
				}
			}

			DataMigrationServices.IncrementVersionNumber(domainObjectDtoRepository);
		}
	}
}
