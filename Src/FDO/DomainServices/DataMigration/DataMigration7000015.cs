using System.Xml.Linq;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Migrates from 7000014 to 7000015
	/// </summary>
	/// ------------------------------------------------------------------------------------
	internal class DataMigration7000015 : IDataMigration
	{
		#region Implementation of IDataMigration

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Perform one increment migration step.
		///
		/// In this case, the migration is not related to a model change,
		/// but is a simple data change that removes the class elements in the xml.
		/// The end resujlt xml will have the top-level 'rt' element and zero, or more,
		/// property level elements.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void PerformMigration(IDomainObjectDTORepository domainObjectDtoRepository)
		{
			DataMigrationServices.CheckVersionNumber(domainObjectDtoRepository, 7000014);

			// No. We need to convert all instances, even if they are no longer part of the model.
			// DM19, for example, removes LgWritingSystem instances and the class form the model,
			// but it tries to access the data as if it had been properly processed by DM15,
			// *but* this code would leave out LgWritingSystem instnaces form being processed here.
			//foreach (var dto in domainObjectDtoRepository.AllInstancesWithSubclasses("CmObject"))
			foreach (var dto in domainObjectDtoRepository.AllInstances())
			{
				var rtElement = XElement.Parse(dto.Xml);
				// Removes all current child nodes (class level),
				// and replaces them with the old property nodes (if any).
#if !__MonoCS__
				rtElement.ReplaceNodes(rtElement.Elements().Elements());
				dto.Xml = rtElement.ToString();
#else // FWNX-165: work around mono bug https://bugzilla.novell.com/show_bug.cgi?id=592435
				var copy = new XElement(rtElement);
				copy.ReplaceNodes(rtElement.Elements().Elements());
				dto.Xml = copy.ToString();
#endif
				domainObjectDtoRepository.Update(dto);
			}

			DataMigrationServices.IncrementVersionNumber(domainObjectDtoRepository);
		}

		#endregion
	}
}
