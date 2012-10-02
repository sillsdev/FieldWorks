using System;
using System.Linq;
using System.Text;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	class DataMigration7000009 : IDataMigration
	{
		private static readonly byte[] owningFlidTag = Encoding.UTF8.GetBytes(" owningflid=\"");
		private static readonly byte closeQuote =  owningFlidTag.Last();
		private static readonly byte[] owningOrdTag = Encoding.UTF8.GetBytes(" owningord=\"");

		/// <summary>
		/// Remove the owningFlid attribute from every "rt" element.
		/// </summary>
		/// <param name="domainObjectDtoRepository"></param>
		public void PerformMigration(IDomainObjectDTORepository domainObjectDtoRepository)
		{
			DataMigrationServices.CheckVersionNumber(domainObjectDtoRepository, 7000008);

			foreach (var dto in domainObjectDtoRepository.AllInstancesWithValidClasses())
			{
				byte[] contents = dto.XmlBytes;
				int index = contents.IndexOfSubArray(owningFlidTag);
				if (index >= 0)
				{
					contents = contents.ReplaceSubArray(index,
						Array.IndexOf(contents, closeQuote, index + owningFlidTag.Length) - index + 1,
						new byte[0]);
				}
				int index2 = contents.IndexOfSubArray(owningOrdTag);
				if (index2 >= 0)
				{
					contents = contents.ReplaceSubArray(index2,
						Array.IndexOf(contents, closeQuote, index2 + owningOrdTag.Length) - index2 + 1,
						new byte[0]);
				}
				if (index >= 0 || index2 >= 0)
				{
					DataMigrationServices.UpdateDTO(domainObjectDtoRepository, dto, contents);
				}
			}
			DataMigrationServices.IncrementVersionNumber(domainObjectDtoRepository);
		}
	}
}
