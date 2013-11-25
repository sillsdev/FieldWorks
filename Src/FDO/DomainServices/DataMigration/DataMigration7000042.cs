// Copyright (c) 2011-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DataMigration7000042.cs
// Responsibility: mcconnel

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;
using SIL.FieldWorks.Common.FwUtils;
using System;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Fix the items in the Complex Form Types and Variant Types lists to have to proper guids
	/// and proper settings for the IsProtected flags.  Also add any missing items that should
	/// exist.  See LT-11340 for details.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DataMigration7000042 : IDataMigration
	{
		private readonly Dictionary<string, string> m_mapGuidName =
			new Dictionary<string, string>(new StringIgnoreCaseComparer());
		private readonly Dictionary<string, string> m_mapNameGuid =
			new Dictionary<string, string>(new StringIgnoreCaseComparer());
		private readonly Dictionary<string, string> m_mapGuidOwner =
			new Dictionary<string, string>(new StringIgnoreCaseComparer());
		private readonly Dictionary<string, List<NewDtoInfo>> m_mapGuidNewDtos =
			new Dictionary<string, List<NewDtoInfo>>(new StringIgnoreCaseComparer());
		private readonly Dictionary<string, string> m_mapBadGoodGuids =
			new Dictionary<string, string>(new StringIgnoreCaseComparer());
		#region IDataMigration Members

		public void PerformMigration(IDomainObjectDTORepository repoDto)
		{
			DataMigrationServices.CheckVersionNumber(repoDto, 7000041);

			var dtoLexDb = repoDto.AllInstancesSansSubclasses("LexDb").First();
			var xeLexDb = XElement.Parse(dtoLexDb.Xml);
			var guidComplexTypes = GetGuidValue(xeLexDb, "ComplexEntryTypes/objsur");
			var guidVariantTypes = GetGuidValue(xeLexDb, "VariantEntryTypes/objsur");
			BuildStandardMaps(guidComplexTypes, guidVariantTypes);
			var extraTypes = new List<LexTypeInfo>();
			var unprotectedTypes = new List<LexTypeInfo>();
			foreach (var dto in repoDto.AllInstancesSansSubclasses("LexEntryType"))
			{
				var xeType = XElement.Parse(dto.Xml);
				// ReSharper disable PossibleNullReferenceException
				var guid = xeType.Attribute("guid").Value;
				// ReSharper restore PossibleNullReferenceException
				string name;
				if (m_mapGuidName.TryGetValue(guid, out name))
				{
					m_mapGuidName.Remove(guid);
					m_mapNameGuid.Remove(name);
					var xeProt = xeType.XPathSelectElement("IsProtected");
					if (xeProt == null)
					{
						unprotectedTypes.Add(new LexTypeInfo(dto, xeType));
					}
					else
					{
						var xaVal = xeProt.Attribute("val");
						if (xaVal == null || xaVal.Value.ToLowerInvariant() != "true")
							unprotectedTypes.Add(new LexTypeInfo(dto, xeType));
					}
				}
				else
				{
					extraTypes.Add(new LexTypeInfo(dto, xeType));
				}
			}
			foreach (var info in unprotectedTypes)
			{
				var xeProt = info.XmlElement.XPathSelectElement("IsProtected");
				if (xeProt != null)
					xeProt.Remove();
				info.XmlElement.Add(new XElement("IsProtected", new XAttribute("val", "true")));
				info.DTO.Xml = info.XmlElement.ToString();
				repoDto.Update(info.DTO);
			}
			if (m_mapGuidName.Count > 0)
				FixOrAddMissingTypes(repoDto, extraTypes);
			FixOwnershipOfSubtypes(repoDto);
			DataMigrationServices.IncrementVersionNumber(repoDto);
		}

		private void FixOwnershipOfSubtypes(IDomainObjectDTORepository repoDto)
		{
			var types = repoDto.AllInstancesWithSubclasses("LexEntryType");
			var cFixedFirst = 0;
			foreach (var dto in types)
			{
				var xml = dto.Xml;
				var fFixed = false;
				foreach (var badGuid in m_mapBadGoodGuids.Keys)
				{
					if (xml.Contains(badGuid))
					{
						var bad = String.Format("guid=\"{0}\"", badGuid);
						var good = String.Format("guid=\"{0}\"", m_mapBadGoodGuids[badGuid]);
						xml = xml.Replace(bad, good);
						var bad2 = String.Format("guid='{0}'", badGuid);
						var good2 = String.Format("guid='{0}'", m_mapBadGoodGuids[badGuid]);
						xml = xml.Replace(bad2, good2);	// probably pure paranoia...
						fFixed = true;
					}
				}
				if (fFixed)
				{
					dto.Xml = xml;
					repoDto.Update(dto);
					++cFixedFirst;
				}
				var cFixed = 0;
				foreach (var dtoSub in repoDto.GetDirectlyOwnedDTOs(dto.Guid))
				{
					DomainObjectDTO dtoOwner;
					if (!repoDto.TryGetOwner(dtoSub.Guid, out dtoOwner) || dtoOwner != dto)
					{
						// we have a broken ownership link -- fix it!
						var xeSub = XElement.Parse(dtoSub.Xml);
						var xaOwner = xeSub.Attribute("ownerguid");
						if (xaOwner == null)
							xeSub.Add(new XAttribute("ownerguid", dto.Guid));
						else
							xaOwner.Value = dto.Guid;
						dtoSub.Xml = xeSub.ToString();
						repoDto.Update(dtoSub);
						++cFixed;
					}
				}
			}
		}

		#endregion

		private void FixOrAddMissingTypes(IDomainObjectDTORepository repoDto, IEnumerable<LexTypeInfo> extraTypes)
		{
			foreach (var info in extraTypes)
			{
				var fChanged = false;
				foreach (var xeAUni in info.XmlElement.XPathSelectElements("Name/AUni"))
				{
					var xaWs = xeAUni.Attribute("ws");
					if (xaWs == null || xaWs.Value.ToLowerInvariant() != "en")
						continue;
					var name = xeAUni.Value;
					string guidStd;
					if (!m_mapNameGuid.TryGetValue(name, out guidStd))
						continue;
					// We need to change the guid of this dto from 'guid to 'guidStd
					ChangeInvalidGuid(repoDto, info, name, guidStd);
				}
				var xeProt = info.XmlElement.XPathSelectElement("IsProtected");
				if (xeProt == null)
				{
					info.XmlElement.Add(new XElement("IsProtected", new XAttribute("val", "true")));
					fChanged = true;
				}
				else
				{
					var xaVal = xeProt.Attribute("val");
					if (xaVal == null)
					{
						xeProt.Add(new XAttribute("val", "true"));
						fChanged = true;
					}
					else if (xaVal.Value.ToLowerInvariant() != "true")
					{
						xaVal.SetValue("true");
						fChanged = true;
					}
				}
				if (fChanged)
				{
					info.DTO.Xml = info.XmlElement.ToString();
					repoDto.Update(info.DTO);
				}
			}

			if (m_mapNameGuid.Count > 0)
			{
				BuildNewTypeMaps();
				var newTypes = new HashSet<DomainObjectDTO>();
				foreach (var guid in m_mapGuidName.Keys)
				{
					// We need to create this LexEntryType!
					var rgNewDtos = m_mapGuidNewDtos[guid];
					foreach (var info in rgNewDtos)
					{
						var dto = new DomainObjectDTO(info.Guid, info.ClassName, info.Xml);
						repoDto.Add(dto);
						if (info.ClassName == "LexEntryType")
							newTypes.Add(dto);

					}
				}
				foreach (var dto in newTypes)
				{
					var dtoOwner = repoDto.GetOwningDTO(dto);
					var xeOwner = XElement.Parse(dtoOwner.Xml);
					XElement xePoss = null;
					if (dtoOwner.Classname == "CmPossibilityList")
					{
						xePoss = xeOwner.Element("Possibilities");
						if (xePoss == null)
						{
							xePoss = new XElement("Possibilities");
							xeOwner.Add(xePoss);
						}
					}
					else if (dtoOwner.Classname == "LexEntryType")
					{
						xePoss = xeOwner.Element("SubPossibilities");
						if (xePoss == null)
						{
							xePoss = new XElement("SubPossibilities");
							xeOwner.Add(xePoss);
						}
					}
					if (xePoss != null)
					{
						var fNeeded = true;
						foreach (var objsur in xePoss.Elements("objsur"))
						{
							var xaGuid = objsur.Attribute("guid");
							if (xaGuid == null)
								throw new Exception("missing guid in an objsur element");
							if (xaGuid.Value.Equals(dto.Guid, StringComparison.OrdinalIgnoreCase))
							{
								fNeeded = false;
								break;
							}
						}
						if (fNeeded)
						{
							xePoss.Add(DataMigrationServices.CreateOwningObjSurElement(dto.Guid));
							dtoOwner.Xml = xeOwner.ToString();
							repoDto.Update(dtoOwner);
						}
					}
				}
			}
		}

		private void ChangeInvalidGuid(IDomainObjectDTORepository repoDto, LexTypeInfo info,
			string name, string guidStd)
		{
			var xaGuid = info.XmlElement.Attribute("guid");
			if (xaGuid == null)
				throw new Exception("The object does not have a guid -- this is impossible!");
			xaGuid.SetValue(guidStd);
			var guidBad = info.DTO.Guid;
			if (!m_mapBadGoodGuids.ContainsKey(guidBad))
				m_mapBadGoodGuids.Add(guidBad, guidStd);
			var className = info.DTO.Classname;
			repoDto.Remove(info.DTO);
			info.DTO = new DomainObjectDTO(guidStd, className, info.XmlElement.ToString());
			repoDto.Add(info.DTO);
			// Fix the owning reference (but only if it's one of the two lists, because otherwise
			// it might be contained in a LexTypeInfo that hasn't yet been processed).
			var bad = String.Format("guid=\"{0}\"", guidBad);
			var good = String.Format("guid=\"{0}\"", guidStd);
			var bad2 = String.Format("guid='{0}'", guidBad);	// probably pure paranoia...
			var good2 = String.Format("guid='{0}'", guidStd);
			DomainObjectDTO dtoOwner;
			if (repoDto.TryGetOwner(info.DTO.Guid, out dtoOwner) && dtoOwner.Classname == "CmPossibilityList")
			{
				dtoOwner.Xml = dtoOwner.Xml.Replace(bad, good).Replace(bad2, good2);
				repoDto.Update(dtoOwner);
			}
			// Fix any references from LexEntryRef objects.
			foreach (var dtoRef in repoDto.AllInstancesWithSubclasses("LexEntryRef"))
			{
				var xml = dtoRef.Xml;
				if (xml.Contains(guidBad))
				{
					dtoRef.Xml = xml.Replace(bad, good).Replace(bad2, good2);
					repoDto.Update(dtoRef);
				}
			}
			m_mapNameGuid.Remove(name);
			m_mapGuidName.Remove(guidStd);
		}

		private void BuildNewTypeMaps()
		{
			m_mapGuidNewDtos.Add(LexEntryTypeTags.kguidLexTypCompound.ToString(), GetDtosForCompound());
			m_mapGuidNewDtos.Add(LexEntryTypeTags.kguidLexTypContraction.ToString(), GetDtosForContraction());
			m_mapGuidNewDtos.Add(LexEntryTypeTags.kguidLexTypDerivation.ToString(), GetDtosForDerivative());
			m_mapGuidNewDtos.Add(LexEntryTypeTags.kguidLexTypIdiom.ToString(), GetDtosForIdiom());
			m_mapGuidNewDtos.Add(LexEntryTypeTags.kguidLexTypPhrasalVerb.ToString(), GetDtosForPhrasalVerb());
			m_mapGuidNewDtos.Add(LexEntryTypeTags.kguidLexTypSaying.ToString(), GetDtosForSaying());
			m_mapGuidNewDtos.Add(LexEntryTypeTags.kguidLexTypDialectalVar.ToString(), GetDtosForDialectalVar());
			m_mapGuidNewDtos.Add(LexEntryTypeTags.kguidLexTypFreeVar.ToString(), GetDtosForFreeVar());
			m_mapGuidNewDtos.Add(LexEntryTypeTags.kguidLexTypIrregInflectionVar.ToString(), GetDtosForIrregInflVar());
			m_mapGuidNewDtos.Add(LexEntryTypeTags.kguidLexTypPluralVar.ToString(), GetDtosForPluralVar());
			m_mapGuidNewDtos.Add(LexEntryTypeTags.kguidLexTypPastVar.ToString(), GetDtosForPastVar());
			m_mapGuidNewDtos.Add(LexEntryTypeTags.kguidLexTypSpellingVar.ToString(), GetDtosForSpellingVariant());
		}

		private List<NewDtoInfo> GetDtosForCompound()
		{
			var rgDtoXmls = new List<NewDtoInfo>();
			var bldr = new StringBuilder();
			bldr.AppendFormat(
				"<rt class=\"LexEntryType\" guid=\"1f6ae209-141a-40db-983c-bee93af0ca3c\" ownerguid=\"{0}\">{1}",
				m_mapGuidOwner[LexEntryTypeTags.kguidLexTypCompound.ToString()], Environment.NewLine);
			bldr.AppendLine("<Abbreviation>");
			bldr.AppendLine("<AUni ws=\"en\">comp. of</AUni>");
			bldr.AppendLine("</Abbreviation>");
			bldr.AppendLine("<Description>");
			bldr.AppendLine("<AStr ws=\"en\">");
			bldr.AppendLine("<Run ws=\"en\">A stem that is made up of more than one root.</Run>");
			bldr.AppendLine("</AStr>");
			bldr.AppendLine("</Description>");
			bldr.AppendLine("<IsProtected val=\"true\" />");
			bldr.AppendLine("<Name>");
			bldr.AppendLine("<AUni ws=\"en\">Compound</AUni>");
			bldr.AppendLine("</Name>");
			bldr.AppendLine("<Discussion>");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b314f2f8-ea5e-11de-86b7-0013722f8dec\" />");
			bldr.AppendLine("</Discussion>");
			bldr.AppendLine("<ReverseAbbr>");
			bldr.AppendLine("<AUni ws=\"en\">comp.</AUni>");
			bldr.AppendLine("</ReverseAbbr>");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("1f6ae209-141a-40db-983c-bee93af0ca3c", "LexEntryType", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StText\" guid=\"b314f2f8-ea5e-11de-86b7-0013722f8dec\" ownerguid=\"1f6ae209-141a-40db-983c-bee93af0ca3c\">");
			bldr.AppendLine("<Paragraphs>");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b31e7c56-ea5e-11de-85d3-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b32a6804-ea5e-11de-840f-0013722f8dec\" />");
			bldr.AppendLine("</Paragraphs>");
			bldr.AppendLine("<DateModified val=\"2011-3-29 16:16:31.319\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b314f2f8-ea5e-11de-86b7-0013722f8dec", "StText", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b31e7c56-ea5e-11de-85d3-0013722f8dec\" ownerguid=\"b314f2f8-ea5e-11de-86b7-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\">Example (English)</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b31e7c56-ea5e-11de-85d3-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b32a6804-ea5e-11de-840f-0013722f8dec\" ownerguid=\"b314f2f8-ea5e-11de-86b7-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run namedStyle=\"Emphasized Text\" ws=\"en\">Blackboard</Run>");
			bldr.AppendLine("<Run ws=\"en\"> contains a stem that refers to \"a large, smooth, usually dark surface on which to write or draw with chalk\". However, the stem is made up of two roots, </Run>");
			bldr.AppendLine("<Run namedStyle=\"Emphasized Text\" ws=\"en\">black</Run>");
			bldr.AppendLine("<Run ws=\"en\"> and </Run>");
			bldr.AppendLine("<Run namedStyle=\"Emphasized Text\" ws=\"en\">board</Run>");
			bldr.AppendLine("<Run ws=\"en\">.</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b32a6804-ea5e-11de-840f-0013722f8dec", "StTxtPara", bldr.ToString()));

			return rgDtoXmls;
		}

		private List<NewDtoInfo> GetDtosForContraction()
		{
			var rgDtoXmls = new List<NewDtoInfo>();
			var bldr = new StringBuilder();
			bldr.AppendFormat(
				"<rt class=\"LexEntryType\" guid=\"73266a3a-48e8-4bd7-8c84-91c730340b7d\" ownerguid=\"{0}\">{1}",
				m_mapGuidOwner[LexEntryTypeTags.kguidLexTypContraction.ToString()], Environment.NewLine);
			bldr.AppendLine("<Abbreviation>");
			bldr.AppendLine("<AUni ws=\"en\">contr. of</AUni>");
			bldr.AppendLine("</Abbreviation>");
			bldr.AppendLine("<Description>");
			bldr.AppendLine("<AStr ws=\"en\">");
			bldr.AppendLine("<Run ws=\"en\">A combination of two lexemes that are phonologically reduced.</Run>");
			bldr.AppendLine("</AStr>");
			bldr.AppendLine("</Description>");
			bldr.AppendLine("<IsProtected val=\"true\" />");
			bldr.AppendLine("<Name>");
			bldr.AppendLine("<AUni ws=\"en\">Contraction</AUni>");
			bldr.AppendLine("</Name>");
			bldr.AppendLine("<Discussion>");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b33653bc-ea5e-11de-9802-0013722f8dec\" />");
			bldr.AppendLine("</Discussion>");
			bldr.AppendLine("<ReverseAbbr>");
			bldr.AppendLine("<AUni ws=\"en\">contr.</AUni>");
			bldr.AppendLine("</ReverseAbbr>");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("73266a3a-48e8-4bd7-8c84-91c730340b7d", "LexEntryType", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StText\" guid=\"b33653bc-ea5e-11de-9802-0013722f8dec\" ownerguid=\"73266a3a-48e8-4bd7-8c84-91c730340b7d\">");
			bldr.AppendLine("<Paragraphs>");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b33fdd1a-ea5e-11de-8c86-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b34bc8c8-ea5e-11de-8b9f-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b357b480-ea5e-11de-875d-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b3613dd4-ea5e-11de-879d-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b36d298c-ea5e-11de-9bb0-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b379153a-ea5e-11de-93cc-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b3829e98-ea5e-11de-807d-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b38e8a50-ea5e-11de-8870-0013722f8dec\" />");
			bldr.AppendLine("</Paragraphs>");
			bldr.AppendLine("<DateModified val=\"2011-3-29 16:16:31.320\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b33653bc-ea5e-11de-9802-0013722f8dec", "StText", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b33fdd1a-ea5e-11de-8c86-0013722f8dec\" ownerguid=\"b33653bc-ea5e-11de-9802-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\">A contraction is a combination of two lexemes, each of which maintains its own meaning.</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b33fdd1a-ea5e-11de-8c86-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b34bc8c8-ea5e-11de-8b9f-0013722f8dec\" ownerguid=\"b33653bc-ea5e-11de-9802-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\">Example: hasn’t ‘has not’</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b34bc8c8-ea5e-11de-8b9f-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b357b480-ea5e-11de-875d-0013722f8dec\" ownerguid=\"b33653bc-ea5e-11de-9802-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\">Contractions are different from compounds. A compound, such as ‘hasbeen’, is a combination of two lexemes with an unpredictable change in meaning. There is no such change in meaning in a contraction.</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b357b480-ea5e-11de-875d-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b3613dd4-ea5e-11de-879d-0013722f8dec\" ownerguid=\"b33653bc-ea5e-11de-9802-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\">Contractions are different from clitics. A clitic is a lexeme that is grammatically independent but attaches phonologically to any adjacent word. An example of a clitic is the English possessive –’s in the phrases ‘Elizabeth’s hat’, ‘the queen’s hat’, and ‘the queen of England’s hat’. The clitic –’s obligatorily attaches to whatever word precedes it. In contrast a contraction is a specific pair of words that regularly combines. The combination may be obligatory, as in the case of ‘let’s’, as in “Let’s go” (“Let us go” has a different meaning), or it may be optional, as in the case of ‘we’ve’, as in “We’ve been honored,” or “We have been honored.”</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b3613dd4-ea5e-11de-879d-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b36d298c-ea5e-11de-9bb0-0013722f8dec\" ownerguid=\"b33653bc-ea5e-11de-9802-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\">Contractions are different from portmanteau morphemes. A portmanteau morpheme is a single, indivisible morpheme that combines two meanings that are usually expressed by separate morphemes. An example of a portmanteau morpheme is the word ‘were’ which is a single morpheme expressing the meaning of the lexeme ‘be’ and the grammatical category ‘Past.tense’. ‘Were’ cannot be divided into two morphemes. (Note that ‘busted’ can be divided into bust-ed ‘bust-Past.tense’.) In contrast the contraction ‘we’re’ (we are) can be divided into ‘we-’re’.</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b36d298c-ea5e-11de-9bb0-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b379153a-ea5e-11de-93cc-0013722f8dec\" ownerguid=\"b33653bc-ea5e-11de-9802-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\">One or both members of a contraction can be shortened. Most English contractions only shorten the second member, as in ‘I’m’ (I am), ‘it’s’ (it is), ‘isn’t’ (is not). Others shorten both members, as in ‘won’t’ (will not), ‘shan’t’ (shall not), ‘ain’t’ (am not).</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b379153a-ea5e-11de-93cc-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b3829e98-ea5e-11de-807d-0013722f8dec\" ownerguid=\"b33653bc-ea5e-11de-9802-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\">A contraction can combine more than two members, as in ‘wouldn’t’ve’ (would not have).</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b3829e98-ea5e-11de-807d-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b38e8a50-ea5e-11de-8870-0013722f8dec\" ownerguid=\"b33653bc-ea5e-11de-9802-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\">English orthography uses the apostrophe to indicate the loss of a phoneme, as in ‘shouldn’t’ (should not). But when both words lose a phoneme, only one apostrophe is used, as in ‘shan’t’ (shall not). In writing sometimes a contraction is written out as two separate words, even when it would normally be shortened to the contracted form in speech. Other languages may or may not choose to follow these orthographic conventions.</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b38e8a50-ea5e-11de-8870-0013722f8dec", "StTxtPara", bldr.ToString()));

			return rgDtoXmls;
		}

		private List<NewDtoInfo> GetDtosForDerivative()
		{
			var rgDtoXmls = new List<NewDtoInfo>();
			var bldr = new StringBuilder();
			bldr.AppendFormat(
				"<rt class=\"LexEntryType\" guid=\"98c273c4-f723-4fb0-80df-eede2204dfca\" ownerguid=\"{0}\">{1}",
				m_mapGuidOwner[LexEntryTypeTags.kguidLexTypDerivation.ToString()], Environment.NewLine);
			bldr.AppendLine("<Abbreviation>");
			bldr.AppendLine("<AUni ws=\"en\">der. of</AUni>");
			bldr.AppendLine("</Abbreviation>");
			bldr.AppendLine("<Description>");
			bldr.AppendLine("<AStr ws=\"en\">");
			bldr.AppendLine("<Run ws=\"en\">A stem that is made up of a root plus an affix that adds a non-inflectional component of meaning.</Run>");
			bldr.AppendLine("</AStr>");
			bldr.AppendLine("</Description>");
			bldr.AppendLine("<IsProtected val=\"true\" />");
			bldr.AppendLine("<Name>");
			bldr.AppendLine("<AUni ws=\"en\">Derivative</AUni>");
			bldr.AppendLine("</Name>");
			bldr.AppendLine("<Discussion>");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b39a75fe-ea5e-11de-8de8-0013722f8dec\" />");
			bldr.AppendLine("</Discussion>");
			bldr.AppendLine("<ReverseAbbr>");
			bldr.AppendLine("<AUni ws=\"en\">der.</AUni>");
			bldr.AppendLine("</ReverseAbbr>");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("98c273c4-f723-4fb0-80df-eede2204dfca", "LexEntryType", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StText\" guid=\"b39a75fe-ea5e-11de-8de8-0013722f8dec\" ownerguid=\"98c273c4-f723-4fb0-80df-eede2204dfca\">");
			bldr.AppendLine("<Paragraphs>");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b3a3ff5c-ea5e-11de-9b21-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b3afeb0a-ea5e-11de-8f7b-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b3bbd6c2-ea5e-11de-9a08-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b3c56020-ea5e-11de-9583-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b3d14bce-ea5e-11de-8c98-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b3dd3786-ea5e-11de-936b-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b3e6c0e4-ea5e-11de-81bf-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b3f2ac92-ea5e-11de-90c6-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b3fe984a-ea5e-11de-9fe0-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b40a83f8-ea5e-11de-8af1-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b4140d56-ea5e-11de-963e-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b41ff904-ea5e-11de-839b-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b42be4bc-ea5e-11de-95fb-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b4356e1a-ea5e-11de-8c84-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b44159c8-ea5e-11de-8dae-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b44d4580-ea5e-11de-9c90-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b456ced4-ea5e-11de-90ca-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b462ba8c-ea5e-11de-8964-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b46ea63a-ea5e-11de-9b58-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b4782f98-ea5e-11de-9d05-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b4841b50-ea5e-11de-9804-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b49006fe-ea5e-11de-9d75-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b499905c-ea5e-11de-8b9e-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b4a57c14-ea5e-11de-87d1-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b4b167c2-ea5e-11de-8214-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b4baf120-ea5e-11de-9c33-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b4c6dcce-ea5e-11de-9d15-0013722f8dec\" />");
			bldr.AppendLine("</Paragraphs>");
			bldr.AppendLine("<DateModified val=\"2011-3-29 16:16:31.321\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b39a75fe-ea5e-11de-8de8-0013722f8dec", "StText", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b3a3ff5c-ea5e-11de-9b21-0013722f8dec\" ownerguid=\"b39a75fe-ea5e-11de-8de8-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\">The derived word often has a different grammatical category from the original. It may thus take the inflectional affixes of the new grammatical category. </Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b3a3ff5c-ea5e-11de-9b21-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b3afeb0a-ea5e-11de-8f7b-0013722f8dec\" ownerguid=\"b39a75fe-ea5e-11de-8de8-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\">In contrast to inflection, derivation</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b3afeb0a-ea5e-11de-8f7b-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b3bbd6c2-ea5e-11de-9a08-0013722f8dec\" ownerguid=\"b39a75fe-ea5e-11de-8de8-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\">• is not obligatory</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b3bbd6c2-ea5e-11de-9a08-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b3c56020-ea5e-11de-9583-0013722f8dec\" ownerguid=\"b39a75fe-ea5e-11de-8de8-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\">• typically produces a greater change of meaning from the original form</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b3c56020-ea5e-11de-9583-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b3d14bce-ea5e-11de-8c98-0013722f8dec\" ownerguid=\"b39a75fe-ea5e-11de-8de8-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\">• is more likely to result in a form which has a somewhat idiosyncratic meaning, and</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b3d14bce-ea5e-11de-8c98-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b3dd3786-ea5e-11de-936b-0013722f8dec\" ownerguid=\"b39a75fe-ea5e-11de-8de8-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\">• often changes the grammatical category of a root.</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b3dd3786-ea5e-11de-936b-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b3e6c0e4-ea5e-11de-81bf-0013722f8dec\" ownerguid=\"b39a75fe-ea5e-11de-8de8-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\">Derivational operations</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b3e6c0e4-ea5e-11de-81bf-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b3f2ac92-ea5e-11de-90c6-0013722f8dec\" ownerguid=\"b39a75fe-ea5e-11de-8de8-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\">• tend to be idiosyncratic and non-productive</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b3f2ac92-ea5e-11de-90c6-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b3fe984a-ea5e-11de-9fe0-0013722f8dec\" ownerguid=\"b39a75fe-ea5e-11de-8de8-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\">• do not occur in well-defined 'paradigms,' and</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b3fe984a-ea5e-11de-9fe0-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b40a83f8-ea5e-11de-8af1-0013722f8dec\" ownerguid=\"b39a75fe-ea5e-11de-8de8-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\">• are 'optional' insofar as they</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b40a83f8-ea5e-11de-8af1-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b4140d56-ea5e-11de-963e-0013722f8dec\" ownerguid=\"b39a75fe-ea5e-11de-8de8-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\"> o shape the basic semantic content of roots and</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b4140d56-ea5e-11de-963e-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b41ff904-ea5e-11de-839b-0013722f8dec\" ownerguid=\"b39a75fe-ea5e-11de-8de8-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\"> o are not governed by some other syntactic operation or element.</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b41ff904-ea5e-11de-839b-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b42be4bc-ea5e-11de-95fb-0013722f8dec\" ownerguid=\"b39a75fe-ea5e-11de-8de8-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\">Examples (English)</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b42be4bc-ea5e-11de-95fb-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b4356e1a-ea5e-11de-8c84-0013722f8dec\" ownerguid=\"b39a75fe-ea5e-11de-8de8-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\">• Kindness is derived from kind.</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b4356e1a-ea5e-11de-8c84-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b44159c8-ea5e-11de-8dae-0013722f8dec\" ownerguid=\"b39a75fe-ea5e-11de-8de8-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\">• Joyful is derived from joy.</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b44159c8-ea5e-11de-8dae-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b44d4580-ea5e-11de-9c90-0013722f8dec\" ownerguid=\"b39a75fe-ea5e-11de-8de8-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\">• Amazement is derived from amaze.</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b44d4580-ea5e-11de-9c90-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b456ced4-ea5e-11de-90ca-0013722f8dec\" ownerguid=\"b39a75fe-ea5e-11de-8de8-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\">• Speaker is derived from speak.</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b456ced4-ea5e-11de-90ca-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b462ba8c-ea5e-11de-8964-0013722f8dec\" ownerguid=\"b39a75fe-ea5e-11de-8de8-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\">• National is derived from nation.</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b462ba8c-ea5e-11de-8964-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b46ea63a-ea5e-11de-9b58-0013722f8dec\" ownerguid=\"b39a75fe-ea5e-11de-8de8-0013722f8dec\">");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b46ea63a-ea5e-11de-9b58-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b4782f98-ea5e-11de-9d05-0013722f8dec\" ownerguid=\"b39a75fe-ea5e-11de-8de8-0013722f8dec\">");
			bldr.AppendLine("<StyleRules>");
			bldr.AppendLine("<Prop namedStyle=\"Normal\" />");
			bldr.AppendLine("</StyleRules>");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\">Kinds</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b4782f98-ea5e-11de-9d05-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b4841b50-ea5e-11de-9804-0013722f8dec\" ownerguid=\"b39a75fe-ea5e-11de-8de8-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\">Here are some kinds of derivational operations:</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b4841b50-ea5e-11de-9804-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b49006fe-ea5e-11de-9d75-0013722f8dec\" ownerguid=\"b39a75fe-ea5e-11de-8de8-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\">• Operations that change the grammatical category of a root</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b49006fe-ea5e-11de-9d75-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b499905c-ea5e-11de-8b9e-0013722f8dec\" ownerguid=\"b39a75fe-ea5e-11de-8de8-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\">Example: Nominalization (English):</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b499905c-ea5e-11de-8b9e-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b4a57c14-ea5e-11de-87d1-0013722f8dec\" ownerguid=\"b39a75fe-ea5e-11de-8de8-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\">Verbs and adjectives can be turned into nouns: amaze &gt; amazement, speak &gt; speaker, perform &gt; performance, soft &gt; softness, warm &gt; warmth</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b4a57c14-ea5e-11de-87d1-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b4b167c2-ea5e-11de-8214-0013722f8dec\" ownerguid=\"b39a75fe-ea5e-11de-8de8-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\">• Operations that change the valency (transitivity) of a root</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b4b167c2-ea5e-11de-8214-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b4baf120-ea5e-11de-9c33-0013722f8dec\" ownerguid=\"b39a75fe-ea5e-11de-8de8-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\">Example: Causation (Swahili):</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b4baf120-ea5e-11de-9c33-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b4c6dcce-ea5e-11de-9d15-0013722f8dec\" ownerguid=\"b39a75fe-ea5e-11de-8de8-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\">kula 'to eat' &gt; kulisha, 'to feed'</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b4c6dcce-ea5e-11de-9d15-0013722f8dec", "StTxtPara", bldr.ToString()));

			return rgDtoXmls;
		}

		private List<NewDtoInfo> GetDtosForIdiom()
		{
			var rgDtoXmls = new List<NewDtoInfo>();
			var bldr = new StringBuilder();
			bldr.AppendFormat(
				"<rt class=\"LexEntryType\" guid=\"b2276dec-b1a6-4d82-b121-fd114c009c59\" ownerguid=\"{0}\">{1}",
				m_mapGuidOwner[LexEntryTypeTags.kguidLexTypIdiom.ToString()], Environment.NewLine);
			bldr.AppendLine("<Abbreviation>");
			bldr.AppendLine("<AUni ws=\"en\">id. of</AUni>");
			bldr.AppendLine("</Abbreviation>");
			bldr.AppendLine("<Description>");
			bldr.AppendLine("<AStr ws=\"en\">");
			bldr.AppendLine("<Run ws=\"en\">A multi-word expression that is recognized as a semantic unit.</Run>");
			bldr.AppendLine("</AStr>");
			bldr.AppendLine("</Description>");
			bldr.AppendLine("<IsProtected val=\"true\" />");
			bldr.AppendLine("<Name>");
			bldr.AppendLine("<AUni ws=\"en\">Idiom</AUni>");
			bldr.AppendLine("</Name>");
			bldr.AppendLine("<Discussion>");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b4d2c886-ea5e-11de-9720-0013722f8dec\" />");
			bldr.AppendLine("</Discussion>");
			bldr.AppendLine("<ReverseAbbr>");
			bldr.AppendLine("<AUni ws=\"en\">id.</AUni>");
			bldr.AppendLine("</ReverseAbbr>");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b2276dec-b1a6-4d82-b121-fd114c009c59", "LexEntryType", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StText\" guid=\"b4d2c886-ea5e-11de-9720-0013722f8dec\" ownerguid=\"b2276dec-b1a6-4d82-b121-fd114c009c59\">");
			bldr.AppendLine("<Paragraphs>");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b4deb434-ea5e-11de-89f0-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b4e83d92-ea5e-11de-993a-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b4f4294a-ea5e-11de-964e-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b50014f8-ea5e-11de-87bd-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b5099e56-ea5e-11de-991f-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b5158a04-ea5e-11de-8832-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b52175bc-ea5e-11de-84c6-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b52aff1a-ea5e-11de-98d1-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b536eac8-ea5e-11de-85aa-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b542d680-ea5e-11de-93c3-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b54c5fde-ea5e-11de-88e8-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b5584b8c-ea5e-11de-9cc4-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b5643744-ea5e-11de-8957-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b56dc098-ea5e-11de-90b0-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b579ac50-ea5e-11de-9f99-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b58597fe-ea5e-11de-8865-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b58f215c-ea5e-11de-9343-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b59b0d14-ea5e-11de-9e7f-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b5a6f8c2-ea5e-11de-90a5-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b5b08220-ea5e-11de-8cc8-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b5bc6dce-ea5e-11de-97b8-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b5c85986-ea5e-11de-8c9a-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b5d1e2e4-ea5e-11de-9e38-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b5ddce92-ea5e-11de-9217-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b5e9ba4a-ea5e-11de-8a9b-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b5f5a5f8-ea5e-11de-9345-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b5ff2f56-ea5e-11de-892e-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b60b1b0e-ea5e-11de-8a7e-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b61706bc-ea5e-11de-9c5f-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b620901a-ea5e-11de-909c-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b62c7bc8-ea5e-11de-8193-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b6386780-ea5e-11de-80d2-0013722f8dec\" />");
			bldr.AppendLine("</Paragraphs>");
			bldr.AppendLine("<DateModified val=\"2011-3-29 16:16:31.321\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b4d2c886-ea5e-11de-9720-0013722f8dec", "StText", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b4deb434-ea5e-11de-89f0-0013722f8dec\" ownerguid=\"b4d2c886-ea5e-11de-9720-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\">An idiom is a multiword expression. Individual components of an idiom can often be inflected in the same way individual words in a phrase can be inflected. This inflection usually follows the same pattern of inflection as the idiom's literal counterpart.</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b4deb434-ea5e-11de-89f0-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b4e83d92-ea5e-11de-993a-0013722f8dec\" ownerguid=\"b4d2c886-ea5e-11de-9720-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\"> Example: have a bee in one's bonnet</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b4e83d92-ea5e-11de-993a-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b4f4294a-ea5e-11de-964e-0013722f8dec\" ownerguid=\"b4d2c886-ea5e-11de-9720-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\"> He has bees in his bonnet.</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b4f4294a-ea5e-11de-964e-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b50014f8-ea5e-11de-87bd-0013722f8dec\" ownerguid=\"b4d2c886-ea5e-11de-9720-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\">An idiom behaves as a single semantic unit.</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b50014f8-ea5e-11de-87bd-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b5099e56-ea5e-11de-991f-0013722f8dec\" ownerguid=\"b4d2c886-ea5e-11de-9720-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\"> It tends to have some measure of internal cohesion such that it can often be replaced by a literal counterpart that is made up of a single word.</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b5099e56-ea5e-11de-991f-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b5158a04-ea5e-11de-8832-0013722f8dec\" ownerguid=\"b4d2c886-ea5e-11de-9720-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\"> Example: kick the bucket</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b5158a04-ea5e-11de-8832-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b52175bc-ea5e-11de-84c6-0013722f8dec\" ownerguid=\"b4d2c886-ea5e-11de-9720-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\"> die</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b52175bc-ea5e-11de-84c6-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b52aff1a-ea5e-11de-98d1-0013722f8dec\" ownerguid=\"b4d2c886-ea5e-11de-9720-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\"> It resists interruption by other words whether they are semantically compatible or not.</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b52aff1a-ea5e-11de-98d1-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b536eac8-ea5e-11de-85aa-0013722f8dec\" ownerguid=\"b4d2c886-ea5e-11de-9720-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\"> Example: pull one's leg</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b536eac8-ea5e-11de-85aa-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b542d680-ea5e-11de-93c3-0013722f8dec\" ownerguid=\"b4d2c886-ea5e-11de-9720-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\"> *pull hard on one's leg</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b542d680-ea5e-11de-93c3-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b54c5fde-ea5e-11de-88e8-0013722f8dec\" ownerguid=\"b4d2c886-ea5e-11de-9720-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\"> *pull on one's left leg</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b54c5fde-ea5e-11de-88e8-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b5584b8c-ea5e-11de-9cc4-0013722f8dec\" ownerguid=\"b4d2c886-ea5e-11de-9720-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\"> It resists reordering of its component parts.</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b5584b8c-ea5e-11de-9cc4-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b5643744-ea5e-11de-8957-0013722f8dec\" ownerguid=\"b4d2c886-ea5e-11de-9720-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\"> Example: let the cat out of the bag</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b5643744-ea5e-11de-8957-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b56dc098-ea5e-11de-90b0-0013722f8dec\" ownerguid=\"b4d2c886-ea5e-11de-9720-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\"> *the cat got left out of the bag</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b56dc098-ea5e-11de-90b0-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b579ac50-ea5e-11de-9f99-0013722f8dec\" ownerguid=\"b4d2c886-ea5e-11de-9720-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\">An idiom has a non-productive syntactic structure. Only single particular lexemes can collocate in an idiomatic construction. Substituting other words from the same generic lexical relation set will destroy the idiomatic meaning of the expression.</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b579ac50-ea5e-11de-9f99-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b58597fe-ea5e-11de-8865-0013722f8dec\" ownerguid=\"b4d2c886-ea5e-11de-9720-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\"> Example: eat one's words</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b58597fe-ea5e-11de-8865-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b58f215c-ea5e-11de-9343-0013722f8dec\" ownerguid=\"b4d2c886-ea5e-11de-9720-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\"> *eat one's sentences</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b58f215c-ea5e-11de-9343-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b59b0d14-ea5e-11de-9e7f-0013722f8dec\" ownerguid=\"b4d2c886-ea5e-11de-9720-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\"> ?swallow one's words</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b59b0d14-ea5e-11de-9e7f-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b5a6f8c2-ea5e-11de-90a5-0013722f8dec\" ownerguid=\"b4d2c886-ea5e-11de-9720-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\">An idiom often shows the following characteristics:</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b5a6f8c2-ea5e-11de-90a5-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b5b08220-ea5e-11de-8cc8-0013722f8dec\" ownerguid=\"b4d2c886-ea5e-11de-9720-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\"> It is syntactically anomalous. It has an unusual grammatical structure.</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b5b08220-ea5e-11de-8cc8-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b5bc6dce-ea5e-11de-97b8-0013722f8dec\" ownerguid=\"b4d2c886-ea5e-11de-9720-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\"> Example: by and large</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b5bc6dce-ea5e-11de-97b8-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b5c85986-ea5e-11de-8c9a-0013722f8dec\" ownerguid=\"b4d2c886-ea5e-11de-9720-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\"> It contains unique, fossilized items.</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b5c85986-ea5e-11de-8c9a-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b5d1e2e4-ea5e-11de-9e38-0013722f8dec\" ownerguid=\"b4d2c886-ea5e-11de-9720-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\"> Examples: to and fro - fro &lt; from = away (Scottish)</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b5d1e2e4-ea5e-11de-9e38-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b5ddce92-ea5e-11de-9217-0013722f8dec\" ownerguid=\"b4d2c886-ea5e-11de-9720-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\"> cobweb - cob &lt; cop = spider (Middle English)</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b5ddce92-ea5e-11de-9217-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b5e9ba4a-ea5e-11de-8a9b-0013722f8dec\" ownerguid=\"b4d2c886-ea5e-11de-9720-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\">Some linguists contend that compound words may qualify as idioms, while others maintain that an idiom must be more lexically complex.</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b5e9ba4a-ea5e-11de-8a9b-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b5f5a5f8-ea5e-11de-9345-0013722f8dec\" ownerguid=\"b4d2c886-ea5e-11de-9720-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\">Idioms contrast with the following:</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b5f5a5f8-ea5e-11de-9345-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b5ff2f56-ea5e-11de-892e-0013722f8dec\" ownerguid=\"b4d2c886-ea5e-11de-9720-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\"> Metaphors satisfy the first requirement for an idiom, that their meaning be obscure, but not the second, that they not be productive.</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b5ff2f56-ea5e-11de-892e-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b60b1b0e-ea5e-11de-8a7e-0013722f8dec\" ownerguid=\"b4d2c886-ea5e-11de-9720-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\"> Examples: throw in the towel</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b60b1b0e-ea5e-11de-8a7e-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b61706bc-ea5e-11de-9c5f-0013722f8dec\" ownerguid=\"b4d2c886-ea5e-11de-9720-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\"> throw in the sponge</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b61706bc-ea5e-11de-9c5f-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b620901a-ea5e-11de-909c-0013722f8dec\" ownerguid=\"b4d2c886-ea5e-11de-9720-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\"> Collocates may have restricted lexical possibilities or use archaic vocabulary such that they are not productive, but their meaning is not opaque.</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b620901a-ea5e-11de-909c-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b62c7bc8-ea5e-11de-8193-0013722f8dec\" ownerguid=\"b4d2c886-ea5e-11de-9720-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\"> Examples: heavy drinking</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b62c7bc8-ea5e-11de-8193-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b6386780-ea5e-11de-80d2-0013722f8dec\" ownerguid=\"b4d2c886-ea5e-11de-9720-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\"> mete out</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b6386780-ea5e-11de-80d2-0013722f8dec", "StTxtPara", bldr.ToString()));

			return rgDtoXmls;
		}

		private List<NewDtoInfo> GetDtosForPhrasalVerb()
		{
			var rgDtoXmls = new List<NewDtoInfo>();
			var bldr = new StringBuilder();
			bldr.AppendFormat(
				"<rt class=\"LexEntryType\" guid=\"35cee792-74c8-444e-a9b7-ed0461d4d3b7\" ownerguid=\"{0}\">{1}",
				m_mapGuidOwner[LexEntryTypeTags.kguidLexTypPhrasalVerb.ToString()], Environment.NewLine);
			bldr.AppendLine("<Abbreviation>");
			bldr.AppendLine("<AUni ws=\"en\">ph. v. of</AUni>");
			bldr.AppendLine("</Abbreviation>");
			bldr.AppendLine("<Description>");
			bldr.AppendLine("<AStr ws=\"en\">");
			bldr.AppendLine("<Run ws=\"en\">A combination of a lexical verb and a verbal particle that forms a single semantic and syntactic unit.</Run>");
			bldr.AppendLine("</AStr>");
			bldr.AppendLine("</Description>");
			bldr.AppendLine("<IsProtected val=\"true\" />");
			bldr.AppendLine("<Name>");
			bldr.AppendLine("<AUni ws=\"en\">Phrasal Verb</AUni>");
			bldr.AppendLine("</Name>");
			bldr.AppendLine("<Discussion>");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b641f0de-ea5e-11de-9395-0013722f8dec\" />");
			bldr.AppendLine("</Discussion>");
			bldr.AppendLine("<ReverseAbbr>");
			bldr.AppendLine("<AUni ws=\"en\">ph. v.</AUni>");
			bldr.AppendLine("</ReverseAbbr>");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("35cee792-74c8-444e-a9b7-ed0461d4d3b7", "LexEntryType", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StText\" guid=\"b641f0de-ea5e-11de-9395-0013722f8dec\" ownerguid=\"35cee792-74c8-444e-a9b7-ed0461d4d3b7\">");
			bldr.AppendLine("<Paragraphs>");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b64ddc8c-ea5e-11de-8425-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b659c844-ea5e-11de-9e3d-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b6635198-ea5e-11de-823b-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b66f3d50-ea5e-11de-8344-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b67b28fe-ea5e-11de-88a1-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b684b25c-ea5e-11de-89fc-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b6909e14-ea5e-11de-884a-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b69c89c2-ea5e-11de-8a6c-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b6a61320-ea5e-11de-96db-0013722f8dec\" />");
			bldr.AppendLine("</Paragraphs>");
			bldr.AppendLine("<DateModified val=\"2011-3-29 16:16:31.321\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b641f0de-ea5e-11de-9395-0013722f8dec", "StText", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b64ddc8c-ea5e-11de-8425-0013722f8dec\" ownerguid=\"b641f0de-ea5e-11de-9395-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\">Example (English)</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b64ddc8c-ea5e-11de-8425-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b659c844-ea5e-11de-9e3d-0013722f8dec\" ownerguid=\"b641f0de-ea5e-11de-9395-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\"> The item ‘give up’ is a phrasal verb, as in the following:</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b659c844-ea5e-11de-9e3d-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b6635198-ea5e-11de-823b-0013722f8dec\" ownerguid=\"b641f0de-ea5e-11de-9395-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\"> He gave up smoking.</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b6635198-ea5e-11de-823b-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b66f3d50-ea5e-11de-8344-0013722f8dec\" ownerguid=\"b641f0de-ea5e-11de-9395-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\"> He gave smoking up.</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b66f3d50-ea5e-11de-8344-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b67b28fe-ea5e-11de-88a1-0013722f8dec\" ownerguid=\"b641f0de-ea5e-11de-9395-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\">Example (Akan)</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b67b28fe-ea5e-11de-88a1-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b684b25c-ea5e-11de-89fc-0013722f8dec\" ownerguid=\"b641f0de-ea5e-11de-9395-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\"> The item gyee ... so 'answered' is a phrasal verb, as in the following:</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b684b25c-ea5e-11de-89fc-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b6909e14-ea5e-11de-884a-0013722f8dec\" ownerguid=\"b641f0de-ea5e-11de-9395-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\"> Kofi gyee Kwame so</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b6909e14-ea5e-11de-884a-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b69c89c2-ea5e-11de-8a6c-0013722f8dec\" ownerguid=\"b641f0de-ea5e-11de-9395-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\"> Kofi received Kwame on</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b69c89c2-ea5e-11de-8a6c-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b6a61320-ea5e-11de-96db-0013722f8dec\" ownerguid=\"b641f0de-ea5e-11de-9395-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\"> 'Kofi answered Kwame.'</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b6a61320-ea5e-11de-96db-0013722f8dec", "StTxtPara", bldr.ToString()));

			return rgDtoXmls;
		}

		private List<NewDtoInfo> GetDtosForSaying()
		{
			var rgDtoXmls = new List<NewDtoInfo>();
			var bldr = new StringBuilder();
			bldr.AppendFormat(
				"<rt class=\"LexEntryType\" guid=\"9466d126-246e-400b-8bba-0703e09bc567\" ownerguid=\"{0}\">{1}",
				m_mapGuidOwner[LexEntryTypeTags.kguidLexTypSaying.ToString()], Environment.NewLine);
			bldr.AppendLine("<Abbreviation>");
			bldr.AppendLine("<AUni ws=\"en\">say. of</AUni>");
			bldr.AppendLine("</Abbreviation>");
			bldr.AppendLine("<Description>");
			bldr.AppendLine("<AStr ws=\"en\">");
			bldr.AppendLine("<Run ws=\"en\">Any pithy phrasal expression of wisdom or truth; esp., an adage, proverb, or maxim.</Run>");
			bldr.AppendLine("</AStr>");
			bldr.AppendLine("</Description>");
			bldr.AppendLine("<IsProtected val=\"true\" />");
			bldr.AppendLine("<Name>");
			bldr.AppendLine("<AUni ws=\"en\">Saying</AUni>");
			bldr.AppendLine("</Name>");
			bldr.AppendLine("<Discussion>");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b6b1fed8-ea5e-11de-8d97-0013722f8dec\" />");
			bldr.AppendLine("</Discussion>");
			bldr.AppendLine("<ReverseAbbr>");
			bldr.AppendLine("<AUni ws=\"en\">say.</AUni>");
			bldr.AppendLine("</ReverseAbbr>");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("9466d126-246e-400b-8bba-0703e09bc567", "LexEntryType", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StText\" guid=\"b6b1fed8-ea5e-11de-8d97-0013722f8dec\" ownerguid=\"9466d126-246e-400b-8bba-0703e09bc567\">");
			bldr.AppendLine("<Paragraphs>");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b6bdea86-ea5e-11de-9926-0013722f8dec\" />");
			bldr.AppendLine("</Paragraphs>");
			bldr.AppendLine("<DateModified val=\"2011-3-29 16:16:31.321\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b6b1fed8-ea5e-11de-8d97-0013722f8dec", "StText", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b6bdea86-ea5e-11de-9926-0013722f8dec\" ownerguid=\"b6b1fed8-ea5e-11de-8d97-0013722f8dec\">");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b6bdea86-ea5e-11de-9926-0013722f8dec", "StTxtPara", bldr.ToString()));

			return rgDtoXmls;
		}

		private List<NewDtoInfo> GetDtosForDialectalVar()
		{
			var rgDtoXmls = new List<NewDtoInfo>();
			var bldr = new StringBuilder();
			bldr.AppendFormat(
				"<rt class=\"LexEntryType\" guid=\"024b62c9-93b3-41a0-ab19-587a0030219a\" ownerguid=\"{0}\">{1}",
				m_mapGuidOwner[LexEntryTypeTags.kguidLexTypDialectalVar.ToString()], Environment.NewLine);
			bldr.AppendLine("<Abbreviation>");
			bldr.AppendLine("<AUni ws=\"en\">dial. var. of</AUni>");
			bldr.AppendLine("</Abbreviation>");
			bldr.AppendLine("<Description>");
			bldr.AppendLine("<AStr ws=\"en\">");
			bldr.AppendLine("<Run ws=\"en\">A variant of a lexeme, characteristically used by a specific demographic subset of the language.</Run>");
			bldr.AppendLine("</AStr>");
			bldr.AppendLine("</Description>");
			bldr.AppendLine("<IsProtected val=\"true\" />");
			bldr.AppendLine("<Name>");
			bldr.AppendLine("<AUni ws=\"en\">Dialectal Variant</AUni>");
			bldr.AppendLine("</Name>");
			bldr.AppendLine("<Discussion>");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b6c773e4-ea5e-11de-9774-0013722f8dec\" />");
			bldr.AppendLine("</Discussion>");
			bldr.AppendLine("<ReverseAbbr>");
			bldr.AppendLine("<AUni ws=\"en\">dial. var.</AUni>");
			bldr.AppendLine("</ReverseAbbr>");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("024b62c9-93b3-41a0-ab19-587a0030219a", "LexEntryType", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StText\" guid=\"b6c773e4-ea5e-11de-9774-0013722f8dec\" ownerguid=\"024b62c9-93b3-41a0-ab19-587a0030219a\">");
			bldr.AppendLine("<Paragraphs>");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b6d35f92-ea5e-11de-9aef-0013722f8dec\" />");
			bldr.AppendLine("</Paragraphs>");
			bldr.AppendLine("<DateModified val=\"2011-3-29 16:16:31.321\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b6c773e4-ea5e-11de-9774-0013722f8dec", "StText", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b6d35f92-ea5e-11de-9aef-0013722f8dec\" ownerguid=\"b6c773e4-ea5e-11de-9774-0013722f8dec\">");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b6d35f92-ea5e-11de-9aef-0013722f8dec", "StTxtPara", bldr.ToString()));

			return rgDtoXmls;
		}

		private List<NewDtoInfo> GetDtosForFreeVar()
		{
			var rgDtoXmls = new List<NewDtoInfo>();
			var bldr = new StringBuilder();
			bldr.AppendFormat(
				"<rt class=\"LexEntryType\" guid=\"4343b1ef-b54f-4fa4-9998-271319a6d74c\" ownerguid=\"{0}\">{1}",
				m_mapGuidOwner[LexEntryTypeTags.kguidLexTypFreeVar.ToString()], Environment.NewLine);
			bldr.AppendLine("<Abbreviation>");
			bldr.AppendLine("<AUni ws=\"en\">fr. var. of</AUni>");
			bldr.AppendLine("</Abbreviation>");
			bldr.AppendLine("<Description>");
			bldr.AppendLine("<AStr ws=\"en\">");
			bldr.AppendLine("<Run ws=\"en\">If two forms are free variants, the same speaker might use either one in the same setting. The more frequent form would be considered the basic form.</Run>");
			bldr.AppendLine("</AStr>");
			bldr.AppendLine("</Description>");
			bldr.AppendLine("<IsProtected val=\"true\" />");
			bldr.AppendLine("<Name>");
			bldr.AppendLine("<AUni ws=\"en\">Free Variant</AUni>");
			bldr.AppendLine("</Name>");
			bldr.AppendLine("<Discussion>");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b6df4b4a-ea5e-11de-8285-0013722f8dec\" />");
			bldr.AppendLine("</Discussion>");
			bldr.AppendLine("<ReverseAbbr>");
			bldr.AppendLine("<AUni ws=\"en\">fr. var.</AUni>");
			bldr.AppendLine("</ReverseAbbr>");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("4343b1ef-b54f-4fa4-9998-271319a6d74c", "LexEntryType", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StText\" guid=\"b6df4b4a-ea5e-11de-8285-0013722f8dec\" ownerguid=\"4343b1ef-b54f-4fa4-9998-271319a6d74c\">");
			bldr.AppendLine("<Paragraphs>");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b6eb36f8-ea5e-11de-95d2-0013722f8dec\" />");
			bldr.AppendLine("</Paragraphs>");
			bldr.AppendLine("<DateModified val=\"2011-3-29 16:16:31.321\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b6df4b4a-ea5e-11de-8285-0013722f8dec", "StText", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b6eb36f8-ea5e-11de-95d2-0013722f8dec\" ownerguid=\"b6df4b4a-ea5e-11de-8285-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\">Linguists generally assume that all differences in form are motivated in some way at some level. If two variants were truly free, there would be no factors that condition the choice of either variant and the choice would be totally random. However in many cases one variant is more common than the other, indicating that there is some conditioning factor at work. The apparent lack of conditioning factors may be due to a historical change that is in process (with the result that the older rule is being inconsistently applied), competing factors that mask each other, or such complex factors that the linguist is unable to sort out the conditioning. The result is that the two forms may appear to be in free variation, but may actually be conditioned. When no conditioning factor can be discerned, the general practice in linguistics is to label the forms Free Variants.</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b6eb36f8-ea5e-11de-95d2-0013722f8dec", "StTxtPara", bldr.ToString()));

			return rgDtoXmls;
		}

		private List<NewDtoInfo> GetDtosForIrregInflVar()
		{
			var rgDtoXmls = new List<NewDtoInfo>();
			var bldr = new StringBuilder();
			bldr.AppendFormat(
				"<rt class=\"LexEntryType\" guid=\"01d4fbc1-3b0c-4f52-9163-7ab0d4f4711c\" ownerguid=\"{0}\">{1}",
				m_mapGuidOwner[LexEntryTypeTags.kguidLexTypIrregInflectionVar.ToString()], Environment.NewLine);
			bldr.AppendLine("<Abbreviation>");
			bldr.AppendLine("<AUni ws=\"en\">irreg. infl. of</AUni>");
			bldr.AppendLine("</Abbreviation>");
			bldr.AppendLine("<Description>");
			bldr.AppendLine("<AStr ws=\"en\">");
			bldr.AppendLine("<Run ws=\"en\">An Irregularly Inflected Form is an inflected form of the lexeme that is different from what you would expect from the normal rules of the grammar.</Run>");
			bldr.AppendLine("</AStr>");
			bldr.AppendLine("</Description>");
			bldr.AppendLine("<IsProtected val=\"true\" />");
			bldr.AppendLine("<Name>");
			bldr.AppendLine("<AUni ws=\"en\">Irregularly Inflected Form</AUni>");
			bldr.AppendLine("</Name>");
			bldr.AppendLine("<Discussion>");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b6f4c056-ea5e-11de-8a9c-0013722f8dec\" />");
			bldr.AppendLine("</Discussion>");
			bldr.AppendLine("<SubPossibilities>");
			bldr.AppendLine("<objsur t=\"o\" guid=\"a32f1d1c-4832-46a2-9732-c2276d6547e8\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"837ebe72-8c1d-4864-95d9-fa313c499d78\" />");
			bldr.AppendLine("</SubPossibilities>");
			bldr.AppendLine("<ReverseAbbr>");
			bldr.AppendLine("<AUni ws=\"en\">irreg. infl.</AUni>");
			bldr.AppendLine("</ReverseAbbr>");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("01d4fbc1-3b0c-4f52-9163-7ab0d4f4711c", "LexEntryType", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StText\" guid=\"b6f4c056-ea5e-11de-8a9c-0013722f8dec\" ownerguid=\"01d4fbc1-3b0c-4f52-9163-7ab0d4f4711c\">");
			bldr.AppendLine("<Paragraphs>");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b700ac0e-ea5e-11de-8890-0013722f8dec\" />");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b70c97bc-ea5e-11de-803e-0013722f8dec\" />");
			bldr.AppendLine("</Paragraphs>");
			bldr.AppendLine("<DateModified val=\"2011-3-29 16:16:31.321\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b6f4c056-ea5e-11de-8a9c-0013722f8dec", "StText", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b700ac0e-ea5e-11de-8890-0013722f8dec\" ownerguid=\"b6f4c056-ea5e-11de-8a9c-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\">The English verb 'hit' is irregular. From the normal rules of grammar we would expect the past tense to be 'hitted', since the normal way to form the past tense is to add the -ed suffix. Instead the correct past tense form is 'hit', as in, \"He hit me yesterday.\" Likewise the past tense of 'break' is 'broke' rather than 'breaked'. So 'hit' (past) and 'broke' are irregularly inflected forms. In the same way the plural form of 'fish' is 'fish' rather than 'fishes', as in \"There are many fish in the lake.\" The plural of 'man' is 'men', not 'mans'. So 'fish' (plural) and 'men' are irregularly inflected forms.</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b700ac0e-ea5e-11de-8890-0013722f8dec", "StTxtPara", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b70c97bc-ea5e-11de-803e-0013722f8dec\" ownerguid=\"b6f4c056-ea5e-11de-8a9c-0013722f8dec\">");
			bldr.AppendLine("<Contents>");
			bldr.AppendLine("<Str>");
			bldr.AppendLine("<Run ws=\"en\">Many dictionaries indicate irregularly inflected forms in the dictionary entry for the lexeme. Generally a minor entry for the irregularly inflected form is also included, especially in dictionaries for language learners who would not know where to find the entry for the basic (uninflected) form of the lexeme.</Run>");
			bldr.AppendLine("</Str>");
			bldr.AppendLine("</Contents>");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b70c97bc-ea5e-11de-803e-0013722f8dec", "StTxtPara", bldr.ToString()));

			return rgDtoXmls;
		}

		private List<NewDtoInfo> GetDtosForPluralVar()
		{
			var rgDtoXmls = new List<NewDtoInfo>();
			var bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"LexEntryType\" guid=\"a32f1d1c-4832-46a2-9732-c2276d6547e8\" ownerguid=\"01d4fbc1-3b0c-4f52-9163-7ab0d4f4711c\">");
			bldr.AppendLine("<Abbreviation>");
			bldr.AppendLine("<AUni ws=\"en\">pl. of</AUni>");
			bldr.AppendLine("</Abbreviation>");
			bldr.AppendLine("<Description>");
			bldr.AppendLine("<AStr ws=\"en\">");
			bldr.AppendLine("<Run ws=\"en\">The plural form of a noun that does not take the regular inflectional affix for plural.</Run>");
			bldr.AppendLine("</AStr>");
			bldr.AppendLine("</Description>");
			bldr.AppendLine("<IsProtected val=\"true\" />");
			bldr.AppendLine("<Name>");
			bldr.AppendLine("<AUni ws=\"en\">Plural</AUni>");
			bldr.AppendLine("</Name>");
			bldr.AppendLine("<Discussion>");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b716211a-ea5e-11de-8768-0013722f8dec\" />");
			bldr.AppendLine("</Discussion>");
			bldr.AppendLine("<ReverseAbbr>");
			bldr.AppendLine("<AUni ws=\"en\">pl.</AUni>");
			bldr.AppendLine("</ReverseAbbr>");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("a32f1d1c-4832-46a2-9732-c2276d6547e8", "LexEntryType", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StText\" guid=\"b716211a-ea5e-11de-8768-0013722f8dec\" ownerguid=\"a32f1d1c-4832-46a2-9732-c2276d6547e8\">");
			bldr.AppendLine("<Paragraphs>");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b7220cc8-ea5e-11de-8bbc-0013722f8dec\" />");
			bldr.AppendLine("</Paragraphs>");
			bldr.AppendLine("<DateModified val=\"2011-3-29 16:16:31.321\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b716211a-ea5e-11de-8768-0013722f8dec", "StText", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b7220cc8-ea5e-11de-8bbc-0013722f8dec\" ownerguid=\"b716211a-ea5e-11de-8768-0013722f8dec\">");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b7220cc8-ea5e-11de-8bbc-0013722f8dec", "StTxtPara", bldr.ToString()));

			return rgDtoXmls;
		}

		private List<NewDtoInfo> GetDtosForPastVar()
		{
			var rgDtoXmls = new List<NewDtoInfo>();
			var bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"LexEntryType\" guid=\"837ebe72-8c1d-4864-95d9-fa313c499d78\" ownerguid=\"01d4fbc1-3b0c-4f52-9163-7ab0d4f4711c\">");
			bldr.AppendLine("<Abbreviation>");
			bldr.AppendLine("<AUni ws=\"en\">pst. of</AUni>");
			bldr.AppendLine("</Abbreviation>");
			bldr.AppendLine("<Description>");
			bldr.AppendLine("<AStr ws=\"en\">");
			bldr.AppendLine("<Run ws=\"en\">The past tense form of a verb that does not take the regular inflectional affix for past tense.</Run>");
			bldr.AppendLine("</AStr>");
			bldr.AppendLine("</Description>");
			bldr.AppendLine("<IsProtected val=\"true\" />");
			bldr.AppendLine("<Name>");
			bldr.AppendLine("<AUni ws=\"en\">Past</AUni>");
			bldr.AppendLine("</Name>");
			bldr.AppendLine("<Discussion>");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b72df880-ea5e-11de-8e18-0013722f8dec\" />");
			bldr.AppendLine("</Discussion>");
			bldr.AppendLine("<ReverseAbbr>");
			bldr.AppendLine("<AUni ws=\"en\">pst.</AUni>");
			bldr.AppendLine("</ReverseAbbr>");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("837ebe72-8c1d-4864-95d9-fa313c499d78", "LexEntryType", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StText\" guid=\"b72df880-ea5e-11de-8e18-0013722f8dec\" ownerguid=\"837ebe72-8c1d-4864-95d9-fa313c499d78\">");
			bldr.AppendLine("<Paragraphs>");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b73781de-ea5e-11de-9216-0013722f8dec\" />");
			bldr.AppendLine("</Paragraphs>");
			bldr.AppendLine("<DateModified val=\"2011-3-29 16:16:31.321\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b72df880-ea5e-11de-8e18-0013722f8dec", "StText", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b73781de-ea5e-11de-9216-0013722f8dec\" ownerguid=\"b72df880-ea5e-11de-8e18-0013722f8dec\">");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b73781de-ea5e-11de-9216-0013722f8dec", "StTxtPara", bldr.ToString()));

			return rgDtoXmls;
		}

		private List<NewDtoInfo> GetDtosForSpellingVariant()
		{
			var rgDtoXmls = new List<NewDtoInfo>();
			var bldr = new StringBuilder();
			bldr.AppendFormat(
				"<rt class=\"LexEntryType\" guid=\"0c4663b3-4d9a-47af-b9a1-c8565d8112ed\" ownerguid=\"{0}\">{1}",
				m_mapGuidOwner[LexEntryTypeTags.kguidLexTypSpellingVar.ToString()], Environment.NewLine);
			bldr.AppendLine("<Abbreviation>");
			bldr.AppendLine("<AUni ws=\"en\">sp. var. of</AUni>");
			bldr.AppendLine("</Abbreviation>");
			bldr.AppendLine("<Description>");
			bldr.AppendLine("<AStr ws=\"en\">");
			bldr.AppendLine("<Run ws=\"en\">A variant spelling of a lexeme.</Run>");
			bldr.AppendLine("</AStr>");
			bldr.AppendLine("</Description>");
			bldr.AppendLine("<IsProtected val=\"true\" />");
			bldr.AppendLine("<Name>");
			bldr.AppendLine("<AUni ws=\"en\">Spelling Variant</AUni>");
			bldr.AppendLine("</Name>");
			bldr.AppendLine("<Discussion>");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b7436d8c-ea5e-11de-8477-0013722f8dec\" />");
			bldr.AppendLine("</Discussion>");
			bldr.AppendLine("<ReverseAbbr>");
			bldr.AppendLine("<AUni ws=\"en\">sp. var.</AUni>");
			bldr.AppendLine("</ReverseAbbr>");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("0c4663b3-4d9a-47af-b9a1-c8565d8112ed", "LexEntryType", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StText\" guid=\"b7436d8c-ea5e-11de-8477-0013722f8dec\" ownerguid=\"0c4663b3-4d9a-47af-b9a1-c8565d8112ed\">");
			bldr.AppendLine("<Paragraphs>");
			bldr.AppendLine("<objsur t=\"o\" guid=\"b74f5944-ea5e-11de-9017-0013722f8dec\" />");
			bldr.AppendLine("</Paragraphs>");
			bldr.AppendLine("<DateModified val=\"2011-3-29 16:16:31.321\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b7436d8c-ea5e-11de-8477-0013722f8dec", "StText", bldr.ToString()));

			bldr = new StringBuilder();
			bldr.AppendLine("<rt class=\"StTxtPara\" guid=\"b74f5944-ea5e-11de-9017-0013722f8dec\" ownerguid=\"b7436d8c-ea5e-11de-8477-0013722f8dec\">");
			bldr.AppendLine("<ParseIsCurrent val=\"False\" />");
			bldr.AppendLine("</rt>");
			rgDtoXmls.Add(new NewDtoInfo("b74f5944-ea5e-11de-9017-0013722f8dec", "StTxtPara", bldr.ToString()));

			return rgDtoXmls;
		}

		private void BuildStandardMaps(string guidComplexTypes, string guidVariantTypes)
		{
			m_mapGuidOwner.Add(LexEntryTypeTags.kguidLexTypCompound.ToString(), guidComplexTypes);
			m_mapGuidName.Add(LexEntryTypeTags.kguidLexTypCompound.ToString(), "Compound");
			m_mapNameGuid.Add("Compound", LexEntryTypeTags.kguidLexTypCompound.ToString());

			m_mapGuidOwner.Add(LexEntryTypeTags.kguidLexTypContraction.ToString(), guidComplexTypes);
			m_mapGuidName.Add(LexEntryTypeTags.kguidLexTypContraction.ToString(), "Contraction");
			m_mapNameGuid.Add("Contraction", LexEntryTypeTags.kguidLexTypContraction.ToString());

			m_mapGuidOwner.Add(LexEntryTypeTags.kguidLexTypDerivation.ToString(), guidComplexTypes);
			m_mapGuidName.Add(LexEntryTypeTags.kguidLexTypDerivation.ToString(), "Derivative");
			m_mapNameGuid.Add("Derivative", LexEntryTypeTags.kguidLexTypDerivation.ToString());

			m_mapGuidOwner.Add(LexEntryTypeTags.kguidLexTypIdiom.ToString(), guidComplexTypes);
			m_mapGuidName.Add(LexEntryTypeTags.kguidLexTypIdiom.ToString(), "Idiom");
			m_mapNameGuid.Add("Idiom", LexEntryTypeTags.kguidLexTypIdiom.ToString());

			m_mapGuidOwner.Add(LexEntryTypeTags.kguidLexTypPhrasalVerb.ToString(), guidComplexTypes);
			m_mapGuidName.Add(LexEntryTypeTags.kguidLexTypPhrasalVerb.ToString(), "Phrasal Verb");
			m_mapNameGuid.Add("Phrasal Verb", LexEntryTypeTags.kguidLexTypPhrasalVerb.ToString());

			m_mapGuidOwner.Add(LexEntryTypeTags.kguidLexTypSaying.ToString(), guidComplexTypes);
			m_mapGuidName.Add(LexEntryTypeTags.kguidLexTypSaying.ToString(), "Saying");
			m_mapNameGuid.Add("Saying", LexEntryTypeTags.kguidLexTypSaying.ToString());

			m_mapGuidOwner.Add(LexEntryTypeTags.kguidLexTypDialectalVar.ToString(), guidVariantTypes);
			m_mapGuidName.Add(LexEntryTypeTags.kguidLexTypDialectalVar.ToString(), "Dialectal Variant");
			m_mapNameGuid.Add("Dialectal Variant", LexEntryTypeTags.kguidLexTypDialectalVar.ToString());

			m_mapGuidOwner.Add(LexEntryTypeTags.kguidLexTypFreeVar.ToString(), guidVariantTypes);
			m_mapGuidName.Add(LexEntryTypeTags.kguidLexTypFreeVar.ToString(), "Free Variant");
			m_mapNameGuid.Add("Free Variant", LexEntryTypeTags.kguidLexTypFreeVar.ToString());

			m_mapGuidOwner.Add(LexEntryTypeTags.kguidLexTypIrregInflectionVar.ToString(), guidVariantTypes);
			m_mapGuidName.Add(LexEntryTypeTags.kguidLexTypIrregInflectionVar.ToString(), "Irregularly Inflected Form");
			m_mapNameGuid.Add("Irregularly Inflected Form", LexEntryTypeTags.kguidLexTypIrregInflectionVar.ToString());

			m_mapGuidOwner.Add(LexEntryTypeTags.kguidLexTypPluralVar.ToString(),
				LexEntryTypeTags.kguidLexTypIrregInflectionVar.ToString());
			m_mapGuidName.Add(LexEntryTypeTags.kguidLexTypPluralVar.ToString(), "Plural");
			m_mapNameGuid.Add("Plural", LexEntryTypeTags.kguidLexTypPluralVar.ToString());

			m_mapGuidOwner.Add(LexEntryTypeTags.kguidLexTypPastVar.ToString(),
				LexEntryTypeTags.kguidLexTypIrregInflectionVar.ToString());
			m_mapGuidName.Add(LexEntryTypeTags.kguidLexTypPastVar.ToString(), "Past");
			m_mapNameGuid.Add("Past", LexEntryTypeTags.kguidLexTypPastVar.ToString());

			m_mapGuidOwner.Add(LexEntryTypeTags.kguidLexTypSpellingVar.ToString(), guidVariantTypes);
			m_mapGuidName.Add(LexEntryTypeTags.kguidLexTypSpellingVar.ToString(), "Spelling Variant");
			m_mapNameGuid.Add("Spelling Variant", LexEntryTypeTags.kguidLexTypSpellingVar.ToString());
		}

		private static string GetGuidValue(XNode xe, string xpath)
		{
			var xeObjsur = xe.XPathSelectElement(xpath);
			if (xeObjsur == null)
				return null;
			var xaGuid = xeObjsur.Attribute("guid");
			return xaGuid != null ? xaGuid.Value : null;
		}

		/// <summary>
		/// This stores the dto and parsed XElement for a LexEntryType that doesn't have a
		/// standard guid, or that doesn't have the correct IsProtected value.
		/// </summary>
		internal class LexTypeInfo
		{
			public DomainObjectDTO DTO { get; set; }
			public XElement XmlElement { get; private set; }

			internal LexTypeInfo(DomainObjectDTO dto, XElement xe)
			{
				DTO = dto;
				XmlElement = xe;
			}
		}

		/// <summary>
		/// This stores the information needed to create a new object's DTO.
		/// </summary>
		internal class NewDtoInfo
		{
			public string Guid { get; private set; }
			public string ClassName { get; private set; }
			public string Xml { get; private set; }

			internal NewDtoInfo(string guid, string className, string xml)
			{
				Guid = guid;
				ClassName = className;
				Xml = xml;
			}
		}
	}
}
