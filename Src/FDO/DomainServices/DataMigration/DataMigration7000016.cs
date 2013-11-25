// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DataMigration7000016.cs
// Responsibility: mcconnel
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Text;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Migrates from 7000015 to 7000016
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DataMigration7000016 : IDataMigration
	{
		// GUID of the Record Types list.
		readonly string ksguidRecTypesList = RnResearchNbkTags.kguidRecTypesList.ToString("D").ToLowerInvariant();
		// GUIDs of newly created record types, which will all be owned by the list
		readonly string ksguidEvent = RnResearchNbkTags.kguidRecEvent.ToString("D").ToLowerInvariant();
		readonly string ksguidMethodology = RnResearchNbkTags.kguidRecMethodology.ToString("D").ToLowerInvariant();
		readonly string ksguidWeather = RnResearchNbkTags.kguidRecWeather.ToString("D").ToLowerInvariant();
		// GUIDs of record types to be owned by Event.
		readonly string ksguidObservation = RnResearchNbkTags.kguidRecObservation.ToString("D").ToLowerInvariant();
		readonly string ksguidConversation = RnResearchNbkTags.kguidRecConversation.ToString("D").ToLowerInvariant();
		readonly string ksguidInterview = RnResearchNbkTags.kguidRecInterview.ToString("D").ToLowerInvariant();
		readonly string ksguidPerformance = RnResearchNbkTags.kguidRecPerformance.ToString("D").ToLowerInvariant();
		// GUIDs of record types that remain owned by the list
		readonly string ksguidLiteratureSummary = RnResearchNbkTags.kguidRecLiteratureSummary.ToString("D").ToLowerInvariant();
		readonly string ksguidAnalysis = RnResearchNbkTags.kguidRecAnalysis.ToString("D").ToLowerInvariant();

#if __MonoCS__ // TODO-Linux: work around mono bug https://bugzilla.novell.com/show_bug.cgi?id=594877
		internal static XElement LocateObjsurElement(XElement e, string attributeTValue, string attributeGuidValue)
		{
			if (e.Name == "objsur" &&
				e.Attribute("t").Value == attributeTValue &&
				e.Attribute("guid").Value.ToLowerInvariant() == attributeGuidValue.ToLowerInvariant())
			{
				return e;
			}

			if (e.Descendants() != null)
			{
				foreach(var x in e.Descendants())
				{
					var ret = LocateObjsurElement(x, attributeTValue, attributeGuidValue);
					if (ret != null)
						return ret;
				}
			}
			return null;
		}
#endif

		#region IDataMigration Members

		/// <summary>
		/// Add a few items to the Notebook Record Type list, and rearrange the list a bit.  The original
		/// list should look something like:
		///		Conversation
		///		Interview
		///			Structured
		///			Unstructured
		///		Literature Summary
		///		Observation
		///			Performance
		///		Analysis
		///	After the migration, the list should look something like:
		///		Event
		///			Observation
		///			Conversation
		///			Interview
		///				Structured
		///				Unstructured
		///			Performance
		///		Literature Summary
		///		Analysis
		///		Methodology
		///		Weather
		/// </summary>
		/// <remarks>
		/// This implements FWR-643.  Note that users can edit this list, so any of the expected
		/// items could have been deleted or moved around in the list.  :-(
		/// </remarks>
		public void PerformMigration(IDomainObjectDTORepository repoDTO)
		{
			DataMigrationServices.CheckVersionNumber(repoDTO, 7000015);

			DomainObjectDTO dtoList = repoDTO.GetDTO(ksguidRecTypesList);
			XElement xeList = XElement.Parse(dtoList.Xml);
			XElement xeListPossibilities = xeList.XPathSelectElement("Possibilities");
			if (xeListPossibilities == null)
			{
				xeListPossibilities = new XElement("Possibilities");
				xeList.Add(xeListPossibilities);
			}
			// The user can edit the list, so these might possibly have been deleted (or moved).  :-(
			DomainObjectDTO dtoObservation = GetDTOIfItExists(repoDTO, ksguidObservation);
			DomainObjectDTO dtoConversation = GetDTOIfItExists(repoDTO, ksguidConversation);
			DomainObjectDTO dtoInterview = GetDTOIfItExists(repoDTO, ksguidInterview);
			DomainObjectDTO dtoPerformance = GetDTOIfItExists(repoDTO, ksguidPerformance);

			// Create the new Event, Methodology, and Weather record types.
			var nowStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

			StringBuilder sb = new StringBuilder();
			sb.AppendFormat("<rt class=\"CmPossibility\" guid=\"{0}\" ownerguid=\"{1}\">",
				ksguidEvent, ksguidRecTypesList);
			sb.Append("<Abbreviation><AUni ws=\"en\">Evnt</AUni></Abbreviation>");
			sb.Append("<Name><AUni ws=\"en\">Event</AUni></Name>");
			sb.AppendFormat("<DateCreated val=\"{0}\"/>", nowStr);
			sb.AppendFormat("<DateModified val=\"{0}\"/>", nowStr);
			sb.Append("</rt>");
			XElement xeEvent = XElement.Parse(sb.ToString());
			var dtoEvent = new DomainObjectDTO(ksguidEvent, "CmPossibility", xeEvent.ToString());
			repoDTO.Add(dtoEvent);
			xeListPossibilities.AddFirst(DataMigrationServices.CreateOwningObjSurElement(ksguidEvent));

			sb = new StringBuilder();
			sb.AppendFormat("<rt class=\"CmPossibility\" guid=\"{0}\" ownerguid=\"{1}\">",
				ksguidMethodology, ksguidRecTypesList);
			sb.Append("<Abbreviation><AUni ws=\"en\">Method</AUni></Abbreviation>");
			sb.Append("<Name><AUni ws=\"en\">Methodology</AUni></Name>");
			sb.AppendFormat("<DateCreated val=\"{0}\"/>", nowStr);
			sb.AppendFormat("<DateModified val=\"{0}\"/>", nowStr);
			sb.Append("</rt>");
			var dtoMethod = new DomainObjectDTO(ksguidMethodology, "CmPossibility", sb.ToString());
			repoDTO.Add(dtoMethod);
			xeListPossibilities.LastNode.AddAfterSelf(DataMigrationServices.CreateOwningObjSurElement(ksguidMethodology));

			sb = new StringBuilder();
			sb.AppendFormat("<rt class=\"CmPossibility\" guid=\"{0}\" ownerguid=\"{1}\">",
				ksguidWeather, ksguidRecTypesList);
			sb.Append("<Abbreviation><AUni ws=\"en\">Wthr</AUni></Abbreviation>");
			sb.Append("<Name><AUni ws=\"en\">Weather</AUni></Name>");
			sb.AppendFormat("<DateCreated val=\"{0}\"/>", nowStr);
			sb.AppendFormat("<DateModified val=\"{0}\"/>", nowStr);
			sb.Append("</rt>");
			var dtoWeather = new DomainObjectDTO(ksguidWeather, "CmPossibility", sb.ToString());
			repoDTO.Add(dtoWeather);
			xeListPossibilities.LastNode.AddAfterSelf(DataMigrationServices.CreateOwningObjSurElement(ksguidWeather));

			DataMigrationServices.UpdateDTO(repoDTO, dtoList, xeList.ToString());

			// Change the ownership links for the moved items.
			if (dtoObservation != null)
				ChangeOwner(repoDTO, dtoObservation, ksguidEvent, "SubPossibilities");
			if (dtoConversation != null)
				ChangeOwner(repoDTO, dtoConversation, ksguidEvent, "SubPossibilities");
			if (dtoInterview != null)
				ChangeOwner(repoDTO, dtoInterview, ksguidEvent, "SubPossibilities");
			if (dtoPerformance != null)
				ChangeOwner(repoDTO, dtoPerformance, ksguidEvent, "SubPossibilities");

			DataMigrationServices.IncrementVersionNumber(repoDTO);
		}

		private void ChangeOwner(IDomainObjectDTORepository repoDTO, DomainObjectDTO dto, string sGuidNew,
			string xpathNew)
		{
			XElement xe = XElement.Parse(dto.Xml);
			XAttribute xaOwner = xe.Attribute("ownerguid");
			string sGuidOld = null;
			if (xaOwner != null)
			{
				sGuidOld = xe.Attribute("ownerguid").Value;
				xe.Attribute("ownerguid").Value = sGuidNew;
			}
			else
			{
				xe.AddAnnotation(new XAttribute("ownerguid", sGuidNew));
			}
			DataMigrationServices.UpdateDTO(repoDTO, dto, xe.ToString());
			if (sGuidOld != null)
			{
				DomainObjectDTO dtoOldOwner = repoDTO.GetDTO(sGuidOld);
				XElement xeOldOwner = XElement.Parse(dtoOldOwner.Xml);
#if !__MonoCS__
				string xpathOld = String.Format(".//objsur[@t='o' and @guid='{0}']", dto.Guid);
				XElement xeOldRef = xeOldOwner.XPathSelectElement(xpathOld);
				if (xeOldRef == null)
				{
					xpathOld = String.Format(".//objsur[@t='o' and @guid='{0}']", dto.Guid.ToLowerInvariant());
					xeOldRef = xeOldOwner.XPathSelectElement(xpathOld);
				}
				if (xeOldRef == null)
				{
					xpathOld = String.Format(".//objsur[@t='o' and @guid='{0}']", dto.Guid.ToUpperInvariant());
					xeOldRef = xeOldOwner.XPathSelectElement(xpathOld);
				}
				if (xeOldRef == null)
				{
					foreach (XElement x in xeOldOwner.XPathSelectElements(".//objsur[@t='0']"))
					{
						var guidDst = x.Attribute("guid");
						if (guidDst != null && guidDst.Value.ToLowerInvariant() == dto.Guid.ToLowerInvariant())
						{
							xeOldRef = x;
							break;
						}
					}
				}
#else // TODO-Linux: work around mono bug https://bugzilla.novell.com/show_bug.cgi?id=594877
				XElement xeOldRef = null;
				xeOldRef = LocateObjsurElement(xeOldOwner, "o", dto.Guid);
				if (xeOldRef == null)
						xeOldRef = LocateObjsurElement(xeOldOwner, "0", dto.Guid);
#endif
				if (xeOldRef != null)
				{
					xeOldRef.Remove();
					DataMigrationServices.UpdateDTO(repoDTO, dtoOldOwner, xeOldOwner.ToString());
				}
			}
			DomainObjectDTO dtoNewOwner = repoDTO.GetDTO(sGuidNew);
			XElement xeNewOwner = XElement.Parse(dtoNewOwner.Xml);
			XElement xeNewField = xeNewOwner.XPathSelectElement(xpathNew);
			if (xeNewField == null)
				xeNewField = CreateXPathElementsAsNeeded(xpathNew, xeNewOwner);
			if (xeNewField == null)
				throw new ArgumentException("invalid XPath expression for storing owning objsur element");
			XElement xeObjSur = DataMigrationServices.CreateOwningObjSurElement(dto.Guid);
			if (xeNewField.HasElements)
				xeNewField.LastNode.AddAfterSelf(xeObjSur);
			else
				xeNewField.AddFirst(xeObjSur);
			DataMigrationServices.UpdateDTO(repoDTO, dtoNewOwner, xeNewOwner.ToString());
		}

		private static XElement CreateXPathElementsAsNeeded(string xpathNew, XElement xeNewOwner)
		{
			string xpath = xpathNew;
			List<string> rgsNames = new List<string>();
			XElement xePartial = null;
			int idx = xpath.LastIndexOf('/');
			if (idx == -1)
			{
				xePartial = xeNewOwner;
				rgsNames.Add(xpathNew);
			}
			while (idx > 0)
			{
				string sName = xpathNew.Substring(idx + 1);
				xpath = xpath.Substring(0, idx);
				if (sName.Length > 0)
				{
					rgsNames.Add(sName);
					if (xpath.Length > 0)
					{
						xePartial = xeNewOwner.XPathSelectElement(xpath);
						if (xePartial != null)
							break;
					}
					else
					{
						xePartial = xeNewOwner;
						break;
					}
				}
				idx = xpath.LastIndexOf('/');
			}
			if (xePartial != null && rgsNames.Count > 0)
			{
				for (int i = rgsNames.Count - 1; i >= 0; --i)
				{
					XElement xeSub = XElement.Parse(String.Format("<{0}></{0}>", rgsNames[i]));
					xePartial.Add(xeSub);
					xePartial = xeSub;
				}
				return xePartial;
			}
			return null;
		}

		private static DomainObjectDTO GetDTOIfItExists(IDomainObjectDTORepository dtoRepository, string sGuid)
		{
			try
			{
				return dtoRepository.GetDTO(sGuid);
			}
			catch (ArgumentException)
			{
				return null;
			}
		}

		#endregion
	}
}
