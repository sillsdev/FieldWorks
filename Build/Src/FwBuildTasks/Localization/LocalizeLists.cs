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
			var listElements = sourceDoc.Root?.Elements("List");
			if (listElements == null || !listElements.Any())
				throw new ArgumentException(
					"Source file is not in the expected format, no Lists found under the root element.");
			if (listElements.Count() != ExpectedListCount)
				throw new ArgumentException(
					$"Source file has an unexpected list count. {listElements.Count()} instead of {ExpectedListCount}");

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
				var group = XElement.Parse(string.Format(@"<group id='{0}'/>", listId));
				ConvertName(list, group, listId);
				ConvertAbbrev(list, group, listId);
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

		private static void ConvertName(XElement list, XElement group, string baseId)
		{
			var nameElements = list.Element("Name")?.Elements("AUni");
			var sourceNode = nameElements?.First();
			var transUnit = XElement.Parse(string.Format(
				EmptyTransUnit,
				 baseId + "_Name", sourceNode?.Value));
			group.Add(transUnit);
		}

		private static void ConvertAbbrev(XElement list, XElement group, string baseId)
		{
			var abbrevElements = list.Element("Abbreviation")?.Elements("AUni");
			var sourceNode = abbrevElements?.First();
			var transUnit = XElement.Parse(string.Format(
				EmptyTransUnit,
				baseId + "_Abbr", sourceNode?.Value));
			group.Add(transUnit);
		}

		private static void ConvertDescription(XElement owner, XElement group, string baseId)
		{
			var descriptionStrings = owner.Element("Description")?.Elements("AStr");
			var sourceNameNode = descriptionStrings?.First();
			if (sourceNameNode == null) // No description found, no work to do
			{
				return;
			}

			var descId = baseId + "_Desc";
			var descGroup = XElement.Parse(string.Format(EmptyGroup, descId));
			ConvertRunsToTransUnits(sourceNameNode, descId, descGroup);
			group.Add(descGroup);
		}

		private static void ConvertPossibilities(XElement owner, XElement group, string possibilityType, string baseId)
		{
			var possibilitiesElement = owner.Element("Possibilities");
			if (possibilitiesElement == null) // No Possibilities found, no work to do
			{
				return;
			}

			var possibilitiesId = baseId + "_Poss";
			var possibilitiesGroup = XElement.Parse(string.Format(EmptyGroup, possibilitiesId));
			AddPossibilitiesToGroup(possibilityType, possibilitiesElement, possibilitiesId, possibilitiesGroup);
			group.Add(possibilitiesGroup);
		}

		/// <summary>
		/// These are only found in Semantic Domains, but it doesn't hurt to handle them generically
		/// </summary>
		private static void ConvertQuestions(XElement owner, XElement group, string baseId)
		{
			var questionsElement = owner.Element("Questions");
			if (questionsElement == null) // No Possibilities found, no work to do
			{
				return;
			}

			var questionsId = baseId + "_Qs";
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
			var exampleStrings = question.Element("ExampleSentences")?.Elements("AStr");
			var sourceNameNode = exampleStrings?.First();
			if (sourceNameNode == null) // No description found, no work to do
			{
				return;
			}

			var examplesId = possId + "_ES";
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
				var transUnit = XElement.Parse(string.Format(
					"<trans-unit id='{0}'><source></source></trans-unit>",
					parentId + "_" + runIndex));
				transUnit.Element("source").Value = SecurityElement.Escape(run.Value);
				group.Add(transUnit);
				++runIndex;
			}
		}

		private static void ConvertQuestion(XElement question, XElement questionGroup, string possId)
		{
			var nameElements = question.Element("Question")?.Elements("AUni");
			var sourceNode = nameElements?.First();
			var transUnit = XElement.Parse(string.Format(
				EmptyTransUnit,
				possId + "_Q", sourceNode?.Value));
			questionGroup.Add(transUnit);
		}

		private static void ConvertExampleWords(XElement question, XElement questionGroup, string possId)
		{
			var nameElements = question.Element("ExampleWords")?.Elements("AUni");
			var sourceNode = nameElements?.First();
			if (sourceNode?.Value == null)
				return;
			var transUnit = XElement.Parse(string.Format(
				"<trans-unit id='{0}'><source></source></trans-unit>",
				possId + "_EW"));
			// ReSharper disable once AssignNullToNotNullAttribute -- Resharper isn't clever enough
			// ReSharper disable once PossibleNullReferenceException -- not possible because of string used to get this xml
			transUnit.Element("source").Value = SecurityElement.Escape(sourceNode.Value);
			questionGroup.Add(transUnit);
		}

		private static void ConvertSubPossibilities(XElement owner, XElement group, string possibilityType, string baseId)
		{
			var possibilitiesElement = owner.Element("SubPossibilities");
			if (possibilitiesElement == null) // No Possibilities found, no work to do
			{
				return;
			}

			var possibilitiesId = baseId + "_SubPos";
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
					var guidElemString = "<sil:Guid>" + possibility.Attribute("guid")?.Value + "</sil:Guid>";
					var guidElement = XElement.Load(new XmlTextReader(guidElemString,
						XmlNodeType.Element,
						new XmlParserContext(null, NameSpaceManager, null, XmlSpace.None)));
					possibilityGroup.Add(guidElement);
				}
				ConvertName(possibility, possibilityGroup, possId);
				ConvertAbbrev(possibility, possibilityGroup, possId);
				ConvertDescription(possibility, possibilityGroup, possId);
				ConvertSubPossibilities(possibility, possibilityGroup, possibilityType, possId);
				ConvertQuestions(possibility, possibilityGroup, possibilitiesId);
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
}