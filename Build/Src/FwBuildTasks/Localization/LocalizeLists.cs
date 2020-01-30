// Copyright (c) 2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
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
		private const string TempTargetLanguage = "es"; // TODO (Hasso) 2020.02: remove
		private const int ExpectedListCount = 29;
		private const string AcademicDomains = "AcademicDomains.xlf";
		private const string MiscLists = "MiscLists.xlf";
		private const string LexicalTypes = "LexicalTypes.xlf";
		private const string SemanticDomains = "SemanticDomains.xlf";
		private const string AnthropologyCategories = "AnthropologyCategories.xlf";
		private const string XliffBody = @"<?xml version='1.0'?>
			<xliff version='1.2' xmlns:sil='software.sil.org'>
				<file source-language='EN' datatype='plaintext' original='{0}'>
					<body>
					</body>
				</file>
			</xliff>";
		private const string EmptyGroup = "<group id='{0}'></group>";
		private const string EmptyTransUnit = "<trans-unit id='{0}'><source>{1}</source></trans-unit>";
		private const string TransUnit = "trans-unit";
		private static readonly XmlNamespaceManager NameSpaceManager = MakeNamespaceManager();

		private static XmlNamespaceManager MakeNamespaceManager()
		{
			var nsm = new XmlNamespaceManager(new NameTable());
			nsm.AddNamespace("sil", "software.sil.org");
			return nsm;
		}

		private static readonly List<ListInfo> ListToXliffMap = new List<ListInfo>
		{
			new ListInfo("LexDb", "DomainTypes", "CmPossibility", AcademicDomains),
			new ListInfo("LangProject", "AnthroList", "CmAnthroItem", AnthropologyCategories),
			new ListInfo("LexDb", "ComplexEntryTypes", "LexEntryType", LexicalTypes),
			new ListInfo("LexDb", "MorphTypes", "MoMorphType", LexicalTypes),
			new ListInfo("LexDb", "SenseTypes", "CmPossibility", LexicalTypes),
			new ListInfo("LexDb", "VariantEntryTypes", "LexEntryType", LexicalTypes),
			new ListInfo("DsDiscourseData", "ChartMarkers", "CmPossibility", MiscLists),
			new ListInfo("DsDiscourseData", "ConstChartTempl", "CmPossibility", MiscLists),
			new ListInfo("LangProject", "ConfidenceLevels", "CmPossibility", MiscLists),
			new ListInfo("LangProject", "Education", "CmPossibility", MiscLists),
			new ListInfo("LangProject", "GenreList", "CmPossibility", MiscLists),
			new ListInfo("LangProject", "Locations", "CmLocation", MiscLists),
			new ListInfo("LangProject", "PartsOfSpeech", "PartOfSpeech", MiscLists),
			new ListInfo("LangProject", "People", "CmPerson", MiscLists),
			new ListInfo("LangProject", "Positions", "CmPossibility", MiscLists),
			new ListInfo("LangProject", "Restrictions", "CmPossibility", MiscLists),
			new ListInfo("LangProject", "Roles", "CmPossibility", MiscLists),
			new ListInfo("LangProject", "Status", "CmPossibility", MiscLists),
			new ListInfo("LangProject", "TextMarkupTags", "CmPossibility", MiscLists),
			new ListInfo("LangProject", "TimeOfDay", "CmPossibility", MiscLists),
			new ListInfo("LangProject", "TranslationTags", "CmPossibility", MiscLists),
			new ListInfo("LexDb", "DialectLabels", "CmPossibility", MiscLists),
			new ListInfo("LexDb", "ExtendedNoteTypes", "CmPossibility", MiscLists),
			new ListInfo("LexDb", "Languages", "CmPossibility", MiscLists),
			new ListInfo("LexDb", "PublicationTypes", "CmPossibility", MiscLists),
			new ListInfo("LexDb", "References", "LexRefType", MiscLists),
			new ListInfo("LexDb", "UsageTypes", "CmPossibility", MiscLists),
			new ListInfo("RnResearchNbk", "RecTypes", "CmPossibility", MiscLists),
			new ListInfo("LangProject", "SemanticDomainList", "CmSemanticDomain", SemanticDomains)
		};

		private static readonly ConversionMap PossMap = new ConversionMap("Possibilities", "_Poss");
		private static readonly ConversionMap SubPosMap = new ConversionMap("SubPossibilities", "_SubPos");
		private static readonly ConversionMap NameMap = new ConversionMap("Name", "_Name");
		private static readonly ConversionMap AbbrMap = new ConversionMap("Abbreviation", "_Abbr");
		private static readonly ConversionMap RevNameMap = new ConversionMap("ReverseName", "_RevName");
		private static readonly ConversionMap RevAbbrMap = new ConversionMap("ReverseAbbr", "_RevAbbr");
		private static readonly ConversionMap DescMap = new ConversionMap("Description", "_Desc");
		private static readonly ConversionMap QuestionsMap = new ConversionMap("Questions", "_Qs");
		private static readonly ConversionMap QuestionMap = new ConversionMap("Question", "_Q");
		private static readonly ConversionMap ExWordsMap = new ConversionMap("ExampleWords", "_EW");
		private static readonly ConversionMap ExSentencesMap = new ConversionMap("ExampleSentences", "_ES");

		#region Xml LocalizedLists To Xliff
		/// <param name="sourceFile">path to the XML file containing lists to localize</param>
		/// <param name="localizationsRoot">path to save lists that are ready to upload to Crowdin</param>
		public static void SplitSourceLists(string sourceFile, string localizationsRoot)
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
				SplitLists(sourceXml, localizationsRoot);
			}
		}

		internal static void SplitLists(XmlTextReader sourceFile, string localizationsRoot)
		{
			var sourceDoc = XDocument.Load(sourceFile);
			var listElements = sourceDoc.Root?.Elements("List").ToArray();
			if (listElements == null || !listElements.Any())
				throw new ArgumentException(
					"Source file is not in the expected format, no Lists found under the root element.");
			if (listElements.Length != ExpectedListCount)
				throw new ArgumentException(
					$"Source file has an unexpected list count. {listElements.Length} instead of {ExpectedListCount}");

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

			var academicDomainsXliff = ConvertListToXliff(AcademicDomains, academicDomainsList);
			academicDomainsXliff.Save(Path.Combine(localizationsRoot, AcademicDomains));
			var miscListsXliff = ConvertListToXliff(MiscLists, miscLists);
			miscListsXliff.Save(Path.Combine(localizationsRoot, MiscLists));
			var lexicalTypesXliff = ConvertListToXliff(LexicalTypes, lexicalTypesLists);
			lexicalTypesXliff.Save(Path.Combine(localizationsRoot, LexicalTypes));
			var semanticDomainsXliff = ConvertListToXliff(SemanticDomains, semanticDomainsList);
			semanticDomainsXliff.Save(Path.Combine(localizationsRoot, SemanticDomains));
			var anthroCatXliff = ConvertListToXliff(AnthropologyCategories, anthropologyCatList);
			anthroCatXliff.Save(Path.Combine(localizationsRoot, AnthropologyCategories));
		}

		private static ListInfo FindListInfoForList(XElement list)
		{
			return ListToXliffMap.Find(x => list.Attribute("owner")?.Value == x.Owner
				&& list.Attribute("field")?.Value == x.Field);
		}

		internal static XDocument ConvertListToXliff(string listFileName, XDocument listsDoc)
		{
			var xliffDoc = XDocument.Parse(string.Format(XliffBody, listFileName));
			var bodyElem = xliffDoc.XPathSelectElement("/xliff/file/body", NameSpaceManager);
			foreach (var list in listsDoc.XPathSelectElements("/Lists/List"))
			{
				var listId = GetListId(list);
				var group = XElement.Parse($"<group id='{listId}'/>");
				ConvertAUniToXliff(list, group, listId, NameMap);
				ConvertAUniToXliff(list, group, listId, AbbrMap);
				ConvertDescription(list, group, listId);
				ConvertPossibilities(list, group, GetPossTypeForList(list), listId);
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

		private static void ConvertAUniToXliff(XElement list, XElement group, string baseId, ConversionMap item)
		{
			var source = list.Element(item.ElementName)?.Elements("AUni").First().Value;
			if (string.IsNullOrWhiteSpace(source))
			{
				return;
			}
			var transUnit = XElement.Parse($"<trans-unit id='{baseId}{item.IdSuffix}'><source>{source}</source></trans-unit>");
			group.Add(transUnit);
		}

		private static void ConvertDescription(XElement owner, XElement group, string baseId)
		{
			var descriptionStrings = owner.Element(DescMap.ElementName)?.Elements("AStr");
			var sourceNameNode = descriptionStrings?.First();
			if (sourceNameNode == null) // No description found, no work to do
			{
				return;
			}

			var descId = baseId + DescMap.IdSuffix;
			var descGroup = XElement.Parse(string.Format(EmptyGroup, descId));
			ConvertRunsToTransUnits(sourceNameNode, descId, descGroup);
			group.Add(descGroup);
		}

		private static void ConvertPossibilities(XElement owner, XElement group, string possibilityType, string baseId)
		{
			var possibilitiesElement = owner.Element(PossMap.ElementName);
			if (possibilitiesElement == null) // No Possibilities found, no work to do
			{
				return;
			}

			var possibilitiesId = baseId + PossMap.IdSuffix;
			var possibilitiesGroup = XElement.Parse(string.Format(EmptyGroup, possibilitiesId));
			AddPossibilitiesToGroup(possibilityType, possibilitiesElement, possibilitiesId, possibilitiesGroup);
			group.Add(possibilitiesGroup);
		}

		/// <summary>
		/// These are only found in Semantic Domains, but it doesn't hurt to handle them generically
		/// </summary>
		private static void ConvertQuestions(XElement owner, XElement group, string baseId)
		{
			var questionsElement = owner.Element(QuestionsMap.ElementName);
			if (questionsElement == null) // No Possibilities found, no work to do
			{
				return;
			}

			var questionsId = baseId + QuestionsMap.IdSuffix;
			var questionsGroup = XElement.Parse(string.Format(EmptyGroup, questionsId));
			AddQuestionsToGroup(questionsElement, questionsId, questionsGroup);
			group.Add(questionsGroup);
		}

		private static void AddQuestionsToGroup(XElement questionsElement, string questionsId, XElement questionsGroup)
		{
			var questionElement = questionsElement.Elements("CmDomainQ");
			int questionIndex = 0;
			foreach (var question in questionElement)
			{
				var possId = questionsId + "_" + questionIndex;
				var questionGroup = XElement.Parse(string.Format(EmptyGroup, possId));
				ConvertQuestion(question, questionGroup, possId);
				ConvertExampleWords(question, questionGroup, possId);
				ConvertExampleSentences(question, questionGroup, possId);
				questionsGroup.Add(questionGroup);
				++questionIndex;
			}
		}

		private static void ConvertExampleSentences(XElement question, XElement questionGroup, string possId)
		{
			var exampleStrings = question.Element(ExSentencesMap.ElementName)?.Elements("AStr");
			var sourceNameNode = exampleStrings?.First();
			if (sourceNameNode == null) // No description found, no work to do
			{
				return;
			}

			var examplesId = possId + ExSentencesMap.IdSuffix;
			var examplesGroup = XElement.Parse(string.Format(EmptyGroup, examplesId));
			ConvertRunsToTransUnits(sourceNameNode, examplesId, examplesGroup);
			questionGroup.Add(examplesGroup);
		}

		private static void ConvertRunsToTransUnits(XElement sourceNameNode, string parentId,
			XElement group)
		{
			int runIndex = 0;
			foreach (var run in sourceNameNode.Elements("Run"))
			{
				var transUnit = XElement.Parse($"<trans-unit id='{parentId + "_" + runIndex}'><source></source></trans-unit>");
				// ReSharper disable once AssignNullToNotNullAttribute - run.Value is never null
				transUnit.Element("source").Value = SecurityElement.Escape(run.Value);
				group.Add(transUnit);
				++runIndex;
			}
		}

		private static void ConvertQuestion(XElement question, XElement questionGroup, string possId)
		{
			var nameElements = question.Element(QuestionMap.ElementName)?.Elements("AUni");
			var sourceNode = nameElements?.First();
			var transUnit = XElement.Parse(string.Format(
				EmptyTransUnit,
				possId + QuestionMap.IdSuffix, sourceNode?.Value));
			questionGroup.Add(transUnit);
		}

		private static void ConvertExampleWords(XElement question, XElement questionGroup, string possId)
		{
			var nameElements = question.Element(ExWordsMap.ElementName)?.Elements("AUni");
			var sourceNode = nameElements?.First();
			if (sourceNode?.Value == null)
				return;
			var transUnit = XElement.Parse($"<trans-unit id='{possId}{ExWordsMap.IdSuffix}'><source></source></trans-unit>");
			// ReSharper disable once AssignNullToNotNullAttribute -- ReSharper isn't clever enough
			transUnit.Element("source").Value = SecurityElement.Escape(sourceNode.Value);
			questionGroup.Add(transUnit);
		}

		private static void ConvertSubPossibilities(XElement owner, XElement group, string possibilityType, string baseId)
		{
			var possibilitiesElement = owner.Element(SubPosMap.ElementName);
			if (possibilitiesElement == null) // No Possibilities found, no work to do
			{
				return;
			}

			var possibilitiesId = baseId + SubPosMap.IdSuffix;
			var possibilitiesGroup = XElement.Parse(string.Format(EmptyGroup, possibilitiesId));
			AddPossibilitiesToGroup(possibilityType, possibilitiesElement, possibilitiesId, possibilitiesGroup);
			group.Add(possibilitiesGroup);
		}

		private static void AddPossibilitiesToGroup(string possibilityType, XElement possibilitiesElement,
			string possibilitiesId, XElement possibilitiesGroup)
		{
			var possibilityElements = possibilitiesElement.Elements(possibilityType);
			int possIndex = 0;
			foreach (var possibility in possibilityElements)
			{
				var possId = possibilitiesId + "_" + possIndex;
				var possibilityGroup = XElement.Parse(string.Format(EmptyGroup, possId));
				if (possibility.Attribute("guid") != null)
				{
					var guidElemString = "<sil:guid>" + possibility.Attribute("guid")?.Value + "</sil:guid>";
					var guidElement = XElement.Load(new XmlTextReader(guidElemString,
						XmlNodeType.Element,
						new XmlParserContext(null, NameSpaceManager, null, XmlSpace.None)));
					possibilityGroup.Add(guidElement);
				}
				ConvertAUniToXliff(possibility, possibilityGroup, possId, NameMap);
				ConvertAUniToXliff(possibility, possibilityGroup, possId, AbbrMap);
				ConvertAUniToXliff(possibility, possibilityGroup, possId, RevNameMap);
				ConvertAUniToXliff(possibility, possibilityGroup, possId, RevAbbrMap);
				ConvertDescription(possibility, possibilityGroup, possId);
				ConvertSubPossibilities(possibility, possibilityGroup, possibilityType, possId);
				ConvertQuestions(possibility, possibilityGroup, possId);
				possibilitiesGroup.Add(possibilityGroup);
				++possIndex;
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

			var xliffDocs = new List<XDocument>();
			foreach (var file in xliffFiles)
			{
				xliffDocs.Add(XDocument.Load(file));
			}

			CombineXliffDocuments(xliffDocs, localizedLists);
			localizedLists.Save(outputList);
		}

		private static void CombineXliffDocuments(List<XDocument> xliffDocs, XDocument localizedLists)
		{
			var listsElement = localizedLists.Root;
			foreach (var xliffList in xliffDocs)
			{
				ConvertXliffToLists(xliffList, listsElement);
			}
		}

		internal static void ConvertXliffToLists(XDocument xliffList, XElement listsElement)
		{
			var groupElements = xliffList.XPathSelectElements("/xliff/file/body/group");
			foreach (var group in groupElements)
			{
				ConvertGroupToList(group, listsElement);
			}
		}

		private static void ConvertGroupToList(XElement group, XElement listsElement)
		{
			var listIdAttribute = group.Attribute("id");
			if(listIdAttribute == null)
				throw new ArgumentException("Invalid list group", nameof(group));
			var listIdParts = listIdAttribute.Value.Split('_');
			if(listIdParts.Length != 2)
				throw new ArgumentException("Invalid list group", nameof(group));
			var listInfo = ListToXliffMap.First(li => li.Owner == listIdParts[0] && li.Field == listIdParts[1]);
			var listElement = XElement.Parse(
					$"<List owner=\"{listInfo.Owner}\" field=\"{listInfo.Field}\" itemClass=\"{listInfo.Type}\"/>");
			ConvertSourceAndTargetToAUnis(group, listElement, NameMap);
			ConvertSourceAndTargetToAUnis(group, listElement, AbbrMap);
			ConvertToMultiRunString(group, listElement, DescMap);
			ConvertPossibilitiesFromXLiff(group, listElement);
			listsElement.Add(listElement);
		}

		private static void ConvertToMultiRunString(XElement group, XElement listElement, ConversionMap item)
		{
			var id = group.Attribute("id")?.Value + item.IdSuffix;
			var subGroup = group.Elements("group").FirstOrDefault(g => g.Attribute("id") != null && g.Attribute("id").Value == id);
			if (subGroup == null)
				return;
			var elements = subGroup.Elements(TransUnit).Where(tu => tu.Attribute("id")?.Value.StartsWith(id) ?? false);
			// ReSharper disable PossibleMultipleEnumeration
			if (elements.Any())
			{
				var element = XElement.Parse($"<{item.ElementName}/>");
				var source = XElement.Parse("<AStr ws='en'/>");
				var target = XElement.Parse($"<AStr ws='{TempTargetLanguage}'/>"); // TODO (Hasso) 2020.01: use the correct target language
				element.Add(source);
				element.Add(target);
				foreach (var transUnit in elements)
				{
					ConvertSourceAndTargetToRun(transUnit, source, target);
				}
				listElement.Add(element);
			}
			// ReSharper restore PossibleMultipleEnumeration
		}

		/// <remarks>
		/// These are only found in Semantic Domains, but it doesn't hurt to handle them generically
		/// </remarks>
		private static void ConvertQuestionsFromXliff(XElement group, XElement listElement)
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
				ConvertSourceAndTargetToAUnis(question, domainQElement, QuestionMap);
				ConvertSourceAndTargetToAUnis(question, domainQElement, ExWordsMap);
				ConvertToMultiRunString(question, domainQElement, ExSentencesMap);
				questionsElement.Add(domainQElement);
			}
			listElement.Add(questionsElement);
		}

		private static void ConvertSourceAndTargetToRun(XElement descTransUnit, XElement source, XElement target)
		{
			var xliffSource = SecurityElement.Escape(descTransUnit.Element("source")?.Value);
			var xliffTarget = SecurityElement.Escape(descTransUnit.Element("target")?.Value);
			source.Add(XElement.Parse($"<Run ws='en'>{xliffSource}</Run>"));
			target.Add(XElement.Parse($"<Run ws='{TempTargetLanguage}'>{(xliffTarget != xliffSource ? xliffTarget : string.Empty)}</Run>"));
		}

		private static void ConvertSourceAndTargetToAUnis(XElement group, XElement listElement, ConversionMap item)
		{
			var id = group.Attribute("id")?.Value + item.IdSuffix;
			var transUnit = group.Elements(TransUnit).FirstOrDefault(tu => tu.Attribute("id")?.Value == id);
			if (transUnit == null)
			{
				return;
			}
			var destElement = XElement.Parse($"<{item.ElementName}/>");
			var source = XElement.Parse("<AUni ws='en'/>");
			var target = XElement.Parse($"<AUni ws='{TempTargetLanguage}'/>");
			var xliffSource = transUnit.Element("source")?.Value;
			var xliffTarget = transUnit.Element("target")?.Value;
			target.Add(xliffTarget != xliffSource ? xliffTarget : string.Empty);
			source.Add(xliffSource);
			destElement.Add(source);
			destElement.Add(target);
			listElement.Add(destElement);
		}

		private static void ConvertPossibilitiesFromXLiff(XElement group, XElement listElement)
		{
			var possibilitiesId = group.Attribute("id")?.Value + PossMap.IdSuffix;
			var xliffPossGroup = group.Elements("group").FirstOrDefault(g => g.Attribute("id") != null && g.Attribute("id").Value == possibilitiesId);
			if (xliffPossGroup == null)
				return;
			var possGroup = XElement.Parse($"<{PossMap.ElementName}/>");
			var xliffPossibilities = xliffPossGroup.Elements("group").Where(g => g.Attribute("id") != null
															&& g.Attribute("id").Value.StartsWith(possibilitiesId));
			foreach (var possItem in xliffPossibilities)
			{
				ConvertPossibilityFromXliff(possItem, listElement.Attribute("itemClass").Value,
					possGroup);
			}
			listElement.Add(possGroup);
		}

		private static void ConvertPossibilityFromXliff(XElement possItem, string itemClass, XElement possGroup)
		{
			var possElem = XElement.Parse($"<{itemClass}/>");
			ConvertGuidFromXliff(possItem, possElem);
			ConvertSourceAndTargetToAUnis(possItem, possElem, NameMap);
			ConvertSourceAndTargetToAUnis(possItem, possElem, AbbrMap);
			ConvertSourceAndTargetToAUnis(possItem, possElem, RevNameMap);
			ConvertSourceAndTargetToAUnis(possItem, possElem, RevAbbrMap);
			ConvertToMultiRunString(possItem, possElem, DescMap);
			ConvertSubPossibilitiesFromXliff(possItem, possElem, itemClass);
			ConvertQuestionsFromXliff(possItem, possElem);
			possGroup.Add(possElem);
		}

		private static void ConvertGuidFromXliff(XElement possItem, XElement possElem)
		{
			XNamespace sil = "software.sil.org";
			var guidElem = possItem.Element(sil + "guid");
			if (guidElem != null)
			{
				possElem.SetAttributeValue("guid", guidElem.Value);
			}
		}

		private static void ConvertSubPossibilitiesFromXliff(XElement possibility, XElement possElem, string itemClass)
		{
			var possibilitiesId = possibility.Attribute("id").Value + SubPosMap.IdSuffix;
			var xliffPossGroup = possibility.Elements("group").FirstOrDefault(g => g.Attribute("id") != null
																			&& g.Attribute("id").Value == possibilitiesId);
			if (xliffPossGroup == null)
				return;
			var possGroup = XElement.Parse($"<{SubPosMap.ElementName}/>");
			var xliffPossibilities = xliffPossGroup.Elements("group").Where(g => g.Attribute("id") != null
																	&& g.Attribute("id").Value.StartsWith(possibilitiesId));
			foreach (var possItem in xliffPossibilities)
			{
				ConvertPossibilityFromXliff(possItem, itemClass, possGroup);
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

	internal struct ConversionMap
	{
		public ConversionMap(string elementName, string idSuffix)
		{
			ElementName = elementName;
			IdSuffix = idSuffix;
		}

		public string ElementName;
		public string IdSuffix;
	}
}