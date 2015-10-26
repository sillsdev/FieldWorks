// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// 1. Do the new styles guids properly.
	/// 	A. Remove any unowned StStyle instances, since 38014 didn't remove the old ones or add the new ones to respective owners.
	/// 	B. Reset the "Version" properties to the respective fooStyles.xml files, *before* changeset 38014.
	/// 		This is for CmResource instances with names of "TeStyles" and "FlexStyles", if they are in the data set.
	/// 2. Run Delint method to clean up everything. (Includes removing any new zombies from Step 2.)
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DataMigration7000062 : IDataMigration
	{
		#region Implementation of IDataMigration

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Perform one increment migration step.
		/// </summary>
		/// <param name="domainObjectDtoRepository">
		/// Repository of all CmObject DTOs available for one migration step.
		/// </param>
		/// <remarks>
		/// The method must add/remove/update the DTOs to the repository,
		/// as it adds/removes objects as part of it work.
		///
		/// Implementors of this interface should ensure the Repository's
		/// starting model version number is correct for the step.
		/// Implementors must also increment the Repository's model version number
		/// at the end of its migration work.
		///
		/// The method also should normally modify the xml string(s)
		/// of relevant DTOs, since that string will be used by the main
		/// data migration calling client (ie. BEP).
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public void PerformMigration(IDomainObjectDTORepository domainObjectDtoRepository)
		{
			DataMigrationServices.CheckVersionNumber(domainObjectDtoRepository, 7000061);

			// Step 1.
			foreach (var resourceDto in domainObjectDtoRepository.AllInstancesSansSubclasses("CmResource"))
			{
				var resourceElement = XElement.Parse(resourceDto.Xml);
				var resourceNameElement = resourceElement.Element("Name");
				if (resourceNameElement == null)
					continue;
				var uniElement = resourceNameElement.Element("Uni");
				if (uniElement == null)
					continue;
				string oldVersion;
				switch (uniElement.Value)
				{
					case "TeStyles":
						oldVersion = "700176e1-4f42-4abd-8fb5-3c586670085d";
						break;
					case "FlexStyles":
						oldVersion = "13c213b9-e409-41fc-8782-7ca0ee983b2c";
						break;
					default:
						continue;
				}
				var versionElement = resourceElement.Element("Version");
				if (versionElement == null)
				{
					resourceElement.Add(new XElement("Version", new XAttribute("val", oldVersion)));
				}
				else
				{
					versionElement.Attribute("val").Value = oldVersion;
				}
				resourceDto.Xml = resourceElement.ToString();
				domainObjectDtoRepository.Update(resourceDto);
			}

			// Step 2.
			DataMigrationServices.Delint(domainObjectDtoRepository);

			DataMigrationServices.IncrementVersionNumber(domainObjectDtoRepository);
		}

		#endregion
	}
}