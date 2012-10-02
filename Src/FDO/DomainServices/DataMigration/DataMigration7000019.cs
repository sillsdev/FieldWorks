using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Palaso.WritingSystems;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// <summary>
	/// Migrates from 7000018 to 7000019
	/// </summary>
	internal class DataMigration7000019 : IDataMigration
	{
		#region Implementation of IDataMigration

		/// <summary>
		/// Perform one increment migration step.
		/// </summary>
		/// <param name="domainObjectDtoRepository">Repository of all CmObject DTOs available for one migration step.</param>
		/// <remarks>
		/// The method must add/remove/update the DTOs to the repository,
		/// as it adds/removes objects as part of it work.
		/// Implementors of this interface should ensure the Repository's
		/// starting model version number is correct for the step.
		/// Implementors must also increment the Repository's model version number
		/// at the end of its migration work.
		/// The method also should normally modify the xml string(s)
		/// of relevant DTOs, since that string will be used by the main
		/// data migration calling client (ie. BEP).
		/// </remarks>
		public void PerformMigration(IDomainObjectDTORepository domainObjectDtoRepository)
		{
			DataMigrationServices.CheckVersionNumber(domainObjectDtoRepository, 7000018);

			// collect all writing system info
			var guidToWsInfo = new Dictionary<string, Tuple<string, DomainObjectDTO, XElement>>();
			foreach (DomainObjectDTO wsDto in domainObjectDtoRepository.AllInstancesSansSubclasses("LgWritingSystem").ToArray())
			{
				XElement wsElem = XElement.Parse(wsDto.Xml);
				XElement icuLocaleElem = wsElem.Element("ICULocale");
				var icuLocale = icuLocaleElem.Element("Uni").Value;
				string langTag = LangTagUtils.ToLangTag(icuLocale);
				guidToWsInfo[wsDto.Guid.ToLowerInvariant()] = Tuple.Create(langTag, wsDto, wsElem);
			}

			// remove all CmSortSpec objects
			foreach (DomainObjectDTO sortSpecDto in domainObjectDtoRepository.AllInstancesSansSubclasses("CmSortSpec").ToArray())
				domainObjectDtoRepository.Remove(sortSpecDto);

			// remove SortSpecs property from LangProject
			DomainObjectDTO lpDto = domainObjectDtoRepository.AllInstancesSansSubclasses("LangProject").First();
			XElement lpElem = XElement.Parse(lpDto.Xml);
			XElement sortSpecsElem = lpElem.Element("SortSpecs");
			bool lpModified = false;
			if (sortSpecsElem != null)
			{
				sortSpecsElem.Remove();
				lpModified = true;
			}

			var referencedWsIds = new HashSet<string>();

			// convert all LangProject writing system references to strings
			if (ConvertRefToString(lpElem.Element("AnalysisWss"), guidToWsInfo, referencedWsIds))
				lpModified = true;
			if (ConvertRefToString(lpElem.Element("VernWss"), guidToWsInfo, referencedWsIds))
				lpModified = true;
			if (ConvertRefToString(lpElem.Element("CurAnalysisWss"), guidToWsInfo, referencedWsIds))
				lpModified = true;
			if (ConvertRefToString(lpElem.Element("CurPronunWss"), guidToWsInfo, referencedWsIds))
				lpModified = true;
			if (ConvertRefToString(lpElem.Element("CurVernWss"), guidToWsInfo, referencedWsIds))
				lpModified = true;
			if (lpModified)
				DataMigrationServices.UpdateDTO(domainObjectDtoRepository, lpDto, lpElem.ToString());

			// convert all ReversalIndex writing system references to strings
			ConvertAllRefsToStrings(domainObjectDtoRepository, "ReversalIndex", guidToWsInfo, referencedWsIds);

			// convert all WordformLookupList writing system references to strings
			ConvertAllRefsToStrings(domainObjectDtoRepository, "WordformLookupList", guidToWsInfo, referencedWsIds);

			// convert all CmPossibilityList writing system references to strings
			ConvertAllRefsToStrings(domainObjectDtoRepository, "CmPossibilityList", guidToWsInfo, referencedWsIds);

			// convert all UserViewField writing system references to strings
			ConvertAllRefsToStrings(domainObjectDtoRepository, "UserViewField", guidToWsInfo, referencedWsIds);

			// convert all CmBaseAnnotation writing system references to strings
			ConvertAllRefsToStrings(domainObjectDtoRepository, "CmBaseAnnotation", guidToWsInfo, referencedWsIds);

			// convert all FsOpenFeature writing system references to strings
			ConvertAllRefsToStrings(domainObjectDtoRepository, "FsOpenFeature", guidToWsInfo, referencedWsIds);

			// convert all ScrMarkerMapping ICU locales to lang tags
			ConvertAllIcuLocalesToLangTags(domainObjectDtoRepository, "ScrMarkerMapping", referencedWsIds);

			// convert all ScrImportSource ICU locales to lang tags
			ConvertAllIcuLocalesToLangTags(domainObjectDtoRepository, "ScrImportSource", referencedWsIds);

			// convert all ICU locales to Language Tags and remove legacy magic font names
			foreach (DomainObjectDTO dto in domainObjectDtoRepository.AllInstances())
				UpdateStringsAndProps(domainObjectDtoRepository, dto, referencedWsIds);

			PalasoWritingSystemManager wsManager = null;
			try
			{
				if (string.IsNullOrEmpty(domainObjectDtoRepository.ProjectFolder))
				{
					wsManager = new PalasoWritingSystemManager();
				}

				else
				{
					var globalStore = new GlobalFileWritingSystemStore(DirectoryFinder.GlobalWritingSystemStoreDirectory);
					string storePath = Path.Combine(domainObjectDtoRepository.ProjectFolder, DirectoryFinder.ksWritingSystemsDir);
					wsManager = new PalasoWritingSystemManager(new LocalFileWritingSystemStore(storePath, globalStore), globalStore);
				}

				IEqualityComparer<IWritingSystem> wsComparer = new WsIdEqualityComparer();
				foreach (Tuple<string, DomainObjectDTO, XElement> wsInfo in guidToWsInfo.Values)
				{
					if (referencedWsIds.Contains(wsInfo.Item1))
					{
						IWritingSystem ws;
						if (!wsManager.GetOrSet(wsInfo.Item1, out ws) && !wsManager.GlobalWritingSystems.Contains(ws, wsComparer))
						{
							string langDefPath = Path.Combine(DirectoryFinder.GetFWDataSubDirectory("Languages"), Path.ChangeExtension(ws.IcuLocale, "xml"));
							if (File.Exists(langDefPath))
								FillWritingSystemFromLangDef(XElement.Load(langDefPath), ws);
							else
								FillWritingSystemFromFDO(domainObjectDtoRepository, wsInfo.Item3, ws);
						}
					}
					// this should also remove all LgCollations as well
					DataMigrationServices.RemoveIncludingOwnedObjects(domainObjectDtoRepository, wsInfo.Item2, false);
				}
				wsManager.Save();

				DataMigrationServices.IncrementVersionNumber(domainObjectDtoRepository);
			}
			finally
			{
				if (wsManager != null)
					wsManager.Dispose();
			}
		}

		private static void ConvertAllIcuLocalesToLangTags(IDomainObjectDTORepository domainObjectDtoRepository, string className,
			HashSet<string> referencedWsIds)
		{
			foreach (DomainObjectDTO dto in domainObjectDtoRepository.AllInstancesWithSubclasses(className))
			{
				XElement elem = XElement.Parse(dto.Xml);
				XElement icuLocaleElem = elem.Element("ICULocale");
				if (icuLocaleElem != null)
				{
					string wsId = LangTagUtils.ToLangTag((string) icuLocaleElem.Element("Uni"));
					icuLocaleElem.AddAfterSelf(new XElement("WritingSystem",
						new XElement("Uni", wsId)));
					icuLocaleElem.Remove();
					DataMigrationServices.UpdateDTO(domainObjectDtoRepository, dto, elem.ToString());
					referencedWsIds.Add(wsId);
				}
			}
		}

		private static void UpdateStringsAndProps(IDomainObjectDTORepository domainObjectDtoRepository, DomainObjectDTO dto,
			HashSet<string> referencedWsIds)
		{
			XElement objElem = XElement.Parse(dto.Xml);
			bool modified = false;
			foreach (XElement elem in objElem.Descendants())
			{
				switch (elem.Name.LocalName)
				{
					case "Run":
					case "AStr":
					case "AUni":
						if (UpdateWsAttribute(elem, referencedWsIds))
							modified = true;
						break;

					case "BulNumFontInfo":
					case "Prop":
					case "WsProp":
						if (UpdateWsAttribute(elem, referencedWsIds))
							modified = true;
						if (UpdateFontAttribute(elem))
							modified = true;
						break;
				}
			}
			if (modified)
				DataMigrationServices.UpdateDTO(domainObjectDtoRepository, dto, objElem.ToString());
		}

		private static bool UpdateWsAttribute(XElement elem, HashSet<string> referencedWsIds)
		{
			bool modified = false;
			XAttribute wsAttr = elem.Attribute("ws");
			if (wsAttr != null)
			{
				string wsVal = wsAttr.Value;
				if (String.IsNullOrEmpty(wsVal))
				{
					wsVal = "en";
					SIL.Utils.Logger.WriteEvent(String.Format(
						"Converting empty ws attribute to ws=\"en\" in DataMigration7000019: ParentNode={1}{0}",
						elem.Parent.ToString(), Environment.NewLine));
				}
				string wsId = LangTagUtils.ToLangTag(wsVal);
				wsAttr.Value = wsId;
				referencedWsIds.Add(wsId);
				modified = true;
			}

			XAttribute wsBaseAttr = elem.Attribute("wsBase");
			if (wsBaseAttr != null)
			{
				string wsVal = wsBaseAttr.Value;
				if (String.IsNullOrEmpty(wsVal))
				{
					wsVal = "en";
					SIL.Utils.Logger.WriteEvent(String.Format(
						"Converting empty wsBase attribute to ws=\"en\" in DataMigration7000019: ParentNode={1}{0}.",
						elem.Parent.ToString(), Environment.NewLine));
				}
				string wsId = LangTagUtils.ToLangTag(wsVal);
				wsBaseAttr.Value = wsId;
				referencedWsIds.Add(wsId);
				modified = true;
			}
			return modified;
		}

		private static bool UpdateFontAttribute(XElement elem)
		{
			XAttribute fontAttr = elem.Attribute("fontFamily");
			if (fontAttr != null)
			{
				switch ((string) fontAttr)
				{
					case "<default serif>":
					case "<default sans serif>":
					case "<default pub font>":
					case "<default heading font>":
					case "<default monospace>":
					case "<default fixed>":
						fontAttr.Value = "<default font>";
						return true;
				}
			}
			return false;
		}

		private static void ConvertAllRefsToStrings(IDomainObjectDTORepository domainObjectDtoRepository, string className,
			Dictionary<string, Tuple<string, DomainObjectDTO, XElement>> guidToWsInfo, HashSet<string> referencedWsIds)
		{
			foreach (DomainObjectDTO dto in domainObjectDtoRepository.AllInstancesWithSubclasses(className))
			{
				XElement elem = XElement.Parse(dto.Xml);
				if (ConvertRefToString(elem.Element("WritingSystem"), guidToWsInfo, referencedWsIds))
					DataMigrationServices.UpdateDTO(domainObjectDtoRepository, dto, elem.ToString());
			}
		}

		private static bool ConvertRefToString(XElement refElem, Dictionary<string, Tuple<string, DomainObjectDTO, XElement>> guidToWsInfo,
			HashSet<string> referencedWsIds)
		{
			if (refElem == null)
				return false;

			var sb = new StringBuilder();
			bool first = true;
			foreach (XElement surElem in refElem.Elements("objsur"))
			{
				var guid = (string)surElem.Attribute("guid");
				if (guid != null)
				{
					string wsId = guidToWsInfo[guid.ToLowerInvariant()].Item1;
					if (!first)
						sb.Append(" ");
					sb.Append(wsId);
					referencedWsIds.Add(wsId);
					first = false;
				}
			}
			refElem.RemoveAll();
			refElem.Add(new XElement("Uni", sb.ToString()));
			return true;
		}

		private static void FillWritingSystemFromLangDef(XElement langDefElem, IWritingSystem ws)
		{
			XElement wsElem = langDefElem.Element("LgWritingSystem");
			if (wsElem != null)
			{
				LanguageSubtag languageSubtag = ws.LanguageSubtag;
				string name = GetMultiUnicode(wsElem, "Name24");
				if (!string.IsNullOrEmpty(name))
				{
					int parenIndex = name.IndexOf('(');
					if (parenIndex != -1)
						name = name.Substring(0, parenIndex).Trim();
					ws.LanguageSubtag = new LanguageSubtag(languageSubtag, name);
				}

				string abbr = GetMultiUnicode(wsElem, "Abbr24");
				if (!string.IsNullOrEmpty(abbr))
					ws.Abbreviation = abbr;
				XElement collsElem = wsElem.Element("Collations24");
				if (collsElem != null)
				{
					XElement collElem = collsElem.Element("LgCollation");
					if (collElem != null)
					{
						string icuRules = GetUnicode(collElem, "ICURules30");
						if (!string.IsNullOrEmpty(icuRules))
						{
							ws.SortUsing = WritingSystemDefinition.SortRulesType.CustomICU;
							ws.SortRules = icuRules;
						}
					}
				}
				string defFont = GetUnicode(wsElem, "DefaultSerif24");
				if (!string.IsNullOrEmpty(defFont))
					ws.DefaultFontName = defFont;
				string defFontFeats = GetUnicode(wsElem, "FontVariation24");
				if (!string.IsNullOrEmpty(defFontFeats))
					ws.DefaultFontFeatures = defFontFeats;
				string keyboard = GetUnicode(wsElem, "KeymanKeyboard24");
				if (!string.IsNullOrEmpty(keyboard))
					ws.Keyboard = keyboard;
				string legacyMapping = GetUnicode(wsElem, "LegacyMapping24");
				if (!string.IsNullOrEmpty(legacyMapping))
					ws.LegacyMapping = legacyMapping;
				XElement localeElem = wsElem.Element("Locale24");
				if (localeElem != null)
				{
					XElement intElem = localeElem.Element("Integer");
					if (intElem != null)
						ws.LCID = (int) intElem.Attribute("val");
				}
				string matchedPairs = GetUnicode(wsElem, "MatchedPairs24");
				if (!string.IsNullOrEmpty(matchedPairs))
					ws.MatchedPairs = matchedPairs;
				string punctPatterns = GetUnicode(wsElem, "PunctuationPatterns24");
				if (!string.IsNullOrEmpty(punctPatterns))
					ws.PunctuationPatterns = punctPatterns;
				string quotMarks = GetUnicode(wsElem, "QuotationMarks24");
				if (!string.IsNullOrEmpty(quotMarks))
					ws.QuotationMarks = quotMarks;
				XElement rtolElem = wsElem.Element("RightToLeft24");
				if (rtolElem != null)
				{
					XElement boolElem = rtolElem.Element("Boolean");
					if (boolElem != null)
						ws.RightToLeftScript = (bool) boolElem.Attribute("val");
				}
				string spellCheck = GetUnicode(wsElem, "SpellCheckDictionary24");
				if (!string.IsNullOrEmpty(spellCheck))
					ws.SpellCheckingId = spellCheck;
				string validChars = GetUnicode(wsElem, "ValidChars24");
				if (!string.IsNullOrEmpty(validChars))
					ws.ValidChars = Icu.Normalize(validChars, Icu.UNormalizationMode.UNORM_NFD);
			}

			var localeName = (string) langDefElem.Element("LocaleName");
			if (!string.IsNullOrEmpty(localeName))
			{
				LanguageSubtag languageSubtag = ws.LanguageSubtag;
				ws.LanguageSubtag = new LanguageSubtag(languageSubtag, localeName);
			}
			var scriptName = (string)langDefElem.Element("LocaleScript");
			if (!string.IsNullOrEmpty(scriptName))
			{
				ScriptSubtag scriptSubtag = ws.ScriptSubtag;
				if (string.IsNullOrEmpty(scriptSubtag.Name))
					ws.ScriptSubtag = new ScriptSubtag(scriptSubtag, scriptName);
			}
			var regionName = (string)langDefElem.Element("LocaleCountry");
			if (!string.IsNullOrEmpty(regionName))
			{
				RegionSubtag regionSubtag = ws.RegionSubtag;
				if (string.IsNullOrEmpty(regionSubtag.Name))
					ws.RegionSubtag = new RegionSubtag(regionSubtag, regionName);
			}
			var variantName = (string) langDefElem.Element("LocaleVariant");
			if (!string.IsNullOrEmpty(variantName))
			{
				VariantSubtag variantSubtag = ws.VariantSubtag;
				if (string.IsNullOrEmpty(variantSubtag.Name))
					ws.VariantSubtag = new VariantSubtag(variantSubtag, variantName);
			}
		}

		private static void FillWritingSystemFromFDO(IDomainObjectDTORepository domainObjectDtoRepository,
			XElement wsElem, IWritingSystem ws)
		{
			LanguageSubtag languageSubtag = ws.LanguageSubtag;
			string name = GetMultiUnicode(wsElem, "Name");
			if (!string.IsNullOrEmpty(name))
			{
				int parenIndex = name.IndexOf('(');
				if (parenIndex != -1)
					name = name.Substring(0, parenIndex).Trim();
				ws.LanguageSubtag = new LanguageSubtag(languageSubtag, name);
			}
			string abbr = GetMultiUnicode(wsElem, "Abbr");
			if (!string.IsNullOrEmpty(abbr))
				ws.Abbreviation = abbr;
			string defFont = GetUnicode(wsElem, "DefaultSerif");
			if (!string.IsNullOrEmpty(defFont))
				ws.DefaultFontName = defFont;
			string defFontFeats = GetUnicode(wsElem, "FontVariation");
			if (!string.IsNullOrEmpty(defFontFeats))
				ws.DefaultFontFeatures = defFontFeats;
			string keyboard = GetUnicode(wsElem, "KeymanKeyboard");
			if (!string.IsNullOrEmpty(keyboard))
				ws.Keyboard = keyboard;
			string legacyMapping = GetUnicode(wsElem, "LegacyMapping");
			if (!string.IsNullOrEmpty(legacyMapping))
				ws.LegacyMapping = legacyMapping;
			XElement localeElem = wsElem.Element("Locale");
			if (localeElem != null)
				ws.LCID = (int) localeElem.Attribute("val");
			string matchedPairs = GetUnicode(wsElem, "MatchedPairs");
			if (!string.IsNullOrEmpty(matchedPairs))
				ws.MatchedPairs = matchedPairs;
			string puncPatterns = GetUnicode(wsElem, "PunctuationPatterns");
			if (!string.IsNullOrEmpty(puncPatterns))
				ws.PunctuationPatterns = puncPatterns;
			string quotMarks = GetUnicode(wsElem, "QuotationMarks");
			if (!string.IsNullOrEmpty(quotMarks))
				ws.QuotationMarks = quotMarks;
			XElement rtolElem = wsElem.Element("RightToLeft");
			if (rtolElem != null)
				ws.RightToLeftScript = (bool) rtolElem.Attribute("val");
			string spellCheck = GetUnicode(wsElem, "SpellCheckDictionary");
			if (!string.IsNullOrEmpty(spellCheck))
				ws.SpellCheckingId = spellCheck;
			string validChars = GetUnicode(wsElem, "ValidChars");
			if (!string.IsNullOrEmpty(validChars))
				ws.ValidChars = Icu.Normalize(validChars, Icu.UNormalizationMode.UNORM_NFD);

			XElement collsElem = wsElem.Element("Collations");
			if (collsElem != null)
			{
				XElement surElem = collsElem.Element("objsur");
				if (surElem != null)
				{
					var guid = (string) surElem.Attribute("guid");
					DomainObjectDTO collDto = domainObjectDtoRepository.GetDTO(guid);
					XElement collElem = XElement.Parse(collDto.Xml);
					string sortRules = GetUnicode(collElem, "ICURules");
					if (!string.IsNullOrEmpty(sortRules))
					{
						ws.SortUsing = WritingSystemDefinition.SortRulesType.CustomICU;
						ws.SortRules = sortRules;
					}
				}
			}
		}

		private static string GetMultiUnicode(XElement elem, string propName)
		{
			XElement propElem = elem.Element(propName);
			if (propElem != null)
				return (string)propElem.Element("AUni");
			return null;
		}

		private static string GetUnicode(XElement elem, string propName)
		{
			XElement propElem = elem.Element(propName);
			if (propElem != null)
				return (string) propElem.Element("Uni");
			return null;
		}

		#endregion
	}
}
