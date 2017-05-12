// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// <summary>
	/// Migrates from 7000010 to 7000011
	/// </summary>
	internal class DataMigration7000011 : IDataMigration
	{
		/// <summary>
		/// Updates the Data Notebook RecordTypes possibilities to use specific GUIDs.
		/// </summary>
		/// <param name="domainObjectDtoRepository">Repository of all CmObject DTOs available for one migration step.</param>
		public void PerformMigration(IDomainObjectDTORepository domainObjectDtoRepository)
		{
			DataMigrationServices.CheckVersionNumber(domainObjectDtoRepository, 7000010);

			DomainObjectDTO nbkDto = domainObjectDtoRepository.AllInstancesSansSubclasses("RnResearchNbk").First();
			XElement nbkElem = XElement.Parse(nbkDto.Xml);
			var recTypesGuid = (string) nbkElem.XPathSelectElement("RnResearchNbk/RecTypes/objsur").Attribute("guid");
			var stack = new Stack<DomainObjectDTO>(domainObjectDtoRepository.GetDirectlyOwnedDTOs(recTypesGuid));
			IEnumerable<DomainObjectDTO> recDtos = domainObjectDtoRepository.AllInstancesSansSubclasses("RnGenericRec");
			IEnumerable<DomainObjectDTO> overlayDtos = domainObjectDtoRepository.AllInstancesSansSubclasses("CmOverlay");
			while (stack.Count > 0)
			{
				DomainObjectDTO dto = stack.Pop();
				foreach (DomainObjectDTO childDto in domainObjectDtoRepository.GetDirectlyOwnedDTOs(dto.Guid))
					stack.Push(childDto);
				XElement posElem = XElement.Parse(dto.Xml);
				XElement uniElem = posElem.XPathSelectElement("CmPossibility/Abbreviation/AUni[@ws='en']");
				if (uniElem != null)
				{
					string newGuid = null;
					switch (uniElem.Value)
					{
						case "Con":
							newGuid = "B7B37B86-EA5E-11DE-80E9-0013722F8DEC";
							break;
						case "Intv":
							newGuid = "B7BF673E-EA5E-11DE-9C4D-0013722F8DEC";
							break;
						case "Str":
							newGuid = "B7C8F092-EA5E-11DE-8D7D-0013722F8DEC";
							break;
						case "Uns":
							newGuid = "B7D4DC4A-EA5E-11DE-867C-0013722F8DEC";
							break;
						case "Lit":
							newGuid = "B7E0C7F8-EA5E-11DE-82CC-0013722F8DEC";
							break;
						case "Obs":
							newGuid = "B7EA5156-EA5E-11DE-9F9C-0013722F8DEC";
							break;
						case "Per":
							newGuid = "B7F63D0E-EA5E-11DE-9F02-0013722F8DEC";
							break;
						case "Ana":
							newGuid = "82290763-1633-4998-8317-0EC3F5027FBD";
							break;
					}
					if (newGuid != null)
						DataMigrationServices.ChangeGuid(domainObjectDtoRepository, dto, newGuid, recDtos.Concat(overlayDtos));
				}
			}

			DomainObjectDTO recTypesDto = domainObjectDtoRepository.GetDTO(recTypesGuid);
			DataMigrationServices.ChangeGuid(domainObjectDtoRepository, recTypesDto, "D9D55B12-EA5E-11DE-95EF-0013722F8DEC", overlayDtos);
			DataMigrationServices.IncrementVersionNumber(domainObjectDtoRepository);
		}
	}
}
