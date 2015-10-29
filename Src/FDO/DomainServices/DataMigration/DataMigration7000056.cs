// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Migrate data from 7000055 to 7000056.
	///
	/// Change the LangProject Guid to really be unique between different projects,
	/// so Lift Bridge is happy.
	/// </summary>
	/// <remarks>
	/// Actually, this DM will try to delete the old one and
	/// </remarks>
	/// ----------------------------------------------------------------------------------------
	internal class DataMigration7000056 : IDataMigration
	{
		#region IDataMigration Members

		public void PerformMigration(IDomainObjectDTORepository domainObjectDtoRepository)
		{
			DataMigrationServices.CheckVersionNumber(domainObjectDtoRepository, 7000055);

			const string sPossibilityListGuid = "e491e5d5-d569-41f2-a9c1-23c048d55d59";
			const string className = "PhPhonData";
			var wmbList = domainObjectDtoRepository.AllInstancesSansSubclasses(className);
			var wmbPhonData = wmbList.FirstOrDefault();
			if (wmbPhonData == null)
			{ // somehow, this project does not have a PhPhonData object... add it
				const string sPhPhonDataGuid = "be765e3e-ea5e-11de-9d42-0013722f8dec";
				var wmbLangProjList = domainObjectDtoRepository.AllInstancesSansSubclasses("LangProj");
				var wmbLangProj = wmbLangProjList.First();
				var wmbLangProjElt = XElement.Parse(wmbLangProj.Xml);
				wmbLangProjElt.Add(new XElement("PhonologicalData",
								   new XElement("objsur",
												new XAttribute("guid", sPhPhonDataGuid),
												new XAttribute("t", "o"))));
				DataMigrationServices.UpdateDTO(domainObjectDtoRepository, wmbLangProj, wmbLangProjElt.ToString());
				// create new PhPhonData object
				var newPhPhonDataElt = new XElement("PhPhonData",
													new XAttribute("guid", sPhPhonDataGuid),
													new XAttribute("ownerguid", wmbLangProj.Guid));
				wmbPhonData = new DomainObjectDTO(sPhPhonDataGuid, "PhPhonData", newPhPhonDataElt.ToString());
				domainObjectDtoRepository.Add(wmbPhonData);
			}
			XElement wmbPhonDataElt = XElement.Parse(wmbPhonData.Xml);
			var wmbPhonDataGuid = wmbPhonDataElt.Attribute("guid").Value;

			// add phon rule feats contents
			wmbPhonDataElt.Add(new XElement("PhonRuleFeats",
											new XElement("objsur",
														 new XAttribute("guid", sPossibilityListGuid),
														 new XAttribute("t", "o"))));
			DataMigrationServices.UpdateDTO(domainObjectDtoRepository, wmbPhonData, wmbPhonDataElt.ToString());

			// create phon rule feats' possibility list
			var sb = new StringBuilder();
			sb.AppendFormat("<rt class=\"CmPossibilityList\" guid=\"{0}\" ownerguid=\"{1}\">", sPossibilityListGuid,
							wmbPhonDataGuid);
			sb.Append("<DateCreated val=\"2012-3-30 18:48:18.679\" />");
			sb.Append("<DateModified val=\"2012-3-30 18:48:18.679\" />");
			sb.Append("<Depth val=\"1\" />");
			sb.Append("<DisplayOption val=\"0\" />");
			sb.Append("<IsClosed val=\"False\" />");
			sb.Append("<IsSorted val=\"True\" />");
			sb.Append("<IsVernacular val=\"False\" />");
			sb.Append("<ItemClsid val=\"7\" />");
			sb.Append("<Name>");
			sb.Append("<AUni ws=\"en\">Phonological Rule Features</AUni>");
			sb.Append("</Name>");
			sb.Append("<Possibilities>");
			sb.Append("</Possibilities>");
			sb.Append("<PreventChoiceAboveLevel val=\"0\" />");
			sb.Append("<PreventDuplicates val=\"False\" />");
			sb.Append("<PreventNodeChoices val=\"False\" />");
			sb.Append("<UseExtendedFields val=\"False\" />");
			sb.Append("<WsSelector val=\"0\" />");
			sb.Append("</rt>");
			var newCmPossibilityListElt = XElement.Parse(sb.ToString());
			var dtoCmPossibilityList = new DomainObjectDTO(sPossibilityListGuid, "CmPossibilityList", newCmPossibilityListElt.ToString());
			domainObjectDtoRepository.Add(dtoCmPossibilityList);

			DataMigrationServices.IncrementVersionNumber(domainObjectDtoRepository);
		}

		#endregion
	}
}
