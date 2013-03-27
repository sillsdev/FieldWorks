using System;
using System.Linq;
using System.Xml.Linq;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Migrate data from 7000051 to 7000052.
	///
	/// Change the LangProject Guid to really be unique between different projects,
	/// so Lift Bridge is happy.
	/// </summary>
	/// <remarks>
	/// Actually, this DM will try to delete the old one and
	/// </remarks>
	/// ----------------------------------------------------------------------------------------
	internal class DataMigration7000052 : IDataMigration
	{
		#region IDataMigration Members

		public void PerformMigration(IDomainObjectDTORepository domainObjectDtoRepository)
		{
			DataMigrationServices.CheckVersionNumber(domainObjectDtoRepository, 7000051);

			var newGuidValue = Guid.NewGuid().ToString().ToLowerInvariant();
			const string className = "WfiMorphBundle";
			var wmbList = domainObjectDtoRepository.AllInstancesSansSubclasses(className);
			foreach (var wmb in wmbList)
			{
				XElement wmbElt = XElement.Parse(wmb.Xml);
				var morphElt = wmbElt.Element("Morph");
				// if we don't have a morph reference,
				// then there's nothing to copy into the form field.
				if (morphElt == null)
					continue;
				var objsurElt = morphElt.Element("objsur");
				var dtoMorphTarget = domainObjectDtoRepository.GetDTO(objsurElt.Attribute("guid").Value);
				// for each form alternative, copy the writing system

				// if for some reason there is a morphbundle form that already exists, delete it before inserting another.
				var formElt = wmbElt.Element("Form");
				if (formElt != null)
					formElt.Remove();

				var morphTargetElt = XElement.Parse(dtoMorphTarget.Xml);
				var morphTargetFormElt = morphTargetElt.Element("Form");
				if (morphTargetFormElt == null)
					continue;
				formElt = XElement.Parse("<Form/>");
				wmbElt.AddFirst(formElt);
				foreach (var aUniElt in morphTargetFormElt.Elements("AUni"))
				{
					string ws = aUniElt.Attribute("ws").Value;
					string form = aUniElt.Value;
					var newFormAltElt = XElement.Parse(String.Format("<AStr ws=\"{0}\"/>", ws));
					formElt.Add(newFormAltElt);
					var newRunElt = XElement.Parse(String.Format("<Run ws=\"{0}\">{1}</Run>", ws, XmlUtils.MakeSafeXml(form)));
					newFormAltElt.Add(newRunElt);
				}
				DataMigrationServices.UpdateDTO(domainObjectDtoRepository, wmb, wmbElt.ToString());
			}

			DataMigrationServices.IncrementVersionNumber(domainObjectDtoRepository);
		}
		#endregion
	}
}
