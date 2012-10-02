using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Migrates from 7000012 to 7000013
	/// </summary>
	/// ------------------------------------------------------------------------------------
	internal class DataMigration7000013 : IDataMigration
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Changes Chart 'missing' markers from invalid ConstChartWordGroup references to
		/// ConstChartTag objects with null Tag property.
		/// </summary>
		/// <param name="domainObjectDtoRepository">Repository of all CmObject DTOs available for
		/// one migration step.</param>
		/// ------------------------------------------------------------------------------------
		public void PerformMigration(IDomainObjectDTORepository domainObjectDtoRepository)
		{
			DataMigrationServices.CheckVersionNumber(domainObjectDtoRepository, 7000012);

			// 1) Select the ConstChartWordGroup class objects.
			// 2) Convert any with null BeginSegment reference to ConstChartTag objects with null Tag reference.
			var objsToChangeClasses = new List<DomainObjectDTO>();
			foreach (var cellPartDto in domainObjectDtoRepository.AllInstancesSansSubclasses("ConstChartWordGroup"))
			{
				var rtElement = XElement.Parse(cellPartDto.Xml);

				// Only migrate the ones that have no BeginSegment
				var wordGrp = rtElement.Element("ConstChartWordGroup");
				if (wordGrp.Element("BeginSegment") != null)
					continue; // Don't migrate this one, it has a BeginSegment reference!

				// change its class value
				rtElement.Attribute("class").Value = "ConstChartTag";

				// Add the empty ConstChartTag element
				rtElement.Add(new XElement("ConstChartTag"));

				// Remove the old WordGroup element
				RemoveField(cellPartDto, rtElement, "ConstChartWordGroup");

				// Update the object XML
				cellPartDto.Xml = rtElement.ToString();
				cellPartDto.Classname = "ConstChartTag";
				objsToChangeClasses.Add(cellPartDto);
			}

			// Update the repository
			var startingStructure = new ClassStructureInfo("ConstituentChartCellPart", "ConstChartWordGroup");
			var endingStructure = new ClassStructureInfo("ConstituentChartCellPart", "ConstChartTag");
			foreach (var changedCellPart in objsToChangeClasses)
				domainObjectDtoRepository.Update(changedCellPart,
					startingStructure,
					endingStructure);

			DataMigrationServices.IncrementVersionNumber(domainObjectDtoRepository);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes the specified field.
		/// </summary>
		/// <param name="dto">The domain transfer object that has a field that may need to be deleted.</param>
		/// <param name="objElement">Name of the object containing fieldToDelete.</param>
		/// <param name="fieldToDelete">The name of the field to delete.</param>
		/// ------------------------------------------------------------------------------------
		private void RemoveField(DomainObjectDTO dto, XElement objElement, string fieldToDelete)
		{
			XElement rmElement = objElement.Element(fieldToDelete);
			if (rmElement != null)
				rmElement.Remove();
		}
	}
}
