using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.WritingSystems;
using SIL.WritingSystems.Migration;
using SIL.Xml;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// <summary>
	/// Migrates from 7000018 to 7000019.
	/// JohnT: In this migration, at least, the model Writing System objects go away.
	/// Therefore it is important to preserve as much information as possible from them in LDML files which replace them.
	/// If there is already a relevant LDML in the folder we leave it alone.
	/// Otherwise if there is an appropriate one in the set we shipped with FieldWorks, we copy the relevant information from that.
	/// Otherwise we make one the best we can from the old writing system FDO object.
	///
	/// This all gets more difficult as FieldWorks moves further away from version 7000019. The first version of this
	/// migration made the new LDML by creating an actual PalasoWritingSystem object, initializing it with the relevant
	/// data, and letting it save itself. This stopped working in July 2011, when WritingSystemDefn got more picky about what IDs
	/// it would accept. We could no longer use it for a data migration which does not include fixing the IDs.
	///
	/// So far, we can still extract the information we want from the current LDML files in the release. This too may change.
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
				string langTag = Version19LangTagUtils.ToLangTag(icuLocale);
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

			var localStoreFolder = Path.Combine(domainObjectDtoRepository.ProjectFolder, FdoFileHelper.ksWritingSystemsDir);

			// If any writing systems that project needs don't already exist as LDML files,
			// create them, either by copying relevant data from a shipping LDML file, or by
			// extracting data from the obsolete writing system object's XML.
			if (!string.IsNullOrEmpty(domainObjectDtoRepository.ProjectFolder))
			{
				foreach (Tuple<string, DomainObjectDTO, XElement> wsInfo in guidToWsInfo.Values)
				{
					if (referencedWsIds.Contains(wsInfo.Item1))
					{
						var ws = new Version19WritingSystemDefn();
						var langTag = wsInfo.Item1;
						ws.LangTag = langTag;
						var ldmlFileName = Path.ChangeExtension(langTag, "ldml");
						string localPath = Path.Combine(localStoreFolder, ldmlFileName);
						if (File.Exists(localPath))
							continue; // already have one.
						string globalPath = Path.Combine(DirectoryFinder.OldGlobalWritingSystemStoreDirectory, ldmlFileName);
						if (File.Exists(globalPath))
							continue; // already have one.
						// Need to make one.

						// Code similar to this was in the old migrator (prior to 7000043). It does not work
						// because the XML files it is looking for are in the Languages subdirectory of the
						// FieldWorks 6 data directory, and this is looking in the FW 7 one. No one has complained
						// so we decided not to try to fix it for the new implementation of the migration.

						//string langDefPath = Path.Combine(FwDirectoryFinder.GetDataSubDirectory("Languages"),
						//    Path.ChangeExtension(langTag, "xml"));
						//if (File.Exists(langDefPath))
						//    FillWritingSystemFromLangDef(XElement.Load(langDefPath), ws);
						//else

						FillWritingSystemFromFDO(domainObjectDtoRepository, wsInfo.Item3, ws);
						ws.Save(localPath);
					}
				}
			}
			foreach (Tuple<string, DomainObjectDTO, XElement> wsInfo in guidToWsInfo.Values)
			{
				// this should also remove all LgCollations as well
				DataMigrationServices.RemoveIncludingOwnedObjects(domainObjectDtoRepository, wsInfo.Item2, false);
			}

			DataMigrationServices.IncrementVersionNumber(domainObjectDtoRepository);
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
					string wsId = Version19LangTagUtils.ToLangTag((string) icuLocaleElem.Element("Uni"));
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
				string wsId = Version19LangTagUtils.ToLangTag(wsVal);
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
				string wsId = Version19LangTagUtils.ToLangTag(wsVal);
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

		// I think this is roughly what we need to reinstate if we reinstate the call to it. See comments there.
		//private static void FillWritingSystemFromLangDef(XElement langDefElem, Version19WritingSystemDefn ws)
		//{
		//    XElement wsElem = langDefElem.Element("LgWritingSystem");
		//    if (wsElem != null)
		//    {
		//        string name = GetMultiUnicode(wsElem, "Name24");
		//        if (!string.IsNullOrEmpty(name))
		//        {
		//            int parenIndex = name.IndexOf('(');
		//            if (parenIndex != -1)
		//                name = name.Substring(0, parenIndex).Trim();
		//            ws.LanguageName = name;
		//        }

		//        string abbr = GetMultiUnicode(wsElem, "Abbr24");
		//        if (!string.IsNullOrEmpty(abbr))
		//            ws.Abbreviation = abbr;
		//        XElement collsElem = wsElem.Element("Collations24");
		//        if (collsElem != null)
		//        {
		//            XElement collElem = collsElem.Element("LgCollation");
		//            if (collElem != null)
		//            {
		//                string icuRules = GetUnicode(collElem, "ICURules30");
		//                if (!string.IsNullOrEmpty(icuRules))
		//                {
		//                    ws.SortUsing = WritingSystemDefinition.SortRulesType.CustomICU;
		//                    ws.SortRules = icuRules;
		//                }
		//            }
		//        }
		//        string defFont = GetUnicode(wsElem, "DefaultSerif24");
		//        if (!string.IsNullOrEmpty(defFont))
		//            ws.DefaultFontName = defFont;
		//        string defFontFeats = GetUnicode(wsElem, "FontVariation24");
		//        if (!string.IsNullOrEmpty(defFontFeats))
		//            ws.DefaultFontFeatures = defFontFeats;
		//        string keyboard = GetUnicode(wsElem, "KeymanKeyboard24");
		//        if (!string.IsNullOrEmpty(keyboard))
		//            ws.Keyboard = keyboard;
		//        string legacyMapping = GetUnicode(wsElem, "LegacyMapping24");
		//        if (!string.IsNullOrEmpty(legacyMapping))
		//            ws.LegacyMapping = legacyMapping;
		//        XElement localeElem = wsElem.Element("Locale24");
		//        if (localeElem != null)
		//        {
		//            XElement intElem = localeElem.Element("Integer");
		//            if (intElem != null)
		//                ws.LCID = (int) intElem.Attribute("val");
		//        }
		//        string matchedPairs = GetUnicode(wsElem, "MatchedPairs24");
		//        if (!string.IsNullOrEmpty(matchedPairs))
		//            ws.MatchedPairs = matchedPairs;
		//        string punctPatterns = GetUnicode(wsElem, "PunctuationPatterns24");
		//        if (!string.IsNullOrEmpty(punctPatterns))
		//            ws.PunctuationPatterns = punctPatterns;
		//        string quotMarks = GetUnicode(wsElem, "QuotationMarks24");
		//        if (!string.IsNullOrEmpty(quotMarks))
		//            ws.QuotationMarks = quotMarks;
		//        XElement rtolElem = wsElem.Element("RightToLeft24");
		//        if (rtolElem != null)
		//        {
		//            XElement boolElem = rtolElem.Element("Boolean");
		//            if (boolElem != null)
		//                ws.RightToLeftScript = (bool) boolElem.Attribute("val");
		//        }
		//        string spellCheck = GetUnicode(wsElem, "SpellCheckDictionary24");
		//        if (!string.IsNullOrEmpty(spellCheck))
		//            ws.SpellCheckingId = spellCheck;
		//        string validChars = GetUnicode(wsElem, "ValidChars24");
		//        if (!string.IsNullOrEmpty(validChars))
		//            ws.ValidChars = Icu.Normalize(validChars, Icu.UNormalizationMode.UNORM_NFD);
		//    }

		//    var localeName = (string) langDefElem.Element("LocaleName");
		//    if (!string.IsNullOrEmpty(localeName))
		//    {
		//        ws.LanguageName = localeName;
		//    }
		//    var scriptName = (string)langDefElem.Element("LocaleScript");
		//    if (!string.IsNullOrEmpty(scriptName))
		//    {
		//        ws.ScriptName = scriptName;
		//    }
		//    var regionName = (string)langDefElem.Element("LocaleCountry");
		//    if (!string.IsNullOrEmpty(regionName))
		//    {
		//        ws.RegionName = regionName;
		//    }
		//    var variantName = (string) langDefElem.Element("LocaleVariant");
		//    if (!string.IsNullOrEmpty(variantName))
		//    {
		//            ws.VariantName = variantName;
		//    }
		//}

		private static void FillWritingSystemFromFDO(IDomainObjectDTORepository domainObjectDtoRepository,
			XElement wsElem, Version19WritingSystemDefn ws)
		{
			string name = GetMultiUnicode(wsElem, "Name");
			if (!string.IsNullOrEmpty(name))
			{
				int parenIndex = name.IndexOf('(');
				if (parenIndex != -1)
					name = name.Substring(0, parenIndex).Trim();
				ws.LanguageName = name;
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
						ws.SortUsing = Version19WritingSystemDefn.SortRulesType.CustomICU;
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

	class Version19WritingSystemDefn
	{
		public enum SortRulesType
		{
			/// <summary>
			/// Default Unicode ordering rules (actually CustomICU without any rules)
			/// </summary>
			[Description("Default Ordering")]
			DefaultOrdering,
			/// <summary>
			/// Custom Simple (Shoebox/Toolbox) style rules
			/// </summary>
			[Description("Custom Simple (Shoebox style) rules")]
			CustomSimple,
			/// <summary>
			/// Custom ICU rules
			/// </summary>
			[Description("Custom ICU rules")]
			CustomICU,
			/// <summary>
			/// Use the sort rules from another language. When this is set, the SortRules are interpreted as a cultureId for the language to sort like.
			/// </summary>
			[Description("Same as another language")]
			OtherLanguage
		}

		private XmlNamespaceManager _nameSpaceManager;

		public Version19WritingSystemDefn()
		{
			_nameSpaceManager = MakeNameSpaceManager();
		}

		private XmlNamespaceManager MakeNameSpaceManager()
		{
			XmlNamespaceManager m = new XmlNamespaceManager(new NameTable());
			m.AddNamespace("palaso", "urn://palaso.org/ldmlExtensions/v1");
			return m;
		}

		private string m_langTag;
		public string LangTag
		{
			get { return m_langTag; }
			set
			{
				m_langTag = value;
				Version19LanguageSubtag languageSubtag;
				Version19ScriptSubtag scriptSubtag;
				Version19RegionSubtag regionSubtag;
				Version19VariantSubtag variantSubtag;
				Version19LangTagUtils.GetSubtags(m_langTag, out languageSubtag, out scriptSubtag, out regionSubtag,
					out variantSubtag);
				ISO = (languageSubtag.IsPrivateUse? "x-" : "") + languageSubtag.Code;
				if (scriptSubtag != null)
					Script = (scriptSubtag.IsPrivateUse ? "x-" : "") + scriptSubtag.Code;
				if (regionSubtag != null)
					Region = (regionSubtag.IsPrivateUse ? "x-" : "") + regionSubtag.Code;
				if (variantSubtag != null)
					Variant = (variantSubtag.IsPrivateUse ? "x-" : "") + variantSubtag.Code;
			}
		}
		public string LanguageName;
		public string Abbreviation;
		public SortRulesType SortUsing;
		public string SortRules;
		public string DefaultFontName;
		public string ISO;
		public string Script;
		public string Region;
		public string Variant;

		public string DefaultFontFeatures;
		public string Keyboard;
		public string LegacyMapping;
		public int LCID;
		public string MatchedPairs;
		public string PunctuationPatterns;
		public string QuotationMarks;
		public bool RightToLeftScript;
		public string SpellCheckingId;
		public string ValidChars;
		public string ScriptName;
		public string RegionName;
		public string VariantName;


		internal void Save(string filePath)
		{
			using (var writer = XmlWriter.Create(filePath, CanonicalXmlSettings.CreateXmlWriterSettings()))
			{
				writer.WriteStartDocument();
				WriteLdml(writer);
				writer.Close();
			}
		}

		private void WriteLdml(XmlWriter writer)
		{
			Debug.Assert(writer != null);
			writer.WriteStartElement("ldml");
			WriteIdentityElement(writer);
			WriteLayoutElement(writer);
			WriteCollationsElement(writer);
			WriteTopLevelSpecialElement(writer);
			writer.WriteEndElement();
		}

		private void WriteTopLevelSpecialElement(XmlWriter writer)
		{
			WriteBeginSpecialElement(writer);
			WriteSpecialValue(writer, "abbreviation", Abbreviation);
			WriteSpecialValue(writer, "defaultFontFamily", DefaultFontName);
			// original wrote DefaultFontSize if != 0 but we don't get that from anywhere.
			WriteSpecialValue(writer, "defaultKeyboard", Keyboard);
			// original wrote IsLegacyEncoded if true but we don't get that from anywhere.
			WriteSpecialValue(writer, "languageName", LanguageName);
			if (SpellCheckingId != ISO)
			{
				WriteSpecialValue(writer, "spellCheckingId", SpellCheckingId);
			}
			writer.WriteEndElement();
		}

		private void WriteCollationsElement(XmlWriter writer)
		{
			if (SortUsing == SortRulesType.DefaultOrdering)
			{
				return;
			}
			writer.WriteStartElement("collations");
			writer.WriteStartElement("collation");
			switch (SortUsing)
			{
				case SortRulesType.OtherLanguage:
					WriteCollationRulesFromOtherLanguage(writer);
					break;
				case SortRulesType.CustomSimple:
					WriteCollationRulesFromCustomSimple(writer);
					break;
				case SortRulesType.CustomICU:
					WriteCollationRulesFromCustomICU(writer);
					break;
				default:
					string message = string.Format("Unhandled SortRulesType '{0}' while writing LDML definition file.", SortUsing);
					throw new ApplicationException(message);
			}
			WriteBeginSpecialElement(writer);
			WriteSpecialValue(writer, "sortRulesType", SortUsing.ToString());
			writer.WriteEndElement();

			writer.WriteEndElement();
			writer.WriteEndElement();
		}

		private void WriteCollationRulesFromOtherLanguage(XmlWriter writer)
		{

			writer.WriteStartElement("base");
			WriteElementWithAttribute(writer, "alias", "source", SortRules);
			writer.WriteEndElement();
		}

		private void WriteCollationRulesFromCustomSimple(XmlWriter writer)
		{
			var parser = new SimpleRulesParser();
			string icu = parser.ConvertToIcuRules(SortRules ?? string.Empty);
			WriteCollationRulesFromICUString(writer, icu);
		}

		private void WriteCollationRulesFromCustomICU(XmlWriter writer)
		{
			WriteCollationRulesFromICUString(writer, SortRules);
		}

		private void WriteCollationRulesFromICUString(XmlWriter writer, string icu)
		{
			icu = icu ?? string.Empty;

			var parser = new IcuRulesParser(false);
			string message;
			// avoid throwing exception, just don't save invalid data
			if (!parser.ValidateIcuRules(icu, out message))
			{
				return;
			}
			parser.WriteIcuRules(writer, icu);
		}

		private void WriteBeginSpecialElement(XmlWriter writer)
		{
			writer.WriteStartElement("special");
			writer.WriteAttributeString("xmlns", "palaso", null, _nameSpaceManager.LookupNamespace("palaso"));
		}

		private void WriteSpecialValue(XmlWriter writer, string field, string value)
		{
			if (String.IsNullOrEmpty(value))
			{
				return;
			}
			writer.WriteStartElement(field, _nameSpaceManager.LookupNamespace("palaso"));
			writer.WriteAttributeString("value", value);
			writer.WriteEndElement();
		}

		private void WriteLayoutElement(XmlWriter writer)
		{
			// if we're left-to-right, we don't need to write out default values
			if (RightToLeftScript)
			{
				writer.WriteStartElement("layout");
				writer.WriteStartElement("orientation");
				// omit default value for "lines" attribute
				writer.WriteAttributeString("characters", "right-to-left");
				writer.WriteEndElement();
				writer.WriteEndElement();
			}
		}

		private void WriteElementWithAttribute(XmlWriter writer, string elementName, string attributeName, string value)
		{
			writer.WriteStartElement(elementName);
			writer.WriteAttributeString(attributeName, value);
			writer.WriteEndElement();
		}

		private void WriteIdentityElement(XmlWriter writer)
		{
			writer.WriteStartElement("identity");
			writer.WriteStartElement("version");
			// Original writes the version number and description, but nothing was trying to load these.
			writer.WriteAttributeString("number", "");
			writer.WriteEndElement();
			WriteElementWithAttribute(writer, "generation", "date", String.Format("{0:s}", DateTime.Now));

			WriteElementWithAttribute(writer, "language", "type", ISO);
			if (!String.IsNullOrEmpty(Script))
			{
				WriteElementWithAttribute(writer, "script", "type", Script);
			}
			if (!String.IsNullOrEmpty(Region))
			{
				WriteElementWithAttribute(writer, "territory", "type", Region);
			}
			if (!String.IsNullOrEmpty(Variant))
			{
				WriteElementWithAttribute(writer, "variant", "type", Variant);
			}
			writer.WriteEndElement();
		}
	}

	#region Subtag base class
	/// <summary>
	/// This class represents a subtag from the IANA language subtag registry.
	/// </summary>
	abstract class Version19Subtag
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:Subtag"/> class.
		/// </summary>
		/// <param name="code">The code.</param>
		/// <param name="name">The name.</param>
		/// <param name="isPrivateUse">if set to <c>true</c> this is a private use subtag.</param>
		protected Version19Subtag(string code, string name, bool isPrivateUse)
		{
			Code = code;
			Name = name;
			IsPrivateUse = isPrivateUse;
		}

		/// <summary>
		/// Gets the code.
		/// </summary>
		/// <value>The code.</value>
		public string Code
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the name.
		/// </summary>
		/// <value>The name.</value>
		public string Name
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets or sets a value indicating whether this instance is private use.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is private use; otherwise, <c>false</c>.
		/// </value>
		public bool IsPrivateUse
		{
			get;
			private set;
		}

		/// <summary>
		/// Determines whether the specified <see cref="T:System.Object"/> is equal to this instance.
		/// </summary>
		/// <param name="obj">The <see cref="T:System.Object"/> to compare with this instance.</param>
		/// <returns>
		/// 	<c>true</c> if the specified <see cref="T:System.Object"/> is equal to this instance; otherwise, <c>false</c>.
		/// </returns>
		/// <exception cref="T:System.NullReferenceException">
		/// The <paramref name="obj"/> parameter is null.
		/// </exception>
		public override bool Equals(object obj)
		{
			return Equals(obj as Version19Subtag);
		}

		/// <summary>
		/// Determines whether the specified <see cref="T:Subtag"/> is equal to this instance.
		/// </summary>
		/// <param name="other">The other.</param>
		/// <returns></returns>
		public bool Equals(Version19Subtag other)
		{
			if (other == null)
				throw new NullReferenceException();

			return other.Code == Code;
		}

		/// <summary>
		/// Returns a hash code for this instance.
		/// </summary>
		/// <returns>
		/// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
		/// </returns>
		public override int GetHashCode()
		{
			return Code.GetHashCode();
		}

		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"/> that represents this instance.
		/// </returns>
		public override string ToString()
		{
			if (!string.IsNullOrEmpty(Name))
				return Name;
			return Code;
		}

		/// <summary>
		/// Compares the language subtags by name.
		/// </summary>
		/// <param name="x">The x.</param>
		/// <param name="y">The y.</param>
		/// <returns></returns>
		public static int CompareByName(Version19Subtag x, Version19Subtag y)
		{
			if (x == null)
			{
				if (y == null)
				{
					return 0;
				}
				else
				{
					return -1;
				}
			}
			else
			{
				if (y == null)
				{
					return 1;
				}
				else
				{
					return x.Name.CompareTo(y.Name);
				}
			}
		}

		/// <summary>
		/// Implements the operator ==.
		/// </summary>
		/// <param name="x">The x.</param>
		/// <param name="y">The y.</param>
		/// <returns>The result of the operator.</returns>
		public static bool operator ==(Version19Subtag x, Version19Subtag y)
		{
			if (ReferenceEquals(x, y))
				return true;
			if ((object)x == null || (object)y == null)
				return false;
			return x.Equals(y);
		}

		/// <summary>
		/// Implements the operator !=.
		/// </summary>
		/// <param name="x">The x.</param>
		/// <param name="y">The y.</param>
		/// <returns>The result of the operator.</returns>
		public static bool operator !=(Version19Subtag x, Version19Subtag y)
		{
			return !(x == y);
		}
	}
	#endregion

	#region LanguageSubtag class
	/// <summary>
	/// This class represents a language from the IANA language subtag registry.
	/// </summary>
	class Version19LanguageSubtag : Version19Subtag
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:LanguageSubtag"/> class.
		/// </summary>
		/// <param name="code">The code.</param>
		/// <param name="name">The name.</param>
		/// <param name="isPrivateUse">if set to <c>true</c> this is a private use subtag.</param>
		/// <param name="iso3Code">The ISO 639-3 language code.</param>
		public Version19LanguageSubtag(string code, string name, bool isPrivateUse, string iso3Code)
			: base(code, name, isPrivateUse)
		{
			ISO3Code = iso3Code;
		}

		/// <summary>
		/// Gets the ISO 639-3 language code.
		/// </summary>
		/// <value>The ISO 639-3 language code.</value>
		public string ISO3Code
		{
			get;
			private set;
		}
	}
	#endregion

	#region ScriptSubtag class
	/// <summary>
	/// This class represents a script from the IANA language subtag registry.
	/// </summary>
	class Version19ScriptSubtag : Version19Subtag
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:ScriptSubtag"/> class.
		/// </summary>
		/// <param name="code">The code.</param>
		/// <param name="name">The name.</param>
		/// <param name="isPrivateUse">if set to <c>true</c> this is a private use subtag.</param>
		public Version19ScriptSubtag(string code, string name, bool isPrivateUse)
			: base(code, name, isPrivateUse)
		{
		}
	}
	#endregion

	#region RegionSubtag class
	/// <summary>
	/// This class represents a region from the IANA language subtag registry.
	/// </summary>
	class Version19RegionSubtag : Version19Subtag
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:RegionSubtag"/> class.
		/// </summary>
		/// <param name="code">The code.</param>
		/// <param name="name">The name.</param>
		/// <param name="isPrivateUse">if set to <c>true</c> this is a private use subtag.</param>
		public Version19RegionSubtag(string code, string name, bool isPrivateUse)
			: base(code, name, isPrivateUse)
		{
		}
	}
	#endregion

	#region VariantSubtag class
	/// <summary>
	/// This class represents a variant from the IANA language subtag registry.
	/// </summary>
	class Version19VariantSubtag : Version19Subtag
	{
		private readonly HashSet<string> m_prefixes = new HashSet<string>();

		/// <summary>
		/// Initializes a new instance of the <see cref="T:VariantSubtag"/> class.
		/// </summary>
		/// <param name="code">The code.</param>
		/// <param name="name">The name.</param>
		/// <param name="isPrivateUse">if set to <c>true</c> this is a private use subtag.</param>
		/// <param name="prefixes">The prefixes.</param>
		public Version19VariantSubtag(string code, string name, bool isPrivateUse, IEnumerable<string> prefixes)
			: base(code, name, isPrivateUse)
		{
			if (prefixes != null)
				m_prefixes.UnionWith(prefixes);
		}

		/// <summary>
		/// Gets the prefixes.
		/// </summary>
		/// <value>The prefixes.</value>
		public IEnumerable<string> Prefixes
		{
			get
			{
				return m_prefixes;
			}
		}
	}
	#endregion

	/// <summary>
	/// Fragments we need of LangTagUtils, compatible with expectations for version 19.
	/// </summary>
	class Version19LangTagUtils
	{
		private const string PrivateUseExpr = "[xX](-[a-zA-Z0-9]{1,40})+";
		private const string LanguageExpr = "[a-zA-Z]{2,3}(-[a-zA-Z]{3}){0,3}";
		private const string ScriptExpr = "[a-zA-Z]{4}";
		private const string RegionExpr = "[a-zA-Z]{2}|[0-9]{3}";
		private const string VariantSubExpr = "[0-9][a-zA-Z0-9]{3}|[a-zA-Z0-9]{5,8}";
		private const string VariantExpr = VariantSubExpr + "(-" + VariantSubExpr + ")*";
		private const string ExtensionExpr = "[a-wyzA-WYZ](-([a-zA-Z0-9]{2,8})+)+";
		private const string FuzzyVariantSubExpr = "[a-zA-Z0-9]{1,40}";
		private const string FuzzyVariantExpr = FuzzyVariantSubExpr + "(-" + FuzzyVariantSubExpr + ")*";
		private const string LangTagExpr = "(\\A(?'privateuse'" + PrivateUseExpr + ")\\z)"
			+ "|(\\A(?'language'" + LanguageExpr + ")"
			+ "(-(?'script'" + ScriptExpr + "))?"
			+ "(-(?'region'" + RegionExpr + "))?"
			+ "(-(?'variant'" + VariantExpr + "))?"
			+ "(-(?'extension'" + ExtensionExpr + "))?"
			+ "(-(?'privateuse'" + PrivateUseExpr + "))?\\z)";
		private static readonly Regex s_langTagPattern;
		private static readonly Regex s_langPattern;
		private static readonly Regex s_scriptPattern;
		private static readonly Regex s_regionPattern;
		private static readonly Regex s_variantPattern;
		static Version19LangTagUtils()
		{
			s_langTagPattern = new Regex(LangTagExpr, RegexOptions.ExplicitCapture);
			s_langPattern = new Regex("\\A(" + LanguageExpr + ")\\z", RegexOptions.ExplicitCapture);
			s_scriptPattern = new Regex("\\A(" + ScriptExpr + ")\\z", RegexOptions.ExplicitCapture);
			s_regionPattern = new Regex("\\A(" + RegionExpr + ")\\z", RegexOptions.ExplicitCapture);
			s_variantPattern = new Regex("\\A(" + FuzzyVariantExpr + ")\\z", RegexOptions.ExplicitCapture);
		}

		/// <summary>
		/// These special variants were recognized in version 19 as non-private-use.
		/// </summary>
		static HashSet<string> s_specialNonPrivateVariants = new HashSet<string>(new [] {"fonipa-x-etic", "fonipa-x-emic", "x-py", "x-pyn"});

		/// <summary>
		/// Converts the specified ICU locale to a language tag. If the ICU locale is already a valid
		/// language tag, it will return it.
		/// </summary>
		/// <param name="icuLocale">The ICU locale.</param>
		/// <returns></returns>
		public static string ToLangTag(string icuLocale)
		{
			if (string.IsNullOrEmpty(icuLocale))
				throw new ArgumentNullException("icuLocale");

			if (icuLocale.Contains("-"))
			{
				Match match = s_langTagPattern.Match(icuLocale);
				if (match.Success)
				{
					// We need to check for mixed case in the language code portion.  This has been
					// observed in user data, and causes crashes later on.  See LT-11288.
					var rgs = icuLocale.Split('-');
					if (rgs[0].ToLowerInvariant() == rgs[0])
						return icuLocale;
					var bldr = new StringBuilder();
					bldr.Append(rgs[0].ToLowerInvariant());
					for (var i = 1; i < rgs.Length; ++i)
					{
						bldr.Append("-");
						bldr.Append(rgs[i].ToLowerInvariant());
					}
					icuLocale = bldr.ToString();
				}
			}

			Icu.UErrorCode err;
			string icuLanguageCode;
			string languageCode;
			Icu.GetLanguageCode(icuLocale, out icuLanguageCode, out err);
			if (icuLanguageCode.Length == 4 && icuLanguageCode.StartsWith("x"))
				languageCode = icuLanguageCode.Substring(1);
			else
				languageCode = icuLanguageCode;
			// Some very old projects may have codes with over-long identifiers. In desperation we truncate these.
			// 4-letter codes starting with 'e' are a special case.
			if (languageCode.Length > 3 && !(languageCode.Length == 4 && languageCode.StartsWith("e")))
				languageCode = languageCode.Substring(0, 3);
			// The ICU locale strings in FW 6.0 allowed numbers in the language tag.  The
			// standard doesn't allow this. Map numbers to letters deterministically, even
			// though the resulting code may have no relation to reality.  (It may be a valid
			// ISO 639-3 language code that is assigned to a totally unrelated language.)
			if (languageCode.Contains('0'))
				languageCode = languageCode.Replace('0', 'a');
			if (languageCode.Contains('1'))
				languageCode = languageCode.Replace('1', 'b');
			if (languageCode.Contains('2'))
				languageCode = languageCode.Replace('2', 'c');
			if (languageCode.Contains('3'))
				languageCode = languageCode.Replace('3', 'd');
			if (languageCode.Contains('4'))
				languageCode = languageCode.Replace('4', 'e');
			if (languageCode.Contains('5'))
				languageCode = languageCode.Replace('5', 'f');
			if (languageCode.Contains('6'))
				languageCode = languageCode.Replace('6', 'g');
			if (languageCode.Contains('7'))
				languageCode = languageCode.Replace('7', 'h');
			if (languageCode.Contains('8'))
				languageCode = languageCode.Replace('8', 'i');
			if (languageCode.Contains('9'))
				languageCode = languageCode.Replace('9', 'j');
			Version19LanguageSubtag languageSubtag;
			if (languageCode == icuLanguageCode)
			{
				languageSubtag = GetLanguageSubtag(
					(languageCode.Length == 4 && languageCode.StartsWith("e")) ?
					languageCode.Substring(1) : languageCode);
			}
			else
			{
				languageSubtag = new Version19LanguageSubtag(languageCode, null, true, null);
			}
			if (icuLanguageCode == icuLocale)
				return ToLangTag(languageSubtag, null, null, null);

			string scriptCode;
			Icu.GetScriptCode(icuLocale, out scriptCode, out err);
			Version19ScriptSubtag scriptSubtag = null;
			if (!string.IsNullOrEmpty(scriptCode))
				scriptSubtag = GetScriptSubtag(scriptCode);

			string regionCode;
			Icu.GetCountryCode(icuLocale, out regionCode, out err);
			Version19RegionSubtag regionSubtag = null;
			if (!string.IsNullOrEmpty(regionCode))
				regionSubtag = GetRegionSubtag(regionCode);

			string variantCode;
			Icu.GetVariantCode(icuLocale, out variantCode, out err);
			Version19VariantSubtag variantSubtag = null;
			if (!string.IsNullOrEmpty(variantCode))
			{
				variantCode = TranslateVariantCode(variantCode, code =>
				{
					string[] pieces = variantCode.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
					return Utils.ListUtils.ToString(pieces, "-", item => TranslateVariantCode(item, subItem => subItem.ToLowerInvariant()));
				});
				variantSubtag = GetVariantSubtag(variantCode);
			}

			return ToLangTag(languageSubtag, scriptSubtag, regionSubtag, variantSubtag);
		}
		/// <summary>
		/// Generates a language tag from the specified subtags.
		/// </summary>
		/// <param name="languageSubtag">The language subtag.</param>
		/// <param name="scriptSubtag">The script subtag.</param>
		/// <param name="regionSubtag">The region subtag.</param>
		/// <param name="variantSubtag">The variant subtag.</param>
		/// <returns></returns>
		public static string ToLangTag(Version19LanguageSubtag languageSubtag, Version19ScriptSubtag scriptSubtag, Version19RegionSubtag regionSubtag, Version19VariantSubtag variantSubtag)
		{
			if (languageSubtag == null)
				throw new ArgumentNullException("languageSubtag");

			bool inPrivateUse = false;
			var sb = new StringBuilder();
			if (languageSubtag.IsPrivateUse)
			{
				sb.Append("x-");
				inPrivateUse = true;
			}
			sb.Append(languageSubtag.Code);

			if (scriptSubtag != null)
			{
				sb.Append("-");
				if (!inPrivateUse && scriptSubtag.IsPrivateUse)
				{
					sb.Append("x-");
					inPrivateUse = true;
				}
				sb.Append(scriptSubtag.Code);
			}

			if (regionSubtag != null)
			{
				sb.Append("-");
				if (!inPrivateUse && regionSubtag.IsPrivateUse && !IsPrivateUseRegionCode(regionSubtag.Code))
				{
					sb.Append("x-");
					inPrivateUse = true;
				}
				sb.Append(regionSubtag.Code);
			}
			else if (languageSubtag.Code == "zh" && languageSubtag.ISO3Code == "cmn")
			{
				sb.Append("-CN");
			}

			if (variantSubtag != null)
			{
				sb.Append("-");
				if (!inPrivateUse && variantSubtag.IsPrivateUse)
					sb.Append("x-");
				sb.Append(variantSubtag.Code);
			}

			return sb.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Translates standard variant codes to their expanded (semi-human-readable) format;
		/// all others are translated using the given function.
		/// </summary>
		/// <param name="variantCode">The variant code.</param>
		/// <param name="defaultFunc">The default translation function.</param>
		/// ------------------------------------------------------------------------------------
		private static string TranslateVariantCode(string variantCode, Func<string, string> defaultFunc)
		{
			switch (variantCode)
			{
				case "IPA": return "fonipa";
				case "X_ETIC": return "fonipa-x-etic";
				case "X_EMIC":
				case "EMC": return "fonipa-x-emic";
				case "X_PY":
				case "PY": return "pinyin";
				default: return defaultFunc(variantCode);
			}
		}

		public static bool IsPrivateUseRegionCode(string regionCode)
		{
			return regionCode == "AA" || regionCode == "ZZ"
				|| (regionCode.CompareTo("QM") >= 0 && regionCode.CompareTo("QZ") <= 0)
				|| (regionCode.CompareTo("XA") >= 0 && regionCode.CompareTo("XZ") <= 0);
		}

		/// <summary>
		/// Gets the language subtag with the specified code.
		/// </summary>
		/// <param name="code">The code.</param>
		/// <returns></returns>
		public static Version19LanguageSubtag GetLanguageSubtag(string code)
		{
			if (string.IsNullOrEmpty(code))
				throw new ArgumentNullException("code");

			return new Version19LanguageSubtag(code, null, !StandardSubtags.IsValidIso639LanguageCode(code), null);
		}

		/// <summary>
		/// Gets the script subtag with the specified code.
		/// </summary>
		/// <param name="code">The code.</param>
		/// <returns></returns>
		public static Version19ScriptSubtag GetScriptSubtag(string code)
		{
			if (string.IsNullOrEmpty(code))
				throw new ArgumentNullException("code");

			Version19ScriptSubtag subtag;
			return new Version19ScriptSubtag(code, null, !StandardSubtags.RegisteredScripts.Contains(code));
		}

		/// <summary>
		/// Gets the region subtag with the specified code.
		/// </summary>
		/// <param name="code">The code.</param>
		/// <returns></returns>
		public static Version19RegionSubtag GetRegionSubtag(string code)
		{
			if (string.IsNullOrEmpty(code))
				throw new ArgumentNullException("code");

			Version19RegionSubtag subtag;
			return new Version19RegionSubtag(code, null, !StandardSubtags.RegisteredRegions.Contains(code));
		}

		/// <summary>
		/// Gets the variant subtag with the specified code.
		/// </summary>
		/// <param name="code">The code.</param>
		/// <returns></returns>
		public static Version19VariantSubtag GetVariantSubtag(string code)
		{
			if (string.IsNullOrEmpty(code))
				throw new ArgumentNullException("code");

			Version19VariantSubtag subtag;
			return new Version19VariantSubtag(code, null,
				!StandardSubtags.IsValidRegisteredVariantCode(code) && !s_specialNonPrivateVariants.Contains(code), null);
		}

		/// <summary>
		/// Gets the subtags of the specified language tag.
		/// </summary>
		/// <param name="langTag">The language tag.</param>
		/// <param name="languageSubtag">The language subtag.</param>
		/// <param name="scriptSubtag">The script subtag.</param>
		/// <param name="regionSubtag">The region subtag.</param>
		/// <param name="variantSubtag">The variant subtag.</param>
		/// <returns></returns>
		public static bool GetSubtags(string langTag, out Version19LanguageSubtag languageSubtag, out Version19ScriptSubtag scriptSubtag,
			out Version19RegionSubtag regionSubtag, out Version19VariantSubtag variantSubtag)
		{
			if (string.IsNullOrEmpty(langTag))
				throw new ArgumentNullException("langTag");

			languageSubtag = null;
			scriptSubtag = null;
			regionSubtag = null;
			variantSubtag = null;

			Match match = s_langTagPattern.Match(langTag);
			if (!match.Success)
				return false;
			var parts = new[] { "language", "script", "region", "variant" };
			Group privateUseGroup = match.Groups["privateuse"];
			string[] privateUseSubTags = null;
			int privateUseSubTagIndex = 0;
			bool privateUsePrefix = false;
			string privateUseSubTag = null;
			int part = -1;
			if (privateUseGroup.Success)
			{
				for (part = parts.Length - 1; part >= 0; part--)
				{
					Group group = match.Groups[parts[part]];
					if (group.Success && privateUseGroup.Index > group.Index)
						break;
				}
				part++;
				privateUseSubTags = privateUseGroup.Value.Split('-');
				privateUseSubTag = NextSubTag(privateUseSubTags, ref privateUseSubTagIndex, out privateUsePrefix);
			}
			string languageCode = match.Groups["language"].Value;
			if (!string.IsNullOrEmpty(languageCode))
			{
				languageSubtag = GetLanguageSubtag(languageCode);
			}
			else if (privateUseSubTag != null && part <= 0)
			{
				languageSubtag = new Version19LanguageSubtag(privateUseSubTag, null, true, null);
				privateUseSubTag = NextSubTag(privateUseSubTags, ref privateUseSubTagIndex, out privateUsePrefix);
			}

			string scriptCode = match.Groups["script"].Value;
			if (!string.IsNullOrEmpty(scriptCode))
			{
				scriptSubtag = GetScriptSubtag(scriptCode);
			}
			else if (privateUseSubTag != null && part <= 1 && s_scriptPattern.IsMatch(privateUseSubTag))
			{
				scriptSubtag = privateUsePrefix ? new Version19ScriptSubtag(privateUseSubTag, null, true) : GetScriptSubtag(privateUseSubTag);
				privateUseSubTag = NextSubTag(privateUseSubTags, ref privateUseSubTagIndex, out privateUsePrefix);
			}

			string regionCode = match.Groups["region"].Value;
			if (!string.IsNullOrEmpty(regionCode))
			{
				regionSubtag = GetRegionSubtag(regionCode);
			}
			else if (privateUseSubTag != null && part <= 2 && s_regionPattern.IsMatch(privateUseSubTag))
			{
				regionSubtag = GetRegionSubtag(privateUseSubTag);
				privateUseSubTag = NextSubTag(privateUseSubTags, ref privateUseSubTagIndex, out privateUsePrefix);
			}

			var variantSb = new StringBuilder();
			bool variantPrivateUsePrefix = false;
			string variantCode = match.Groups["variant"].Value;
			if (!string.IsNullOrEmpty(variantCode))
			{
				variantSb.Append(variantCode);
			}
			// We would like to also check this subtag against the variant pattern
			// to ensure that we have a legitimate variant code, but for loading legacy projects
			// with poorly-formed codes, we have to do something with the private use subtag,
			// so if it doesn't match any of the others we force it to be a variant even if
			// it is not properly formed.
			else if (privateUseSubTag != null && part <= 3)
			{
				variantSb.Append(privateUseSubTag);
				variantPrivateUsePrefix = privateUsePrefix;
				privateUseSubTag = NextSubTag(privateUseSubTags, ref privateUseSubTagIndex, out privateUsePrefix);
			}

			while (privateUseSubTag != null)
			{
				variantSb.Append("-");
				if (privateUsePrefix)
					variantSb.Append("x-");
				variantSb.Append(privateUseSubTag);
				privateUseSubTag = NextSubTag(privateUseSubTags, ref privateUseSubTagIndex, out privateUsePrefix);
			}

			variantCode = variantSb.ToString();
			if (!string.IsNullOrEmpty(variantCode))
			{
				variantSubtag = variantPrivateUsePrefix ? new Version19VariantSubtag(variantCode, null, true, null)
					: GetVariantSubtag(variantCode);
			}
			return true;
		}

		private static string NextSubTag(string[] subTags, ref int subTagIndex, out bool privateUsePrefix)
		{
			privateUsePrefix = false;
			if (subTagIndex < 0 || subTagIndex >= subTags.Length)
				return null;

			if (subTags[subTagIndex].ToLowerInvariant() == "x")
			{
				privateUsePrefix = true;
				subTagIndex++;
			}
			return subTags[subTagIndex++];
		}
	}
}
