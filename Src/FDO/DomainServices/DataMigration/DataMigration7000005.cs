// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DataMigration7000005.cs
// Responsibility: Bush
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;
using System.Xml.XPath;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Migrate data from 7000004 to 7000005.
	///
	/// Change model for Data Notebook to combine RnEvent and RnAnalysis into RnGenericRec
	/// </summary>
	///
	/// <remarks>
	/// This migration needs to:
	///		1. Move the attributes that are in RnEvent into RnGenericRec.
	///		2. Move the attributes that are in RnAnalysis into RnGenericRec.
	///		3. Delete the Rn Event and RnAnalysis Classes.
	/// </remarks>
	/// ----------------------------------------------------------------------------------------
	internal class DataMigration7000005 : IDataMigration
	{
		#region IDataMigration Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Change model for Data Notebook to combine the RnEvent and RnAnalysis classes
		/// into RnGenericRec
		/// </summary>
		/// <param name="domainObjectDtoRepository">
		/// Repository of all CmObject DTOs available for one migration step.
		/// </param>
		/// <remarks>
		/// The method must add/remove/update the DTOs to the repository,
		/// as it adds/removes objects as part of its work.
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
			DataMigrationServices.CheckVersionNumber(domainObjectDtoRepository, 7000004);

			// 1. Change EventTypes field to RecTypes
			var nbkDto = domainObjectDtoRepository.AllInstancesSansSubclasses("RnResearchNbk").First();
			var nbkElement = XElement.Parse(nbkDto.Xml);
			var nbkClsElement = nbkElement.Element("RnResearchNbk");
			var typesFieldElement = nbkClsElement.Element("EventTypes");
			var typesSurElement = typesFieldElement.Element("objsur");
			nbkClsElement.Add(new XElement("RecTypes", typesSurElement));
			typesFieldElement.Remove();
			DataMigrationServices.UpdateDTO(domainObjectDtoRepository, nbkDto, nbkElement.ToString());

			// 2. Add Analysis possibility to Record Types list
			var typesGuid = typesSurElement.Attribute("guid").Value;
			var typesDto = domainObjectDtoRepository.GetDTO(typesGuid);
			var typesElement = XElement.Parse(typesDto.Xml);
			typesElement.XPathSelectElement("CmMajorObject/Name/AUni[@ws='en']").SetValue("Entry Types");
			var posElement = typesElement.XPathSelectElement("CmPossibilityList/Possibilities");
			posElement.Add(
				DataMigrationServices.CreateOwningObjSurElement("82290763-1633-4998-8317-0EC3F5027FBD"));
			DataMigrationServices.UpdateDTO(domainObjectDtoRepository, typesDto,
				typesElement.ToString());
			var ord = posElement.Elements().Count();

			var nowStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
			var typeElement = new XElement("rt", new XAttribute("class", "CmPossibility"),
				new XAttribute("guid", "82290763-1633-4998-8317-0EC3F5027FBD"),
				new XAttribute("ownerguid", typesGuid),
				new XAttribute("owningflid", "8008"), new XAttribute("owningord", ord.ToString()),
				new XElement("CmObject"),
				new XElement("CmPossibility",
					new XElement("Abbreviation",
						new XElement("AUni", new XAttribute("ws", "en"), "Ana")),
					new XElement("BackColor", new XAttribute("val", "16711680")),
					new XElement("DateCreated", new XAttribute("val", nowStr)),
					new XElement("DateModified", new XAttribute("val", nowStr)),
					new XElement("Description",
						new XElement("AStr", new XAttribute("ws", "en"),
							new XElement("Run", new XAttribute("ws", "en"), "Reflection on events and other types of data, such as literature summaries or interviews. An analysis does not add data; it interprets and organizes data. An analysis entry may synthesize emerging themes. It may draw connections between observations. It is a place to speculate and hypothesize, or document moments of discovery and awareness. Analytic notes can be turned into articles. Or, they may just be steps on the stairway toward greater understanding."))),
					new XElement("ForeColor", new XAttribute("val", "16777215")),
					new XElement("Name",
						new XElement("AUni", new XAttribute("ws", "en"), "Analysis")),
					new XElement("UnderColor", new XAttribute("val", "255")),
					new XElement("UnderStyle", new XAttribute("val", "1"))));
			domainObjectDtoRepository.Add(new DomainObjectDTO("82290763-1633-4998-8317-0EC3F5027FBD",
				"CmPossibility", typeElement.ToString()));

			// 3. Move the attributes that are in RnEvent and RnAnalysis into RnGenericRec.
			MigrateSubclassOfGenRec(domainObjectDtoRepository, "RnEvent");
			MigrateSubclassOfGenRec(domainObjectDtoRepository, "RnAnalysis");

			DataMigrationServices.IncrementVersionNumber(domainObjectDtoRepository);
		}

		private static void MigrateSubclassOfGenRec(IDomainObjectDTORepository domainObjectDtoRepository,
			string subclassName)
		{
			// We need a copy of the collection because we will remove items from it during the loop.
			foreach (var GenRecDto in domainObjectDtoRepository.AllInstancesSansSubclasses(subclassName).ToArray())
			{
				var rtElement = XElement.Parse(GenRecDto.Xml);
				var clsElement = rtElement.Element(GenRecDto.Classname);
				var recElement = rtElement.Element("RnGenericRec");
				recElement.Add(clsElement.Elements());
				clsElement.Remove();
				rtElement.Attribute("class").Value = "RnGenericRec";
				if (GenRecDto.Classname == "RnAnalysis")
				{
					recElement.Add(new XElement("Type",
						DataMigrationServices.CreateReferenceObjSurElement("82290763-1633-4998-8317-0EC3F5027FBD")));
				}
				String oldClassName = GenRecDto.Classname;
				GenRecDto.Classname = "RnGenericRec";
				DataMigrationServices.UpdateDTO(domainObjectDtoRepository, GenRecDto, rtElement.ToString(), oldClassName);
			}
		}

		#endregion
	}
}
