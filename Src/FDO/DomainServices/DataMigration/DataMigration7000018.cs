// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DataMigration7000018.cs
// Responsibility: mcconnel
//
// <remarks>
// This implements FWR-648, FWR-741, and part of FWR-1163.
// It also ensures that built-in styles for the LangProject.Styles list are all marked built-in.
// </remarks>

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;

using SIL.Utils;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Migrates from 7000017 to 7000018
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DataMigration7000018 : IDataMigration
	{
		IDomainObjectDTORepository m_repoDTO;
		/// <summary>list of objects affected by TE's stylesheet</summary>
		HashSet<DomainObjectDTO> m_scrDtos = new HashSet<DomainObjectDTO>();
		/// <summary>the LangProject object</summary>
		DomainObjectDTO m_dtoLangProj;
		/// <summary>XML string representation of the &lt;Styles&gt; field in the LangProject object</summary>
		string m_sLangProjStyles;
		/// <summary>the LangProject StStyle objects</summary>
		List<DomainObjectDTO> m_langProjStyles = new List<DomainObjectDTO>();
		/// <summary></summary>
		Dictionary<string, DomainObjectDTO> m_mapPropsToStyle = new Dictionary<string, DomainObjectDTO>();
		Dictionary<string, string> m_mapStyleNameToGuid = new Dictionary<string, string>();
		int m_cNewCharStyles = 0;
		int m_cNewParaStyles = 0;


		#region IDataMigration Members
		/// <summary>
		/// 1. Create the "Strong" style if it doesn't exist (FWR-741).
		/// 2. Convert direct formatting to styles (FWR-648).
		/// 3. Move the StStyle objects in LexDb.Styles to LangProject.Styles, deleting those
		///	   with the same name as one already in LangProject.Styles (FWR-1163).
		///	4. Delete the Styles field from LexDb (FWR-1163).
		///	5. Remove "External Link" and "Internal Link" styles - migrate use of these to
		///	   "Hyperlink" style (FWR-1163).
		///	6. Rename "Language Code" style to "Writing System Abbreviation" (FWR-1163).
		///	7. Ensure that built-in styles from LangProject.Styles are marked built-in.
		/// </summary>
		public void PerformMigration(IDomainObjectDTORepository repoDTO)
		{
			m_repoDTO = repoDTO;
			// Get the list of StStyle DTOs from the LexDb.Styles field, and delete the
			// LexDb.Styles field.
			string sClass = "LexDb";
			DomainObjectDTO dtoLexDb = GetFirstInstance(sClass);
			string sXmlLexDb = dtoLexDb.Xml;
			int idxStyles = sXmlLexDb.IndexOf("<Styles>");
			int idxStylesLim = sXmlLexDb.IndexOf("</Styles>") + 9;
			string sLexDbStyles = sXmlLexDb.Substring(idxStyles, idxStylesLim - idxStyles);
			List<DomainObjectDTO> stylesLexDb = new List<DomainObjectDTO>();
			foreach (string sGuid in GetGuidList(sLexDbStyles))
			{
				var dto = m_repoDTO.GetDTO(sGuid);
				stylesLexDb.Add(dto);
			}
			dtoLexDb.Xml = sXmlLexDb.Remove(idxStyles, idxStylesLim - idxStyles);
			m_repoDTO.Update(dtoLexDb);

			// Get the list of StStyle DTOs (and style names) from the LangProject.Styles field.
			m_dtoLangProj = GetFirstInstance("LangProject");
			string sXmlLangProj = m_dtoLangProj.Xml;
			idxStyles = sXmlLangProj.IndexOf("<Styles>");
			idxStylesLim = sXmlLangProj.IndexOf("</Styles>") + 9;
			m_sLangProjStyles = sXmlLangProj.Substring(idxStyles, idxStylesLim - idxStyles);
			string sLangProjStylesOrig = m_sLangProjStyles;
			int idxEnd = m_sLangProjStyles.Length - 9;
			List<string> styleNames = new List<string>();
			m_langProjStyles.Clear();
			m_mapStyleNameToGuid.Clear();
			DomainObjectDTO dtoHyperlink = null;
			DomainObjectDTO dtoExternalLink = null;
			DomainObjectDTO dtoInternalLink = null;
			DomainObjectDTO dtoLanguageCode = null;
			DomainObjectDTO dtoWrtSysAbbr = null;
			DomainObjectDTO dtoStrong = null;
			foreach (string sGuid in GetGuidList(m_sLangProjStyles))
			{
				var dto = m_repoDTO.GetDTO(sGuid);
				string sName = GetStyleName(dto.Xml);
				styleNames.Add(sName);
				m_langProjStyles.Add(dto);
				m_mapStyleNameToGuid.Add(sName, dto.Guid.ToLowerInvariant());
				switch (sName)
				{
					case "Hyperlink":
						dtoHyperlink = dto;
						break;
					case "External Link":
						dtoExternalLink = dto;
						break;
					case "Internal Link":
						dtoInternalLink = dto;
						break;
					case "Language Code":
						dtoLanguageCode = dto;
						break;
					case "Writing System Abbreviation":
						dtoWrtSysAbbr = dto;
						break;
					case "Strong":
						dtoStrong = dto;
						break;
				}
			}
			var mapLexDbStyleGuidName = new Dictionary<string, string>();
			// For each style in the ones we might delete we need to know the name of the style it is based on and its next style
			foreach (var dto in stylesLexDb)
			{
				var elt = XElement.Parse(dto.Xml);
				var name = elt.Element("Name").Element("Uni").Value;
				mapLexDbStyleGuidName[dto.Guid] = name;
			}
			Dictionary<string, string> mapStyleGuids = new Dictionary<string, string>();
			foreach (var dto in stylesLexDb)
			{
				string sXml = dto.Xml;
				string sName = GetStyleName(sXml);
				if (styleNames.Contains(sName))
				{
					var keeperGuid = m_mapStyleNameToGuid[sName];
					mapStyleGuids.Add(dto.Guid.ToLowerInvariant(), keeperGuid);
					// Duplicate between Notebook (LangProj) styles already in the dictionary,
					// and Lexicon (LexDb) ones being added. Discard the Lexicon style OBJECT, but keep its rules.
					var dtoKeeper = repoDTO.GetDTO(keeperGuid);
					var keeperElement = XElement.Parse(dtoKeeper.Xml);
					var dropElement = XElement.Parse(dto.Xml);
					var rules = dropElement.Element("Rules");
					if (rules != null)
					{
						var oldRules = keeperElement.Element("Rules");
						oldRules.ReplaceWith(rules);
					}
					UpdateStyleCrossReference(mapLexDbStyleGuidName, dropElement, keeperElement, "BasedOn");
					UpdateStyleCrossReference(mapLexDbStyleGuidName, dropElement, keeperElement, "Next");
					dtoKeeper.Xml = keeperElement.ToString();
					repoDTO.Update(dtoKeeper);
					m_repoDTO.Remove(dto);
				}
				else
				{
					// change the ownership links
					m_sLangProjStyles = m_sLangProjStyles.Insert(idxEnd, string.Format("<objsur guid=\"{0}\" t=\"o\"/>{1}", dto.Guid, Environment.NewLine));
					idxEnd = m_sLangProjStyles.Length - 9;
					SetOwnerGuid(dto, m_dtoLangProj.Guid);
					switch (sName)
					{
						case "Hyperlink":					dtoHyperlink = dto;		break;
						case "External Link":				dtoExternalLink = dto;	break;
						case "Internal Link":				dtoInternalLink = dto;	break;
						case "Language Code":				dtoLanguageCode = dto;	break;
						case "Writing System Abbreviation":	dtoWrtSysAbbr = dto;	break;
						case "Strong":						dtoStrong = dto;		break;
					}
					styleNames.Add(sName);
					m_langProjStyles.Add(dto);
					m_mapStyleNameToGuid.Add(sName, dto.Guid.ToLowerInvariant());
				}
			}
			// if "Hyperlink" does not exist, create it.
			if (dtoHyperlink == null)
			{
				dtoHyperlink = CreateCharStyle("Hyperlink",
					"<Prop forecolor=\"blue\" undercolor=\"blue\" underline=\"single\" />",
					ContextValues.Internal);
				m_langProjStyles.Add(dtoHyperlink);
				m_mapStyleNameToGuid.Add("Hyperlink", dtoHyperlink.Guid.ToLowerInvariant());
			}
			else
			{
				// ensure that the Hyperlink style has an "internal" context.
				string sXml = dtoHyperlink.Xml;
				int idx = sXml.IndexOf("<Context");
				if (idx > 0)
				{
					XElement xeHyper = XElement.Parse(sXml);
					foreach (XElement xe in xeHyper.Descendants("Context"))
					{
						int nVal;
						XAttribute xa = xe.Attribute("val");
						if (xa != null && Int32.TryParse(xa.Value, out nVal) && nVal != (int)ContextValues.Internal)
						{
							nVal = (int)ContextValues.Internal;
							xa.Value = nVal.ToString();
							dtoHyperlink.Xml = xeHyper.ToString();
							m_repoDTO.Update(dtoHyperlink);
							break;
						}
					}
				}
			}
			// delete "External Link" and "Internal Link", and prepare to replace links to
			// them with a link to "Hyperlink".
			if (dtoExternalLink != null)
			{
				mapStyleGuids.Add(dtoExternalLink.Guid.ToLowerInvariant(), dtoHyperlink.Guid.ToLowerInvariant());
				m_sLangProjStyles = DeleteStyle(m_sLangProjStyles, dtoExternalLink);
				m_mapStyleNameToGuid["External Link"] = dtoHyperlink.Guid.ToLowerInvariant();
			}
			if (dtoInternalLink != null)
			{
				mapStyleGuids.Add(dtoInternalLink.Guid.ToLowerInvariant(), dtoHyperlink.Guid.ToLowerInvariant());
				m_sLangProjStyles = DeleteStyle(m_sLangProjStyles, dtoInternalLink);
				m_mapStyleNameToGuid["Internal Link"] = dtoHyperlink.Guid.ToLowerInvariant();
			}
			if (dtoLanguageCode == null)
			{
				// If neither "Language Code" nor "Writing System Abbreviation" exist, create the
				// "Writing System Abbreviation" style.
				if (dtoWrtSysAbbr == null)
				{
					dtoWrtSysAbbr = CreateCharStyle("Writing System Abbreviation",
						"<Prop fontsize=\"8000\" fontsizeUnit=\"mpt\" forecolor=\"2f60ff\" />",
						ContextValues.General);
					m_langProjStyles.Add(dtoWrtSysAbbr);
					m_mapStyleNameToGuid.Add("Writing System Abbreviation", dtoWrtSysAbbr.Guid.ToLowerInvariant());
				}
				else
				{
					// We don't need to do anything.
				}
			}
			else
			{
				if (dtoWrtSysAbbr == null)
				{
					// Rename "Language Code" to "Writing System Abbreviation".
					string sXml = dtoLanguageCode.Xml;
					dtoLanguageCode.Xml = sXml.Replace("<Uni>Language Code</Uni>",
						"<Uni>Writing System Abbreviation</Uni>");
					m_repoDTO.Update(dtoLanguageCode);
					m_mapStyleNameToGuid.Add("Writing System Abbreviation", dtoLanguageCode.Guid.ToLowerInvariant());
				}
				else
				{
					// delete "Language Code", and prepare to replace links to it with a link to
					// "Writing System Abbreviation".
					mapStyleGuids.Add(dtoLanguageCode.Guid.ToLowerInvariant(), dtoWrtSysAbbr.Guid.ToLowerInvariant());
					m_sLangProjStyles = DeleteStyle(m_sLangProjStyles, dtoLanguageCode);
					m_mapStyleNameToGuid["Language Code"] = dtoWrtSysAbbr.Guid.ToLowerInvariant();
				}
			}
			// if "Strong" does not exist, create it.
			if (dtoStrong == null)
			{
				dtoStrong = CreateCharStyle("Strong", "<Prop bold=\"invert\" />", ContextValues.General);
				m_langProjStyles.Add(dtoStrong);
				m_mapStyleNameToGuid.Add("Strong", dtoStrong.Guid.ToLowerInvariant());
			}
			ChangeStyleReferences();
			UpdateStyleLinks(mapStyleGuids);
			ReplaceDirectFormattingWithStyles();
			EnsureBuiltinStylesAreMarkedBuiltin();

			if (m_sLangProjStyles != sLangProjStylesOrig)
			{
				sXmlLangProj = sXmlLangProj.Remove(idxStyles, idxStylesLim - idxStyles);
				m_dtoLangProj.Xml = sXmlLangProj.Insert(idxStyles, m_sLangProjStyles);
				m_repoDTO.Update(m_dtoLangProj);
			}

			DataMigrationServices.IncrementVersionNumber(m_repoDTO);
		}

		private void UpdateStyleCrossReference(Dictionary<string, string> mapLexDbStyleGuidName, XElement dropElement, XElement keeperElement, string elementName)
		{
			var keepBasedOn = dropElement.Element(elementName);
			if (keepBasedOn != null)
			{
				var basedOnGuid = keepBasedOn.Element("objsur").Attribute("guid").Value.ToLowerInvariant();
				string basedOnName;
				if (mapLexDbStyleGuidName.TryGetValue(basedOnGuid, out basedOnName))
				{
					string newBasedOnGuid;
					if (m_mapStyleNameToGuid.TryGetValue(basedOnName, out newBasedOnGuid))
					{
						// The corresponding style will be this one. We want the keeper style to be based on it.
						var keeperBasedOn = keeperElement.Element(elementName);
						var newBasedOn = new XElement(elementName,
							new XElement("objsur", new XAttribute("t", "r"), new XAttribute("guid", newBasedOnGuid)));
						if (keeperBasedOn != null)
							keeperBasedOn.ReplaceWith(newBasedOn);
						else
							keeperElement.Add(newBasedOn);
					}
				}
			}
		}

		private DomainObjectDTO CreateCharStyle(string sName, string sProp, ContextValues context)
		{
			DomainObjectDTO dtoStyle;
			Guid guid = Guid.NewGuid();
			string sGuid = guid.ToString().ToLowerInvariant();
			StringBuilder sb = new StringBuilder();
			sb.AppendLine(String.Format("<rt class=\"StStyle\" guid=\"{0}\" ownerguid=\"{1}\">",
				sGuid, m_dtoLangProj.Guid));
			sb.AppendLine("<IsBuiltIn val=\"true\" />");
			sb.AppendLine("<Name>");
			sb.AppendLine(String.Format("<Uni>{0}</Uni>", sName));
			sb.AppendLine("</Name>");
			sb.AppendLine("<Rules>");
			sb.AppendLine(sProp);
			sb.AppendLine("</Rules>");
			sb.AppendLine("<Type val=\"1\" />");
			if (context != ContextValues.General)
				sb.AppendLine(String.Format("<Context val=\"{0}\" />", (int)context));
			sb.AppendLine("</rt>");
			dtoStyle = new DomainObjectDTO(sGuid, "StStyle", sb.ToString());
			m_repoDTO.Add(dtoStyle);
			int idxEnd = m_sLangProjStyles.IndexOf("</Styles>");
			m_sLangProjStyles = m_sLangProjStyles.Insert(idxEnd, String.Format("<objsur guid=\"{0}\" t=\"o\"/>{1}", sGuid, Environment.NewLine));
			return dtoStyle;
		}

		private void UpdateStyleLinks(Dictionary<string, string> mapStyleGuids)
		{
			foreach (DomainObjectDTO dto in m_repoDTO.AllInstancesSansSubclasses("StStyle"))
			{
				string sXml = dto.Xml;
				if (sXml.Contains("<BasedOn>") || sXml.Contains("<Next>"))
				{
					XElement xeStyle = XElement.Parse(sXml);
					bool fChanged = UpdateStyleLink(mapStyleGuids, xeStyle, "BasedOn");
					fChanged |= UpdateStyleLink(mapStyleGuids, xeStyle, "Next");
					if (fChanged)
					{
						dto.Xml = xeStyle.ToString();
						m_repoDTO.Update(dto);	//  probably redundant, but shouldn't hurt
					}
				}
			}
			// Also change any references in Data Notebook UserViewField objects, even though
			// these probably don't matter.
			foreach (DomainObjectDTO dto in m_repoDTO.AllInstancesSansSubclasses("UserViewField"))
			{
				string sXml = dto.Xml;
				if (!sXml.Contains("<Flid val=\"40"))
					continue;
				if (sXml.Contains("<Uni>External Link</Uni>"))
				{
					dto.Xml = sXml.Replace("<Uni>External Link</Uni>", "<Uni>Hyperlink</Uni>");
					m_repoDTO.Update(dto);
				}
				else if (sXml.Contains("<Uni>Internal Link</Uni>"))
				{
					dto.Xml = sXml.Replace("<Uni>Internal Link</Uni>", "<Uni>Hyperlink</Uni>");
					m_repoDTO.Update(dto);
				}
				else if (sXml.Contains("<Uni>Language Code</Uni>"))
				{
					dto.Xml = sXml.Replace("<Uni>Language Code</Uni>", "<Uni>Writing System Abbreviation</Uni>");
					m_repoDTO.Update(dto);
				}
			}
		}

		/// <summary>
		/// If the link in a BasedOn or Next field points to a style that has been superseded,
		/// then change it.  Return true if a change is made.
		/// </summary>
		private bool UpdateStyleLink(Dictionary<string, string> mapStyleGuids,
			XElement xeStyle, string sLinkName)
		{
			foreach (XElement xeLink in xeStyle.Descendants(sLinkName))
			{
				foreach (XElement xeObjsur in xeLink.Descendants("objsur"))
				{
					XAttribute xa = xeObjsur.Attribute("guid");
					string sGuidOld = xa.Value.ToLowerInvariant();
					string sGuidNew;
					if (mapStyleGuids.TryGetValue(sGuidOld, out sGuidNew))
					{
						xa.Value = sGuidNew;
						return true;
					}
					else
					{
						return false;
					}
				}
			}
			return false;
		}

		private string DeleteStyle(string sLangProjStyles, DomainObjectDTO dto)
		{
			string sGuid = dto.Guid.ToLowerInvariant();
			m_repoDTO.Remove(dto);
			XElement xe = XElement.Parse(sLangProjStyles);
			var objsur = (from os in xe.Descendants("objsur")
						  where os.Attribute("guid").Value.ToLowerInvariant() == sGuid && os.Attribute("t").Value == "o"
						  select os).FirstOrDefault(); // Ought not be null, but play it safe.
			if (objsur != null)
			{
				objsur.Remove();
				sLangProjStyles = xe.ToString();
			}
			return sLangProjStyles;
		}

		private void SetOwnerGuid(DomainObjectDTO dto, string sGuidNewOwner)
		{
			string sXml = dto.Xml;
			int idx = sXml.IndexOf(" ownerguid=") + 11;
			char chQuote = sXml[idx];
			++idx;
			int idxLim = sXml.IndexOf(chQuote, idx);
			sXml = sXml.Remove(idx, idxLim - idx);
			dto.Xml = sXml.Insert(idx, sGuidNewOwner);
			m_repoDTO.Update(dto);
		}

		private string GetStyleName(string sXmlStyle)
		{
			int idxName = sXmlStyle.IndexOf("<Name>");
			int idxNameLim = sXmlStyle.IndexOf("</Name>") + 7;
			string sXmlName = sXmlStyle.Substring(idxName, idxNameLim - idxName);
			int idx = sXmlName.IndexOf("<Uni>") + 5;
			int idxLim = sXmlName.IndexOf("</Uni>");
			return sXmlName.Substring(idx, idxLim - idx);
		}

		private DomainObjectDTO GetFirstInstance(string sClass)
		{
			foreach (var dto in m_repoDTO.AllInstancesSansSubclasses(sClass))
			{
				return dto;
			}
			return null;
		}

		private List<string> GetGuidList(string sLexDbStyles)
		{
			List<string> styleGuids = new List<string>();
			int idx = sLexDbStyles.IndexOf(" guid=");
			while (idx > 0)
			{
				idx += 6;
				char chQuote = sLexDbStyles[idx];
				Debug.Assert(chQuote == '"' || chQuote == '\'');
				++idx;
				int idxLim = sLexDbStyles.IndexOf(chQuote, idx);
				string sGuid = sLexDbStyles.Substring(idx, idxLim - idx);
				styleGuids.Add(sGuid);
				idx = sLexDbStyles.IndexOf(" guid=", idxLim);
			}
			return styleGuids;
		}

		private void ChangeStyleReferences()
		{
			// First, get the list of objects that should not be changed.
			EnsureScrObjListFull();
			byte[] rgbRun = Encoding.UTF8.GetBytes("<Run ");
			byte[] rgbExternalLink1 = Encoding.UTF8.GetBytes(" namedStyle=\"External Link\"");
			byte[] rgbInternalLink1 = Encoding.UTF8.GetBytes(" namedStyle=\"Internal Link\"");
			byte[] rgbLanguageCode1 = Encoding.UTF8.GetBytes(" namedStyle=\"Language Code\"");
			byte[] rgbExternalLink2 = Encoding.UTF8.GetBytes(" namedStyle='External Link'");
			byte[] rgbInternalLink2 = Encoding.UTF8.GetBytes(" namedStyle='Internal Link'");
			byte[] rgbLanguageCode2 = Encoding.UTF8.GetBytes(" namedStyle='Language Code'");
			foreach (DomainObjectDTO dto in m_repoDTO.AllInstancesWithValidClasses())
			{
				if (m_scrDtos.Contains(dto))
					continue;
				byte[] rgbXml = dto.XmlBytes;
				if (BytesContain(rgbXml, rgbRun) &&
					(BytesContain(rgbXml, rgbExternalLink1) || BytesContain(rgbXml, rgbInternalLink1) ||
					 BytesContain(rgbXml, rgbLanguageCode1) || BytesContain(rgbXml, rgbExternalLink2) ||
					 BytesContain(rgbXml, rgbInternalLink2) || BytesContain(rgbXml, rgbLanguageCode2)))
				{
					string sXml = dto.Xml;
					bool fChanged = false;
					XElement xeObj = XElement.Parse(sXml);
					foreach (XElement xe in xeObj.Descendants("Run"))
					{
						XAttribute xaStyle = xe.Attribute("namedStyle");
						if (xaStyle != null)
						{
							if (xaStyle.Value == "External Link" || xaStyle.Value == "Internal Link")
							{
								xaStyle.Value = "Hyperlink";
								fChanged = true;
							}
							else if (xaStyle.Value == "Language Code")
							{
								xaStyle.Value = "Writing System Abbreviation";
								fChanged = true;
							}
						}
					}
					if (fChanged)
					{
						dto.Xml = xeObj.ToString();
						m_repoDTO.Update(dto);
					}
				}
			}
		}

		private void EnsureScrObjListFull()
		{
			if (m_scrDtos.Count == 0)
			{
				foreach (DomainObjectDTO dto in m_repoDTO.AllInstancesSansSubclasses("Scripture"))
				{
					m_scrDtos.Add(dto);
					AddOwnedObjects(dto, m_scrDtos);
				}
			}
		}

		private void ReplaceDirectFormattingWithStyles()
		{
			EnsureScrObjListFull();
			byte[] rgbRun = Encoding.UTF8.GetBytes("<Run ");
			foreach (DomainObjectDTO dto in m_repoDTO.AllInstancesWithValidClasses())
			{
				if (m_scrDtos.Contains(dto))
					continue;
				if (dto.XmlBytes.IndexOfSubArray(rgbRun) < 0)
					continue;
				bool fChanged = false;
				XElement xeObj = XElement.Parse(dto.Xml);
				bool fUpdate = false;
				if (dto.Classname == "PhEnvironment")
					fUpdate = FixUnderlines(xeObj);
				foreach (XElement xe in xeObj.Descendants("Run"))
				{
					string sNormDirectFmt = GetDirectStringFormats(xe);
					if (String.IsNullOrEmpty(sNormDirectFmt))
						continue;
					DomainObjectDTO dtoStyle = GetMatchingStyle(sNormDirectFmt, "<Type val=\"1\"/>");
					ReplaceRunDirectFmts(xe, GetStyleName(dtoStyle.Xml));
					fUpdate = true;
				}
				if (fUpdate)
				{
					dto.Xml = xeObj.ToString();
					m_repoDTO.Update(dto);
				}
			}
			byte[] rgbProp = Encoding.UTF8.GetBytes("<Prop ");
			foreach (DomainObjectDTO dtoPara in m_repoDTO.AllInstancesWithSubclasses("StTxtPara"))
			{
				if (m_scrDtos.Contains(dtoPara))
					continue;
				if (dtoPara.XmlBytes.IndexOfSubArray(rgbProp) < 0)
					continue;
				XElement xePara = XElement.Parse(dtoPara.Xml);
				bool fUpdate = false;
				foreach (XElement xe in xePara.Descendants("Prop"))
				{
					string sNormDirectFmt = GetDirectParaFormats(xe, true);
					if (String.IsNullOrEmpty(sNormDirectFmt))
						continue;
					DomainObjectDTO dtoStyle = GetMatchingStyle(sNormDirectFmt, null);
					ReplacePropDirectFmts(xe, GetStyleName(dtoStyle.Xml));
					fUpdate = true;
				}
				if (fUpdate)
				{
					dtoPara.Xml = xePara.ToString();
					m_repoDTO.Update(dtoPara);
				}
			}
		}

		private bool FixUnderlines(XElement xeObj)
		{
			bool fModified = false;
			foreach (XElement xeRun in xeObj.Descendants("Run"))
			{
				XAttribute xaUnderline = xeRun.Attribute("underline");
				XAttribute xaUndercolor = xeRun.Attribute("undercolor");
				if (xaUnderline != null && xaUnderline.Value.ToLowerInvariant() == "none")
				{
					xaUnderline.Remove();
					if (xaUndercolor != null)
						xaUndercolor.Remove();
					fModified = true;
				}
				else if (xaUnderline == null && xaUndercolor != null)
				{
					xaUndercolor.Remove();
					fModified = true;
				}
			}
			return fModified;
		}

		private DomainObjectDTO GetMatchingStyle(string sNormProps, string sTypeVal)
		{
			if (m_mapPropsToStyle.Count == 0)
				LoadPropsToStyleMap();
			DomainObjectDTO dtoStyle;
			if (!m_mapPropsToStyle.TryGetValue(sNormProps, out dtoStyle))
			{
				StringBuilder sb = new StringBuilder();
				Guid guidNew = Guid.NewGuid();
				sb.AppendLine(String.Format("<rt class=\"StStyle\" guid=\"{0}\" ownerguid=\"{1}\">",
					guidNew, m_dtoLangProj.Guid));
				string sNewName = null;
				if (String.IsNullOrEmpty(sTypeVal))
					sNewName = String.Format("Paragraph Formatting {0}", ++m_cNewParaStyles);
				else
					sNewName = String.Format("Character Formatting {0}", ++m_cNewCharStyles);
				sb.AppendLine(String.Format("<Name><Uni>{0}</Uni></Name>", sNewName));
				sb.AppendLine(sNormProps);
				if (!String.IsNullOrEmpty(sTypeVal))
					sb.AppendLine(sTypeVal);
				sb.AppendLine("</rt>");
				dtoStyle = new DomainObjectDTO(guidNew.ToString().ToLowerInvariant(), "StStyle", sb.ToString());
				m_repoDTO.Add(dtoStyle);
				int idxEnd = m_sLangProjStyles.IndexOf("</Styles>");
				m_sLangProjStyles = m_sLangProjStyles.Insert(idxEnd, String.Format("<objsur guid=\"{0}\" t=\"o\"/>{1}",
					guidNew, Environment.NewLine));
				m_mapPropsToStyle.Add(sNormProps, dtoStyle);
			}
			return dtoStyle;
		}

		private void LoadPropsToStyleMap()
		{
			foreach (DomainObjectDTO dto in m_langProjStyles)
			{
				string sNormProps = GetNormalizedStyleProps(dto);
				if (String.IsNullOrEmpty(sNormProps))
					continue;	// empty definition is possible...
				// More than one style can have the same properties -- we'll use the first, or
				// the first without a <Context> element (ie, the general context).
				DomainObjectDTO dtoPrior;
				if (m_mapPropsToStyle.TryGetValue(sNormProps, out dtoPrior))
				{
					if (dtoPrior.Xml.Contains("<Context ") && !dto.Xml.Contains("<Context "))
						m_mapPropsToStyle[sNormProps] = dto;
				}
				else
				{
					m_mapPropsToStyle.Add(sNormProps, dto);
				}
				// Try for fuzzier matching -- Data Notebook's direct formatting left out some
				// information that the corresponding style supplied.  Also, some prebuilt lists
				// handled direct formatting of bold or italic differently than the program
				// did, either for styles or applying direct formatting.
				// See comments on FWR-648.
				string sNormProps2 = sNormProps;
				if (sNormProps2.Contains("=\"invert\""))
				{
					sNormProps2 = sNormProps2.Replace("bold=\"invert\"", "bold=\"on\"");
					sNormProps2 = sNormProps2.Replace("italic=\"invert\"", "italic=\"on\"");
				}
				if (sNormProps2.Contains("bulNumScheme=\"1"))
				{
					sNormProps2 = sNormProps2.Replace(" bulNumStartAt=\"1\"", "");
					sNormProps2 = sNormProps2.Replace(" spaceAfter=\"0\"", "");
					sNormProps2 = sNormProps2.Replace(" spaceBefore=\"0\"", "");
					sNormProps2 = sNormProps2.Replace(" trailingIndent=\"0\"", "");
				}
				DomainObjectDTO dtoPrior2;
				if (sNormProps2 != sNormProps)
				{
					if (m_mapPropsToStyle.TryGetValue(sNormProps2, out dtoPrior2))
					{
						if (dtoPrior2.Xml.Contains("<Context ") && !dto.Xml.Contains("<Context "))
							m_mapPropsToStyle[sNormProps2] = dto;
					}
					else
					{
						m_mapPropsToStyle.Add(sNormProps2, dto);
					}
				}
			}
		}

		private string GetNormalizedStyleProps(DomainObjectDTO dtoStyle)
		{
			XElement xeStyle = XElement.Parse(dtoStyle.Xml);
			string sType = null;
			foreach (XElement xeType in xeStyle.Descendants("Type"))
			{
				XAttribute xa = xeType.Attribute("val");
				sType = xa.Value;
			}
			string sBasedOnGuid = null;
			foreach (XElement xeBasedOn in xeStyle.Descendants("BasedOn"))
			{
				foreach (XElement xeObjsur in xeBasedOn.Descendants("objsur"))
				{
					XAttribute xaGuid = xeObjsur.Attribute("guid");
					sBasedOnGuid = xaGuid.Value.ToLowerInvariant();
				}
			}
			string sNormalizedProps = null;
			foreach (XElement xeProp in xeStyle.Descendants("Prop"))
			{
				if (sType == "1")
				{
					sNormalizedProps = GetDirectStringFormats(xeProp);
				}
				else
				{
					sNormalizedProps = GetDirectParaFormats(xeProp, false);
				}
				if (!String.IsNullOrEmpty(sNormalizedProps) &&
					!String.IsNullOrEmpty(sBasedOnGuid))
				{
					StringBuilder sb = new StringBuilder(sNormalizedProps);
					sb.AppendLine("<BasedOn>");
					sb.AppendLine(String.Format("<objsur t=\"r\" guid=\"{0}\" />", sBasedOnGuid));
					sb.AppendLine("</BasedOn>");
					return sb.ToString();
				}
			}
			return sNormalizedProps;
		}

		/// <summary>
		/// Convert the direct formatting attributes of a &lt;Run&gt; element into a
		/// normalized XML string suitable for specifying a style.
		/// </summary>
		private string GetDirectStringFormats(XElement xeRun)
		{
			// Normalize the order of any direct formatting attributes.
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("<Rules>");
			sb.Append("<Prop");
			bool fPropClosed = false;
			int cchEmpty = sb.Length;
			AddPropToBuilderIfItExists(xeRun, sb, "backcolor");
			AddPropToBuilderIfItExists(xeRun, sb, "bold");
			AddPropToBuilderIfItExists(xeRun, sb, "fontFamily");
			AddPropToBuilderIfItExists(xeRun, sb, "fontVariations");
			AddPropToBuilderIfItExists(xeRun, sb, "fontsize");
			AddPropToBuilderIfItExists(xeRun, sb, "fontsizeUnit");
			AddPropToBuilderIfItExists(xeRun, sb, "forecolor");
			AddPropToBuilderIfItExists(xeRun, sb, "italic");
			AddPropToBuilderIfItExists(xeRun, sb, "offset");
			AddPropToBuilderIfItExists(xeRun, sb, "offsetUnit");
			AddPropToBuilderIfItExists(xeRun, sb, "superscript");
			XAttribute xaUnderline = xeRun.Attribute("underline");
			// If underline="squiggle", then ignore both underline and undercolor.
			// This is used in PhEnvironment.StringRepresentation to mark errors.
			if (xaUnderline != null)
			{
				if (xaUnderline.Value.ToLowerInvariant() != "squiggle")
				{
					sb.AppendFormat(" {0}=\"{1}\"", "underline", xaUnderline.Value);
					AddPropToBuilderIfItExists(xeRun, sb, "undercolor");
				}
			}
			else
			{
				AddPropToBuilderIfItExists(xeRun, sb, "undercolor");
			}
			foreach (XElement xeWsStyles in xeRun.Descendants("WsStyles9999"))
			{
				sb.AppendLine(">");
				fPropClosed = true;
				// Should be only one <WsStyles9999> element at most!
				sb.AppendLine("<WsStyles9999>");
				foreach (XElement xeWsProp in xeWsStyles.Descendants("WsProp"))
				{
					AddPropToBuilderIfItExists(xeWsProp, sb, "ws");
					AddPropToBuilderIfItExists(xeWsProp, sb, "backcolor");
					AddPropToBuilderIfItExists(xeWsProp, sb, "bold");
					AddPropToBuilderIfItExists(xeWsProp, sb, "fontFamily");
					AddPropToBuilderIfItExists(xeWsProp, sb, "fontVariations");
					AddPropToBuilderIfItExists(xeWsProp, sb, "fontsize");
					AddPropToBuilderIfItExists(xeWsProp, sb, "fontsizeUnit");
					AddPropToBuilderIfItExists(xeWsProp, sb, "forecolor");
					AddPropToBuilderIfItExists(xeWsProp, sb, "italic");
					AddPropToBuilderIfItExists(xeWsProp, sb, "offset");
					AddPropToBuilderIfItExists(xeWsProp, sb, "offsetUnit");
					AddPropToBuilderIfItExists(xeWsProp, sb, "superscript");
					AddPropToBuilderIfItExists(xeWsProp, sb, "undercolor");
					AddPropToBuilderIfItExists(xeWsProp, sb, "underline");
				}
				sb.AppendLine("</WsStyles9999>");
			}
			if (sb.Length > cchEmpty)
			{
				if (fPropClosed)
					sb.AppendLine("</Prop>");
				else
					sb.AppendLine(" />");
				sb.AppendLine("</Rules>");
				XAttribute xa = xeRun.Attribute("namedStyle");
				if (xa != null)
				{
					sb.AppendLine("<BasedOn>");
					sb.AppendLine(String.Format("<objsur t=\"r\" guid=\"{0}\" />", m_mapStyleNameToGuid[xa.Value]));
					sb.AppendLine("</BasedOn>");
				}
				return sb.ToString();
			}
			else
			{
				return null;
			}
		}

		private string GetDirectParaFormats(XElement xeProp, bool fCheckNamedStyle)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("<Rules>");
			sb.Append("<Prop");
			bool fPropClosed = false;
			int cchEmpty = sb.Length;
			AddPropToBuilderIfItExists(xeProp, sb, "align");
			AddPropToBuilderIfItExists(xeProp, sb, "backcolor");
			AddPropToBuilderIfItExists(xeProp, sb, "bulNumScheme");
			AddPropToBuilderIfItExists(xeProp, sb, "bulNumStartAt");
			AddPropToBuilderIfItExists(xeProp, sb, "bulNumTxtAft");
			AddPropToBuilderIfItExists(xeProp, sb, "bulNumTxtBef");
			AddPropToBuilderIfItExists(xeProp, sb, "firstIndent");
			AddPropToBuilderIfItExists(xeProp, sb, "leadingIndent");
			AddPropToBuilderIfItExists(xeProp, sb, "lineHeight");
			AddPropToBuilderIfItExists(xeProp, sb, "lineHeightUnit");
			AddPropToBuilderIfItExists(xeProp, sb, "rightToLeft");
			AddPropToBuilderIfItExists(xeProp, sb, "spaceAfter");
			AddPropToBuilderIfItExists(xeProp, sb, "spaceBefore");
			AddPropToBuilderIfItExists(xeProp, sb, "trailingIndent");
			AddPropToBuilderIfItExists(xeProp, sb, "padLeading");
			AddPropToBuilderIfItExists(xeProp, sb, "padTrailing");
			AddPropToBuilderIfItExists(xeProp, sb, "padTop");
			AddPropToBuilderIfItExists(xeProp, sb, "padBottom");
			AddPropToBuilderIfItExists(xeProp, sb, "borderTop");
			AddPropToBuilderIfItExists(xeProp, sb, "borderBottom");
			AddPropToBuilderIfItExists(xeProp, sb, "borderLeading");
			AddPropToBuilderIfItExists(xeProp, sb, "borderTrailing");
			if (sb.Length > cchEmpty)
			{
				sb.AppendLine(">");
				fPropClosed = true;
			}
			foreach (XElement xeBulNum in xeProp.Descendants("BulNumFontInfo"))
			{
				if (!fPropClosed)
				{
					sb.AppendLine(">");
					fPropClosed = true;
				}
				// Should be only one <BulNumFontInfo> element at most!
				sb.Append("<BulNumFontInfo");
				AddPropToBuilderIfItExists(xeBulNum, sb, "bold");
				AddPropToBuilderIfItExists(xeBulNum, sb, "italic");
				AddPropToBuilderIfItExists(xeBulNum, sb, "backcolor");
				AddPropToBuilderIfItExists(xeBulNum, sb, "forecolor");
				AddPropToBuilderIfItExists(xeBulNum, sb, "fontFamily");
				AddPropToBuilderIfItExists(xeBulNum, sb, "fontsize");
				AddPropToBuilderIfItExists(xeBulNum, sb, "offset");
				AddPropToBuilderIfItExists(xeBulNum, sb, "superscript");
				AddPropToBuilderIfItExists(xeBulNum, sb, "underline");
				AddPropToBuilderIfItExists(xeBulNum, sb, "undercolor");
				sb.AppendLine(" />");
			}
			foreach (XElement xeWsStyles in xeProp.Descendants("WsStyles9999"))
			{
				if (!fPropClosed)
				{
					sb.AppendLine(">");
					fPropClosed = true;
				}
				// Should be only one <WsStyles9999> element at most!
				sb.AppendLine("<WsStyles9999>");
				foreach (XElement xeWsProp in xeWsStyles.Descendants("WsProp"))
				{
					AddPropToBuilderIfItExists(xeWsProp, sb, "ws");
					AddPropToBuilderIfItExists(xeWsProp, sb, "backcolor");
					AddPropToBuilderIfItExists(xeWsProp, sb, "bold");
					AddPropToBuilderIfItExists(xeWsProp, sb, "fontFamily");
					AddPropToBuilderIfItExists(xeWsProp, sb, "fontVariations");
					AddPropToBuilderIfItExists(xeWsProp, sb, "fontsize");
					AddPropToBuilderIfItExists(xeWsProp, sb, "fontsizeUnit");
					AddPropToBuilderIfItExists(xeWsProp, sb, "forecolor");
					AddPropToBuilderIfItExists(xeWsProp, sb, "italic");
					AddPropToBuilderIfItExists(xeWsProp, sb, "offset");
					AddPropToBuilderIfItExists(xeWsProp, sb, "offsetUnit");
					AddPropToBuilderIfItExists(xeWsProp, sb, "superscript");
					AddPropToBuilderIfItExists(xeWsProp, sb, "undercolor");
					AddPropToBuilderIfItExists(xeWsProp, sb, "underline");
				}
				sb.AppendLine("</WsStyles9999>");
			}
			if (sb.Length > cchEmpty)
			{
				sb.AppendLine("</Prop>");
				sb.AppendLine("</Rules>");
				if (fCheckNamedStyle)
				{
					sb.AppendLine("<BasedOn>");
					XAttribute xa = xeProp.Attribute("namedStyle");
					if (xa != null)
						sb.AppendLine(String.Format("<objsur t=\"r\" guid=\"{0}\" />", m_mapStyleNameToGuid[xa.Value]));
					else
						sb.AppendLine(String.Format("<objsur t=\"r\" guid=\"{0}\" />", m_mapStyleNameToGuid["Normal"]));
					sb.AppendLine("</BasedOn>");
				}
				return sb.ToString();
			}
			else
			{
				return null;
			}
		}

		private static void AddPropToBuilderIfItExists(XElement xe, StringBuilder sb,
			string sName)
		{
			XAttribute xa = xe.Attribute(sName);
			if (xa != null)
				sb.AppendFormat(" {0}=\"{1}\"", sName, XmlUtils.MakeSafeXmlAttribute(xa.Value));
		}


		private bool BytesContain(byte[] rgbXml, byte[] rgbTarget)
		{
			if (rgbTarget.Length > rgbXml.Length)
				return false;
			else if (rgbXml.Length == 0 || rgbTarget.Length == 0)
				return true;
			else
				return rgbXml.IndexOfSubArray(rgbTarget) > 0;
		}

		private void AddOwnedObjects(DomainObjectDTO dto, HashSet<DomainObjectDTO> scrObjs)
		{
			foreach (DomainObjectDTO dtoOwned in m_repoDTO.GetDirectlyOwnedDTOs(dto.Guid))
			{
				scrObjs.Add(dtoOwned);
				AddOwnedObjects(dtoOwned, scrObjs);
			}
		}

		private void ReplaceRunDirectFmts(XElement xeRun, string sStyleName)
		{
			List<XAttribute> rgxaDel = new List<XAttribute>();
			XAttribute xaUnderline = xeRun.Attribute("underline");
			XAttribute xaUndercolor = xeRun.Attribute("undercolor");
			if (xaUnderline != null)
			{
				if (xaUnderline.Value.ToLowerInvariant() != "squiggle")
				{
					xaUnderline.Remove();
					if (xaUndercolor != null)
						xaUndercolor.Remove();
				}
			}
			else if (xaUndercolor != null)
			{
				xaUndercolor.Remove();
			}
			foreach (XAttribute xa in xeRun.Attributes())
			{
				switch (xa.Name.LocalName)
				{
					case "backcolor":
					case "bold":
					case "fontFamily":
					case "fontVariations":
					case "fontsize":
					case "fontsizeUnit":
					case "forecolor":
					case "italic":
					case "offset":
					case "offsetUnit":
					case "superscript":
					case "namedStyle":
						rgxaDel.Add(xa);
						break;
				}
			}
			foreach (XAttribute xa in rgxaDel)
				xa.Remove();
			xeRun.Add(new XAttribute("namedStyle", sStyleName));
		}

		private void ReplacePropDirectFmts(XElement xeProp, string sStyleName)
		{
			List<XAttribute> rgxaDel = new List<XAttribute>();
			foreach (XAttribute xa in xeProp.Attributes())
			{
				switch (xa.Name.LocalName)
				{
					case "align":
					case "backcolor":
					case "bulNumScheme":
					case "bulNumStartAt":
					case "bulNumTxtAft":
					case "bulNumTxtBef":
					case "firstIndent":
					case "leadingIndent":
					case "lineHeight":
					case "lineHeightUnit":
					case "rightToLeft":
					case "spaceAfter":
					case "spaceBefore":
					case "trailingIndent":
					case "namedStyle":
						rgxaDel.Add(xa);
						break;
				}
			}
			foreach (XAttribute xa in rgxaDel)
				xa.Remove();
			List<XElement> rgxeDel = new List<XElement>();
			foreach (XElement xe in xeProp.Descendants("BulNumFontInfo"))
				rgxeDel.Add(xe);
			foreach (XElement xe in xeProp.Descendants("WsStyles9999"))
				rgxeDel.Add(xe);
			foreach (XElement xe in rgxeDel)
				xe.Remove();
			xeProp.Add(new XAttribute("namedStyle", sStyleName));
		}

		private void EnsureBuiltinStylesAreMarkedBuiltin()
		{
			foreach (DomainObjectDTO dto in m_langProjStyles)
			{
				string sXml = dto.Xml;
				if (IsStyleBuiltIn(sXml))
					continue;
				string sName = GetStyleName(sXml);
				switch (sName)
				{
					case "Normal":
					case "Numbered List":
					case "Bulleted List":
					case "Heading 1":
					case "Heading 2":
					case "Heading 3":
					case "Block Quote":
					case "Title Text":
					case "Emphasized Text":
					case "Writing System Abbreviation":
					case "Added Text":
					case "Deleted Text":
					case "Hyperlink":
					case "Strong":
						int idxInsert = sXml.IndexOf("<Name>");
						sXml = sXml.Insert(idxInsert, "<IsBuiltIn val=\"true\" />" + Environment.NewLine);
						dto.Xml = sXml;
						m_repoDTO.Update(dto);
						break;
				}
			}
		}

		private bool IsStyleBuiltIn(string sXmlStyle)
		{
			// look for <IsBuiltIn val="true" />
			int idxIsBuiltIn = sXmlStyle.IndexOf("<IsBuiltIn");
			if (idxIsBuiltIn < 0)
				return false;
			int idxIsBuiltInLim = sXmlStyle.IndexOf(">", idxIsBuiltIn) + 1;
			string sIsBuiltIn = sXmlStyle.Substring(idxIsBuiltIn, idxIsBuiltInLim - idxIsBuiltIn);
			int idx = sIsBuiltIn.IndexOf("val=", 11) + 4;
			char chQuote = sIsBuiltIn[idx++];
			int idxLim = sIsBuiltIn.IndexOf(chQuote, idx);
			string sVal = sIsBuiltIn.Substring(idx, idxLim - idx);
			return sVal.ToLowerInvariant() == "true";
		}

		#endregion
	}
}
