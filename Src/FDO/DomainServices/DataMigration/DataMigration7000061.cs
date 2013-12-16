// Copyright (c) 2012-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DataMigration7000061.cs
// Responsibility: RandyR

using System.Linq;
using System.Xml.Linq;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// 1. Do the new styles guids properly.
	/// 	A. Remove any unowned StStyle instances, since 38014 didn't remove the old ones or add the new ones to respective owners.
	/// 	B. Reset the "Version" properties to the respective fooStyles.xml files, *before* changeset 38014.
	/// 		This is for CmResource instances with names of "TeStyles" and "FlexStyles", if they are in the data set.
	/// 2. Remove all but the first "objsur" elements in all atomic owning and reference properties.
	///		(Skip, since Delint call will remove the new 'zombies'. Also remove all corresponding owned objects.)
	///		This is to fix bad FW 6.0 data.
	/// 3. Remove unowned CmIndirectAnnotation and CmBaseAnnotation instances.
	/// 4. Run Delint method to clean up everything. (Includes removing any new zombies from Step 2.)
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DataMigration7000061 : IDataMigration
	{
		public void PerformMigration(IDomainObjectDTORepository repoDto)
		{
			DataMigrationServices.CheckVersionNumber(repoDto, 7000060);

			// Step 1.A. & 3.
			//var unownedGonerCandidates = new List<DomainObjectDTO>();
			//unownedGonerCandidates.AddRange(repoDto.AllInstancesSansSubclasses("CmBaseAnnotation"));
			//unownedGonerCandidates.AddRange(repoDto.AllInstancesSansSubclasses("CmIndirectAnnotation"));
			//unownedGonerCandidates.AddRange(repoDto.AllInstancesSansSubclasses("StStyle"));
			//unownedGonerCandidates.AddRange(repoDto.AllInstancesSansSubclasses("StText"));
			//var unownedGoners = new List<DomainObjectDTO>();
			//foreach (var domainObjectDto in unownedGonerCandidates)
			//{
			//    DomainObjectDTO ownerDto;
			//    repoDto.TryGetOwner(domainObjectDto.Guid, out ownerDto);
			//    if (ownerDto != null)
			//        continue;
			//    unownedGoners.Add(domainObjectDto);
			//}
			//foreach (var unownedGoner in unownedGoners)
			//{
			//    DataMigrationServices.RemoveIncludingOwnedObjects(repoDto, unownedGoner, false);
			//}

			// Step 1.B.
			foreach (var resourceDto in repoDto.AllInstancesSansSubclasses("CmResource"))
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
				repoDto.Update(resourceDto);
			}

			// Step 2.
			var mdc = repoDto.MDC;
			foreach (var clid in mdc.GetClassIds())
			{
				if (mdc.GetAbstract(clid))
					continue;

				var className = mdc.GetClassName(clid);
				foreach (var atomicPropId in mdc.GetFields(clid, true, (int)CellarPropertyTypeFilter.AllAtomic))
				{
					var propName = mdc.GetFieldName(atomicPropId);
					var isCustomProperty = mdc.IsCustom(atomicPropId);
					foreach (var dto in repoDto.AllInstancesSansSubclasses(className))
					{
						var element = XElement.Parse(dto.Xml);
						var propElement = isCustomProperty
											  ? (element.Elements("Custom").Where(customPropElement => customPropElement.Attribute("name").Value == propName)).FirstOrDefault()
											  : element.Element(propName);
						if (propElement == null || !propElement.HasElements)
							continue;

						var objsurElements = propElement.Elements("objsur").ToList();
						if (objsurElements.Count <= 1)
							continue;

						// Remove all but first one of them.
						propElement.RemoveNodes();
						propElement.Add(objsurElements[0]);
						dto.Xml = element.ToString();
						repoDto.Update(dto);
					}
				}
			}

			// Step 4.
			DataMigrationServices.Delint(repoDto);

			DataMigrationServices.IncrementVersionNumber(repoDto);
		}
	}
}
