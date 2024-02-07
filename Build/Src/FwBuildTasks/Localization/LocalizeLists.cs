// Copyright (c) 2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.Build.Utilities;

// ReSharper disable PossibleNullReferenceException - Wolf!

namespace SIL.FieldWorks.Build.Tasks.Localization
{
	/// <summary>
	/// This class has the two-fold goal of providing source translations for the FieldWorks Localizable lists
	/// in a format that can be used by Crowdin, and rebuilding a LocalizedLists file that can be shipped and
	/// and loaded by FieldWorks from the translated content.
	/// </summary>
	internal class LocalizeLists
	{
		private const int ExpectedListCount = 29;

		internal const string AcademicDomains = "AcademicDomains.xlf";
		internal const string MiscLists = "MiscLists.xlf";
		internal const string LexicalTypes = "LexicalTypes.xlf";
		internal const string SemanticDomains = "SemanticDomains.xlf";
		internal const string AnthropologyCategories = "AnthropologyCategories.xlf";

		/// <remarks>
		/// NB: for those of you wondering why these are in such a ridiculous order,
		/// this is the order in which FLEx exports the lists. Preserving this order
		/// allows diffing of exports with round-tripped files to ensure losslessness.
		/// </remarks>
		private static readonly List<ListInfo> ListToXliffMap = new List<ListInfo>
		{
			new ListInfo("LexDb", "DomainTypes", "CmPossibility", AcademicDomains),
			new ListInfo("LangProject", "AnthroList", "CmAnthroItem", AnthropologyCategories),
			new ListInfo("LexDb", "ComplexEntryTypes", "LexEntryType", LexicalTypes),
			new ListInfo("LangProject", "ConfidenceLevels", "CmPossibility", MiscLists),
			new ListInfo("LexDb", "DialectLabels", "CmPossibility", MiscLists),
			new ListInfo("LangProject", "Education", "CmPossibility", MiscLists),
			new ListInfo("LexDb", "ExtendedNoteTypes", "CmPossibility", MiscLists),
			new ListInfo("LangProject", "GenreList", "CmPossibility", MiscLists),
			new ListInfo("LexDb", "Languages", "CmPossibility", MiscLists),
			new ListInfo("LexDb", "References", "LexRefType", MiscLists),
			new ListInfo("LangProject", "Locations", "CmLocation", MiscLists),
			new ListInfo("LexDb", "MorphTypes", "MoMorphType", LexicalTypes),
			new ListInfo("RnResearchNbk", "RecTypes", "CmPossibility", MiscLists),
			new ListInfo("LangProject", "PartsOfSpeech", "PartOfSpeech", MiscLists),
			new ListInfo("LangProject", "People", "CmPerson", MiscLists),
			new ListInfo("LangProject", "Positions", "CmPossibility", MiscLists),
			new ListInfo("LexDb", "PublicationTypes", "CmPossibility", MiscLists),
			new ListInfo("LangProject", "Restrictions", "CmPossibility", MiscLists),
			new ListInfo("LangProject", "Roles", "CmPossibility", MiscLists),
			new ListInfo("LangProject", "SemanticDomainList", "CmSemanticDomain", SemanticDomains),
			new ListInfo("LexDb", "SenseTypes", "CmPossibility", LexicalTypes),
			new ListInfo("LangProject", "Status", "CmPossibility", MiscLists),
			new ListInfo("DsDiscourseData", "ChartMarkers", "CmPossibility", MiscLists),
			new ListInfo("DsDiscourseData", "ConstChartTempl", "CmPossibility", MiscLists),
			new ListInfo("LangProject", "TextMarkupTags", "CmPossibility", MiscLists),
			new ListInfo("LangProject", "TimeOfDay", "CmPossibility", MiscLists),
			new ListInfo("LangProject", "TranslationTags", "CmPossibility", MiscLists),
			new ListInfo("LexDb", "UsageTypes", "CmPossibility", MiscLists),
			new ListInfo("LexDb", "VariantEntryTypes", "LexEntryType", LexicalTypes)
		};

		private static readonly ConversionMap PossMap = new ConversionMap("Possibilities", "_Poss");
		private static readonly ConversionMap SubPosMap = new ConversionMap("SubPossibilities", "_SubPos");
		private static readonly ConversionMap NameMap = new ConversionMap("Name", "_Name");
		private static readonly ConversionMap AbbrMap = new ConversionMap("Abbreviation", "_Abbr");
		private static readonly ConversionMap RevNameMap = new ConversionMap("ReverseName", "_RevName");
		private static readonly ConversionMap RevAbbrMap = new ConversionMap("ReverseAbbr", "_RevAbbr"); // for LexEntryType
		private static readonly ConversionMap RevAbbrevMap = new ConversionMap("ReverseAbbreviation", "_RevAbbrev"); // for LexRefType
		private static readonly ConversionMap GlsAppendMap = new ConversionMap("GlossAppend", "_GlsApp");
		private static readonly ConversionMap DescMap = new ConversionMap("Description", "_Desc");
		private static readonly ConversionMap QuestionsMap = new ConversionMap("Questions", "_Qs");
		private static readonly ConversionMap QuestionMap = new ConversionMap("Question", "_Q");
		private static readonly ConversionMap ExWordsMap = new ConversionMap("ExampleWords", "_EW");
		private static readonly ConversionMap ExSentencesMap = new ConversionMap("ExampleSentences", "_ES");

		private static readonly AttConversionMap GuidMap = new AttConversionMap("guid", "guid");

		private static readonly SubTypeMap SubtypesMap = new SubTypeMap("LexEntryType", "LexEntryInflType");

		#region Xml LocalizedLists To Xliff
		/// <param name="sourceFile">path to the XML file containing lists to localize</param>
		/// <param name="localizationsRoot">path to save XLIFF files that are ready to upload to Crowdin</param>
		/// <param name="targetLang">If specified, any strings in this locale will be included as 'final' translations</param>
		/// <param name="listsToInclude">If specified, only these lists will be expected and used in the conversion</param>
		/// <param name="logger"/>
		public static void SplitSourceLists(string sourceFile, string localizationsRoot, string targetLang,
			List<string> listsToInclude = null, TaskLoggingHelper logger = null)
		{
			if (!File.Exists(sourceFile))
				throw new ArgumentException("The source file does not exist.",
					nameof(sourceFile));
			if (!Directory.Exists(localizationsRoot))
				throw new ArgumentException(
					"Destination directory for the localizations source does not exist.",
					nameof(localizationsRoot));
			using (var sourceXml = new XmlTextReader(sourceFile))
			{
				SplitLists(sourceXml, localizationsRoot, targetLang, listsToInclude, logger);
			}
		}

		internal static void SplitLists(XmlTextReader sourceFile, string localizationsRoot, string targetLang,
			List<string> listsToInclude = null, TaskLoggingHelper logger = null)
		{
			ValidateListsToIncludeArgument(listsToInclude);
			var sourceDoc = XDocument.Load(sourceFile);
			var listElements = sourceDoc.Root?.Elements("List").ToArray();
			if (listElements == null || !listElements.Any())
				throw new ArgumentException(
					"Source file is not in the expected format, no Lists found under the root element.");
			ValidateSourceDocWithListsToInclude(listElements, listsToInclude, targetLang, logger);
			listsToInclude = listsToInclude ?? ListToXliffMap.Select(info => info.XliffFile).ToList();

			// LexEntryInflTypes have a field GlossPrepend that exists, but the original designers didn't expect anyone to use it.
			// We don't ship anything of this type, but we do want to alert any future developers on the odd chance that we might.
			if (sourceDoc.XPathSelectElement("//GlossPrepend") != null)
				throw new NotSupportedException("GlossPrepend is not supported because we weren't shipping any at the time of writing.");

			var academicDomainsList = new XDocument(new XElement("Lists"));
			var miscLists = new XDocument(new XElement("Lists"));
			var lexicalTypesLists = new XDocument(new XElement("Lists"));
			var semanticDomainsList = new XDocument(new XElement("Lists"));
			var anthropologyCatList = new XDocument(new XElement("Lists"));

			foreach (var list in listElements)
			{
				var listInfo = FindListInfoForList(list);
				if (listInfo == null)
					throw new ArgumentException(
						"Unknown list encountered. Update ListToXliff map?");

				// only add the content for the lists that were requested
				if (!listsToInclude.Contains(listInfo.XliffFile))
					continue;

				switch (listInfo.XliffFile)
				{
					// ReSharper disable PossibleNullReferenceException -- Impossible because of code above
					case AcademicDomains:
					{
						academicDomainsList.Root.Add(new XElement(list));
						break;
					}
					case MiscLists:
					{
						miscLists.Root.Add(new XElement(list));
						break;
					}
					case LexicalTypes:
					{
						lexicalTypesLists.Root.Add(new XElement(list));
						break;
					}
					case SemanticDomains:
					{
						semanticDomainsList.Root.Add(new XElement(list));
						break;
					}
					case AnthropologyCategories:
					{
						anthropologyCatList.Root.Add(new XElement(list));
						break;
					}
					// ReSharper restore PossibleNullReferenceException
				}
			}

			if (listsToInclude.Contains(AcademicDomains))
			{
				var academicDomainsXliff = ConvertListToXliff(AcademicDomains, academicDomainsList, targetLang);
				academicDomainsXliff.Save(Path.Combine(localizationsRoot, AcademicDomains));
			}
			if (listsToInclude.Contains(MiscLists))
			{
				var miscListsXliff = ConvertListToXliff(MiscLists, miscLists, targetLang);
				miscListsXliff.Save(Path.Combine(localizationsRoot, MiscLists));
			}
			if (listsToInclude.Contains(LexicalTypes))
			{
				var lexicalTypesXliff = ConvertListToXliff(LexicalTypes, lexicalTypesLists, targetLang);
				lexicalTypesXliff.Save(Path.Combine(localizationsRoot, LexicalTypes));
			}
			if (listsToInclude.Contains(SemanticDomains))
			{
				var semanticDomainsXliff = ConvertListToXliff(SemanticDomains, semanticDomainsList, targetLang);
				semanticDomainsXliff.Save(Path.Combine(localizationsRoot, SemanticDomains));
			}
			if (listsToInclude.Contains(AnthropologyCategories))
			{
				var anthroCatXliff = ConvertListToXliff(AnthropologyCategories, anthropologyCatList, targetLang);
				anthroCatXliff.Save(Path.Combine(localizationsRoot, AnthropologyCategories));
			}
		}

		private static void ValidateSourceDocWithListsToInclude(XElement[] listElements, IEnumerable<string> listsToInclude, string targetLang, TaskLoggingHelper logger)
		{
			if (listsToInclude == null && listElements.Length != ExpectedListCount)
			{
				var msg = $"Source file has an unexpected list count. {listElements.Length} instead of {ExpectedListCount}";
				if (targetLang == null || logger == null)
					throw new ArgumentException(msg);
				logger.LogWarning($"{msg} for '{targetLang}'.");
			}
			else if(listsToInclude != null)
			{
				var missingLists = listsToInclude.ToList();
				foreach (var listElement in listElements)
				{
					var listInfo = FindListInfoForList(listElement);
					missingLists.Remove(listInfo?.XliffFile);
				}

				if (missingLists.Any())
				{
					throw new ArgumentException($"Source file does not have content for all lists to include. " +
												$"{string.Join(",", missingLists)} content was not found.");
				}
			}
		}

		private static void ValidateListsToIncludeArgument(IEnumerable<string> listsToInclude)
		{
			if (listsToInclude == null)
				return;
			foreach (var list in listsToInclude)
			{
				if (list != LocalizeLists.AcademicDomains && list != LocalizeLists.MiscLists &&
					list != LocalizeLists.SemanticDomains && list != LocalizeLists.LexicalTypes &&
					list != LocalizeLists.AnthropologyCategories)
					throw new ArgumentException(
						$"ListsToInclude is expecting one or more .xlf file names. e.g. {LocalizeLists.SemanticDomains}");
			}
		}

		private static ListInfo FindListInfoForList(XElement list)
		{
			return ListToXliffMap.Find(x => list.Attribute("owner")?.Value == x.Owner
				&& list.Attribute("field")?.Value == x.Field);
		}

		internal static XDocument ConvertListToXliff(string listFileName, XDocument listsDoc, string targetLang)
		{
			var xliffDoc = XDocument.Parse(string.Format(XliffUtils.BodyTemplate, listFileName));
			if (targetLang != null)
			{
				xliffDoc.XPathSelectElement("/xliff/file").SetAttributeValue("target-language", targetLang);
			}
			var bodyElem = xliffDoc.XPathSelectElement("/xliff/file/body", XliffUtils.NameSpaceManager);
			foreach (var list in listsDoc.XPathSelectElements("/Lists/List"))
			{
				var listId = GetListId(list);
				var group = XElement.Parse($"<group id='{listId}'/>");
				ConvertAUniToXliff(list, group, listId, NameMap, targetLang);
				ConvertAUniToXliff(list, group, listId, AbbrMap, targetLang);
				ConvertAStrToXliff(list, group, listId, DescMap, targetLang);
				ConvertPossibilities(list, group, GetPossTypeForList(list), listId, targetLang);
				bodyElem?.Add(group);
			}

			return xliffDoc;
		}

		private static string GetPossTypeForList(XElement list)
		{
			var listInfo = FindListInfoForList(list);
			if (listInfo != null)
			{
				return listInfo.Type;
			}
			throw new ArgumentException("Unknown list found");
		}

		private static void ConvertAUniToXliff(XElement list, XElement group, string baseId, ConversionMap item, string targetLang, ConversionMap? context = null)
		{
			var sourceElem = list.Element(item.ElementName);
			var source = sourceElem?.Elements("AUni").First().Value;
			if (string.IsNullOrWhiteSpace(source))
			{
				return;
			}
			var transUnit = XElement.Parse(string.Format(XliffUtils.TransUnitTemplate, baseId + item.IdSuffix, SecurityElement.Escape(source)));

			if (context != null)
			{
				var descElem = list.Element(((ConversionMap)context).ElementName);
				var description = AStrValue(descElem?.Element("AStr"));

				if (!string.IsNullOrWhiteSpace(description))
				{
					transUnit.Add(XElement.Parse(string.Format(XliffUtils.ContextTemplate, SecurityElement.Escape(description))));
				}
			}

			if (targetLang != null)
			{
				var target = sourceElem.XPathSelectElement($"AUni[@ws='{targetLang}']")?.Value;
				if (!string.IsNullOrWhiteSpace(target))
				{
					transUnit.Add(XElement.Parse(string.Format(XliffUtils.FinalTargetTemplate, SecurityElement.Escape(target))));
				}
			}
			group.Add(transUnit);
		}

		private static void ConvertPossibilities(XElement owner, XElement group, string possibilityType, string baseId, string targetLang)
		{
			var possibilitiesElement = owner.Element(PossMap.ElementName);
			if (possibilitiesElement == null) // No Possibilities found, no work to do
			{
				return;
			}

			var possibilitiesId = baseId + PossMap.IdSuffix;
			var possibilitiesGroup = XElement.Parse(string.Format(XliffUtils.EmptyGroup, possibilitiesId));
			AddPossibilitiesToGroup(possibilityType, possibilitiesElement, baseId, possibilitiesId, possibilitiesGroup, targetLang);
			group.Add(possibilitiesGroup);
		}

		/// <summary>
		/// These are only found in Semantic Domains, but it doesn't hurt to handle them generically
		/// </summary>
		private static void ConvertQuestions(XElement owner, XElement group, string baseId, string targetLang)
		{
			var questionsElement = owner.Element(QuestionsMap.ElementName);
			if (questionsElement == null) // No Possibilities found, no work to do
			{
				return;
			}

			var questionsId = baseId + QuestionsMap.IdSuffix;
			var questionsGroup = XElement.Parse(string.Format(XliffUtils.EmptyGroup, questionsId));
			AddQuestionsToGroup(questionsElement, questionsId, questionsGroup, targetLang);
			group.Add(questionsGroup);
		}

		private static void AddQuestionsToGroup(XElement questionsElement, string questionsId, XElement questionsGroup, string targetLang)
		{
			var questionElement = questionsElement.Elements("CmDomainQ");
			int questionIndex = 0;
			foreach (var question in questionElement)
			{
				var possId = questionsId + "_" + questionIndex;
				var questionGroup = XElement.Parse(string.Format(XliffUtils.EmptyGroup, possId));
				ConvertAUniToXliff(question, questionGroup, possId, QuestionMap, targetLang);
				ConvertAUniToXliff(question, questionGroup, possId, ExWordsMap, targetLang);
				ConvertAStrToXliff(question, questionGroup, possId, ExSentencesMap, targetLang);
				questionsGroup.Add(questionGroup);
				++questionIndex;
			}
		}

		private static void ConvertAStrToXliff(XElement owner, XElement ownerGroup, string possId, ConversionMap item, string targetLang)
		{
			var itemNode = owner.Element(item.ElementName);
			var source = AStrValue(itemNode?.Element("AStr"));
			if (string.IsNullOrWhiteSpace(source)) // nothing to translate
			{
				return;
			}
			var target = targetLang == null ? null : AStrValue(itemNode?.XPathSelectElement($"AStr[@ws='{targetLang}']"));

			// 2020.02: Yes, conflating all runs into a single trans-unit loses some information, but we have only one string in our lists
			// with multiple runs, and it is a royal pain to translate. In fact, no previous translators have bothered preserving run breaks.
			// The run is in a subgroup all by itself because we started by preserving runs, and we didn't feel like removing the extra group.
			var groupId = possId + item.IdSuffix;
			var groupNode = XElement.Parse(string.Format(XliffUtils.EmptyGroup, groupId));
			var transUnit = XElement.Parse(string.Format(XliffUtils.TransUnitTemplate, groupId + "_0", SecurityElement.Escape(source)));
				if (!string.IsNullOrWhiteSpace(target))
				{
					transUnit.Add(XElement.Parse(string.Format(XliffUtils.FinalTargetTemplate, SecurityElement.Escape(target))));
				}
			groupNode.Add(transUnit);
			ownerGroup.Add(groupNode);
		}

		private static string AStrValue(XElement aStrElem)
		{
			if (aStrElem == null)
			{
				return null;
			}
			var aStrBuilder = new StringBuilder();
			foreach (var run in aStrElem.Elements("Run"))
			{
				aStrBuilder.Append(run.Value);
			}
			return aStrBuilder.ToString();
		}

		private static void ConvertSubPossibilities(XElement owner, XElement group, string possibilityType,
			string baseId, string parentId, string targetLang)
		{
			var possibilitiesElement = owner.Element(SubPosMap.ElementName);
			if (possibilitiesElement == null) // No Possibilities found, no work to do
			{
				return;
			}

			var possibilitiesId = parentId + SubPosMap.IdSuffix;
			var possibilitiesGroup = XElement.Parse(string.Format(XliffUtils.EmptyGroup, possibilitiesId));
			AddPossibilitiesToGroup(possibilityType, possibilitiesElement, baseId, possibilitiesId, possibilitiesGroup, targetLang);
			group.Add(possibilitiesGroup);
		}

		private static void AddPossibilitiesToGroup(string possibilityType, XElement possibilitiesElement,
			string baseId, string groupId, XElement possibilitiesGroup, string targetLang)
		{
			var handleSubtypes = possibilityType == SubtypesMap.SuperType;
			var possibilityElements = handleSubtypes
				? possibilitiesElement.Elements().Where(e => e.Name == SubtypesMap.SuperType || e.Name == SubtypesMap.SubType)
				: possibilitiesElement.Elements(possibilityType);
			var possIndex = 0;
			foreach (var possibility in possibilityElements)
			{
				// If there is a GUID, use it as the key rather than using the index (LT-20251).
				// ENHANCE (Hasso) 2020.06: fall back to the English abbreviation before the index (need to sanitize Abbrevs)
				var guidAtt = possibility.Attribute(GuidMap.AttName);
				var possId = guidAtt == null ? $"{groupId}_{possIndex}" : $"{baseId}_{guidAtt.Value}";
				var possGroup = XElement.Parse(string.Format(XliffUtils.EmptyGroup, possId));
				if (handleSubtypes && possibility.Name != possibilityType)
				{
					var typeElem = XElement.Load(new XmlTextReader(
						$"<sil:type>{possibility.Name}</sil:type>",
						XmlNodeType.Element,
						new XmlParserContext(null, XliffUtils.NameSpaceManager, null, XmlSpace.None)));
					possGroup.Add(typeElem);

				}
				ConvertAttributeAsElement(GuidMap, possibility, possGroup);

				ConversionMap? context = null;
				if (possibilityType == "CmSemanticDomain")
				{
					context = DescMap;
				}

				ConvertAUniToXliff(possibility, possGroup, possId, NameMap, targetLang, context);
				ConvertAUniToXliff(possibility, possGroup, possId, AbbrMap, targetLang);
				ConvertAUniToXliff(possibility, possGroup, possId, RevNameMap, targetLang);
				ConvertAUniToXliff(possibility, possGroup, possId, RevAbbrMap, targetLang);
				ConvertAUniToXliff(possibility, possGroup, possId, RevAbbrevMap, targetLang);
				ConvertAUniToXliff(possibility, possGroup, possId, GlsAppendMap, targetLang);
				ConvertAStrToXliff(possibility, possGroup, possId, DescMap, targetLang);
				ConvertQuestions(possibility, possGroup, possId, targetLang);
				ConvertSubPossibilities(possibility, possGroup, possibilityType,
					guidAtt == null ? possId : baseId, possId, targetLang);
				possibilitiesGroup.Add(possGroup);
				++possIndex;
			}
		}

		/// <summary>
		/// Store attributes in custom XLIFF elements
		/// </summary>
		private static void ConvertAttributeAsElement(AttConversionMap attMap, XElement sourceElement, XElement targetElement)
		{
			var attValue = sourceElement.Attribute(attMap.AttName)?.Value;
			if (attValue != null)
			{
				var attElemString = $"<sil:{attMap.SilEltName}>{SecurityElement.Escape(attValue)}</sil:{attMap.SilEltName}>";
				var attElement = XElement.Load(new XmlTextReader(attElemString,
					XmlNodeType.Element,
					new XmlParserContext(null, XliffUtils.NameSpaceManager, null, XmlSpace.None)));
				targetElement.Add(attElement);
			}
		}

		private static string GetListId(XElement list)
		{
			if (!list.HasAttributes || list.Attribute("owner") == null || list.Attribute("field") == null)
				throw new ArgumentException("List element is missing attributes. Bad source data for owner?");

			return list.Attribute("owner")?.Value + "_" + list.Attribute("field")?.Value;
		}
		#endregion

		#region Xliff to LocalizedLists format
		public static void CombineXliffFiles(List<string> xliffFiles, string outputList)
		{
			var localizedLists = new XDocument();
			localizedLists.Add(XElement.Parse($"<?xml version='1.0' encoding='UTF-8'?><Lists date='{DateTime.Now:MM/dd/yyy H:mm:ss zzz}'/>"));

			var xliffDocs = xliffFiles.Select(XDocument.Load).ToList();

			CombineXliffDocuments(xliffDocs, localizedLists);
			localizedLists.Save(outputList);
		}

		/// <remarks>
		/// By concatenating the XLIFF files into a single document before converting, we can restore the lists
		/// in the same order in which FieldWorks exported them.
		/// </remarks>
		private static void CombineXliffDocuments(List<XDocument> xliffDocs, XDocument localizedLists)
		{
			var targetLanguage = TargetLanguageOfXliffDoc(xliffDocs[0]);
			if (xliffDocs.Any(d => TargetLanguageOfXliffDoc(d) != targetLanguage))
			{
				throw new ArgumentException("All documents must share the same target language", nameof(xliffDocs));
			}
			var listsElement = localizedLists.Root;
			var masterDoc = XDocument.Parse(string.Format(XliffUtils.BodyTemplate, "master.xlf"));
			masterDoc.XPathSelectElement("/xliff/file").Add(new XAttribute("target-language", targetLanguage));
			var masterBody = masterDoc.XPathSelectElement("/xliff/file/body");
			foreach (var xliffDocument in xliffDocs)
			{
				masterBody.Add(xliffDocument.XPathSelectElements("xliff/file/body/group"));
			}
			ConvertXliffToLists(masterDoc, listsElement);
		}

		private static string TargetLanguageOfXliffDoc(XDocument xliffDoc)
		{
			return xliffDoc.XPathSelectElement("/xliff/file").Attribute("target-language").Value;
		}

		internal static void ConvertXliffToLists(XDocument xliffList, XElement listsElement)
		{
			var fileElement = xliffList.XPathSelectElement("/xliff/file");
			var targetLanguage = fileElement.Attribute("target-language")?.Value;
			if(string.IsNullOrEmpty(targetLanguage))
				throw new ArgumentException("Missing target tanguage.");
			targetLanguage = NormalizeLocales.Normalize(targetLanguage);

			var groupElements = xliffList.XPathSelectElements("/xliff/file/body/group").ToDictionary(elt => elt.Attribute("id").Value);
			foreach (var listInfo in ListToXliffMap)
			{
				if (groupElements.TryGetValue($"{listInfo.Owner}_{listInfo.Field}", out var group))
				{
					ConvertGroupToList(group, listInfo, targetLanguage, listsElement);
				}
			}
		}

		private static void ConvertGroupToList(XElement group, ListInfo listInfo, string targetLanguage, XElement listsElement)
		{
			var listIdAttribute = group.Attribute("id");
			if(listIdAttribute == null)
				throw new ArgumentException("Invalid list group (no id)", nameof(group));
			var listIdParts = listIdAttribute.Value.Split('_');
			if(listIdParts.Length != 2)
				throw new ArgumentException($"Invalid list group id '{listIdAttribute}'", nameof(group));
			var listElement = XElement.Parse(
					$"<List owner=\"{listInfo.Owner}\" field=\"{listInfo.Field}\" itemClass=\"{listInfo.Type}\"/>");
			ConvertSourceAndTargetToAUnis(group, listElement, targetLanguage, NameMap);
			ConvertSourceAndTargetToAUnis(group, listElement, targetLanguage, AbbrMap);
			ConvertToMultiRunString(group, listElement, targetLanguage, DescMap);
			ConvertPossibilitiesFromXLiff(group, listElement, targetLanguage);
			listsElement.Add(listElement);
		}

		private static void ConvertToMultiRunString(XElement group, XElement listElement, string targetLanguage, ConversionMap item)
		{
			var id = group.Attribute("id")?.Value + item.IdSuffix;
			var subGroup = group.Elements("group").FirstOrDefault(g => g.Attribute("id") != null && g.Attribute("id").Value == id);
			if (subGroup == null)
				return;
			var elements = subGroup.Elements(XliffUtils.TransUnit).Where(tu => tu.Attribute("id")?.Value.StartsWith(id) ?? false);
			// ReSharper disable PossibleMultipleEnumeration
			if (elements.Any())
			{
				var element = XElement.Parse($"<{item.ElementName}/>");
				var source = XElement.Parse("<AStr ws='en'/>");
				var target = XElement.Parse($"<AStr ws='{targetLanguage}'/>");
				element.Add(source);
				element.Add(target);
				foreach (var transUnit in elements)
				{
					ConvertSourceAndTargetToRun(transUnit, source, target, targetLanguage);
				}
				listElement.Add(element);
			}
			// ReSharper restore PossibleMultipleEnumeration
		}

		/// <remarks>
		/// These are only found in Semantic Domains, but it doesn't hurt to handle them generically
		/// </remarks>
		private static void ConvertQuestionsFromXliff(XElement group, XElement listElement, string targetLanguage)
		{
			var id = group.Attribute("id")?.Value + QuestionsMap.IdSuffix;
			var questionsGroup = group.Elements("group").FirstOrDefault(g => g.Attribute("id")?.Value == id);
			if (questionsGroup == null)
				return;
			var questions = questionsGroup.Elements("group")
				.Where(g => g.Attribute("id")?.Value.StartsWith(id) ?? false).ToArray();
			if (!questions.Any())
				return;
			var questionsElement = XElement.Parse($"<{QuestionsMap.ElementName}/>");
			foreach (var question in questions)
			{
				var domainQElement = XElement.Parse("<CmDomainQ/>");
				ConvertSourceAndTargetToAUnis(question, domainQElement, targetLanguage, QuestionMap);
				ConvertSourceAndTargetToAUnis(question, domainQElement, targetLanguage, ExWordsMap);
				ConvertToMultiRunString(question, domainQElement, targetLanguage, ExSentencesMap);
				questionsElement.Add(domainQElement);
			}
			listElement.Add(questionsElement);
		}

		private static void ConvertSourceAndTargetToRun(XElement astrTransUnit, XElement source, XElement target, string targetLanguage)
		{
			var xliffSource = astrTransUnit.Element("source");
			var xliffTarget = astrTransUnit.Element("target");
			var sourceRun = XElement.Parse("<Run ws='en'/>");
			var targetRun = XElement.Parse($"<Run ws='{targetLanguage}'/>");
			sourceRun.Add(xliffSource?.Value);
			if(XliffUtils.IsTranslated(xliffTarget))
			{
				targetRun.Add(xliffTarget?.Value);
			}
			source.Add(sourceRun);
			target.Add(targetRun);
		}

		private static void ConvertSourceAndTargetToAUnis(XElement group, XElement listElement,
			string targetLanguage, ConversionMap item)
		{
			var id = group.Attribute("id")?.Value + item.IdSuffix;
			var transUnit = group.Elements(XliffUtils.TransUnit).FirstOrDefault(tu => tu.Attribute("id")?.Value == id);
			if (transUnit == null)
			{
				return;
			}
			var destElement = XElement.Parse($"<{item.ElementName}/>");
			var source = XElement.Parse("<AUni ws='en'/>");
			var target = XElement.Parse($"<AUni ws='{targetLanguage}'/>");
			source.Add(transUnit.Element("source")?.Value);
			var xliffTarget = transUnit.Element("target");
			if (XliffUtils.IsTranslated(xliffTarget))
			{
				target.Add(xliffTarget.Value);
			}
			destElement.Add(source);
			destElement.Add(target);
			listElement.Add(destElement);
		}

		private static void ConvertPossibilitiesFromXLiff(XElement group, XElement listElement, string targetLanguage)
		{
			var possibilitiesId = group.Attribute("id")?.Value + PossMap.IdSuffix;
			var xliffPossGroup = group.Elements("group").FirstOrDefault(g => g.Attribute("id") != null && g.Attribute("id").Value == possibilitiesId);
			if (xliffPossGroup == null)
				return;
			var possGroup = new XElement(PossMap.ElementName);
			var xliffPossibilities = xliffPossGroup.Elements("group").Where(g => g.Attribute("id") != null);
			foreach (var possItem in xliffPossibilities)
			{
				ConvertPossibilityFromXliff(possItem, listElement.Attribute("itemClass").Value, possGroup, targetLanguage);
			}

			if(possGroup.HasElements)
			{
				listElement.Add(possGroup);
			}
		}

		private static void ConvertPossibilityFromXliff(XElement possItem, string itemClass, XElement possGroup, string targetLanguage)
		{
			var possElem = XElement.Parse($"<{possItem.Element(XliffUtils.SilNamespace + "type")?.Value ?? itemClass}/>");
			ConvertAttributeFromXliff(GuidMap, possItem, possElem);
			ConvertSourceAndTargetToAUnis(possItem, possElem, targetLanguage, NameMap);
			ConvertSourceAndTargetToAUnis(possItem, possElem, targetLanguage, AbbrMap);
			ConvertToMultiRunString(possItem, possElem, targetLanguage, DescMap); // this is where descriptions are ordered in the export from FLEx
			ConvertSourceAndTargetToAUnis(possItem, possElem, targetLanguage, RevNameMap);
			ConvertSourceAndTargetToAUnis(possItem, possElem, targetLanguage, RevAbbrMap);
			ConvertSourceAndTargetToAUnis(possItem, possElem, targetLanguage, RevAbbrevMap);
			ConvertSourceAndTargetToAUnis(possItem, possElem, targetLanguage, GlsAppendMap);
			ConvertQuestionsFromXliff(possItem, possElem, targetLanguage);
			ConvertSubPossibilitiesFromXliff(possItem, possElem, itemClass, targetLanguage);
			possGroup.Add(possElem);
		}

		/// <summary>
		/// Convert attributes that were stored in custom XLIFF elements
		/// </summary>
		private static void ConvertAttributeFromXliff(AttConversionMap attMap, XElement source, XElement target, XElement optionalTarget = null)
		{
			var attElem = source.Element(XliffUtils.SilNamespace + attMap.SilEltName);
			if (attElem != null)
			{
				target.SetAttributeValue(attMap.AttName, attElem.Value);
				optionalTarget?.SetAttributeValue(attMap.AttName, attElem.Value);
			}
		}

		private static void ConvertSubPossibilitiesFromXliff(XElement possibility, XElement possElem, string itemClass, string targetLanguage)
		{
			var possibilitiesId = possibility.Attribute("id").Value + SubPosMap.IdSuffix;
			var xliffPossGroup = possibility.Elements("group").FirstOrDefault(g => g.Attribute("id") != null
																			&& g.Attribute("id").Value == possibilitiesId);
			if (xliffPossGroup == null)
				return;
			var possGroup = XElement.Parse($"<{SubPosMap.ElementName}/>");
			var xliffPossibilities = xliffPossGroup.Elements("group").Where(g => g.Attribute("id") != null);
			foreach (var possItem in xliffPossibilities)
			{
				ConvertPossibilityFromXliff(possItem, itemClass, possGroup, targetLanguage);
			}
			possElem.Add(possGroup);
		}

		#endregion
	}

	internal sealed class ListInfo : Tuple<string, string, string, string>
	{
		public ListInfo(string owner, string field, string type, string xliffFile) : base(owner,
			field, type, xliffFile)
		{
		}

		public string Owner => Item1;
		public string Field => Item2;
		public string Type => Item3;
		public string XliffFile => Item4;
	}

	internal struct SubTypeMap
	{
		public SubTypeMap(string superType, string subType)
		{
			SuperType = superType;
			SubType = subType;
		}

		public string SuperType { get; }
		public string SubType { get; }
	}
}