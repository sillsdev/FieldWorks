// Copyright (c) 2014-$year$ SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.UI.WebControls;
using System.Xml;
using Icu.Collation;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	public class LcmXhtmlGenerator : ILcmContentGenerator
	{
		internal const string CurrentEntryMarker = "blueBubble.png";
		private const string ImagesFolder = "Images";
		private const string PublicIdentifier = @"-//W3C//DTD XHTML 1.1//EN";

		/// <summary>
		/// This is the limit for the number of entries allowed on a single page of the output (used only when generating internal previews)
		/// </summary>
#if DEBUG
		// Assembling fragments and letter headings takes a long time when debugging, but doesn't seem to be a problem otherwise.
		// Spare developers some pain until resolving this becomes a priority.
		public const int EntriesPerPage = 100;
#else
		public const int EntriesPerPage = 1000;
#endif


		/// <summary>
		/// Saves the generated content in the Temp directory, to a unique but discoverable and somewhat stable location.
		/// </summary>
		/// <returns>The path to the XHTML file</returns>
		public static string SavePreviewHtmlWithStyles(int[] entryHvos, DictionaryPublicationDecorator publicationDecorator, DictionaryConfigurationModel configuration, XCore.PropertyTable propertyTable,
			IThreadedProgress progress = null, int entriesPerPage = EntriesPerPage)
		{
			var preferredPath = GetPreferredPreviewPath(configuration, propertyTable.GetValue<LcmCache>("cache"), entryHvos.Length == 1);
			var xhtmlPath = Path.ChangeExtension(preferredPath, "xhtml");
			try
			{
				SavePublishedHtmlWithStyles(entryHvos, publicationDecorator, entriesPerPage, configuration, propertyTable, xhtmlPath, progress);
			}
			catch (IOException ioEx)
			{
				// LT-17118: we should no longer be loading previews twice in a row, and each project gets its own tmp dir, but despite
				// our best efforts, we are previewing the same Config in the same Project two times "at once." Find a unique name.
				for (var i = 0; ioEx != null; ++i)
				{
					ioEx = null;
					xhtmlPath = Path.ChangeExtension(preferredPath + i, "xhtml");
					try
					{
						SavePublishedHtmlWithStyles(entryHvos, publicationDecorator, entriesPerPage, configuration, propertyTable, xhtmlPath, progress);
					}
					catch (IOException e)
					{
						ioEx = e; // somebody's way too busy; go around again
					}
				}
				Debug.WriteLine("{0}.xhtml was locked; preview saved to {1} instead", preferredPath, xhtmlPath);
			}
			return xhtmlPath;
		}

		/// <summary>
		/// Saves the generated content into the given xhtml and css file paths for all the entries in
		/// the given collection.
		/// </summary>
		public static void SavePublishedHtmlWithStyles(int[] entryHvos, DictionaryPublicationDecorator publicationDecorator, int entriesPerPage,
			DictionaryConfigurationModel configuration, XCore.PropertyTable propertyTable, string xhtmlPath, IThreadedProgress progress = null)
		{
			var entryCount = entryHvos.Length;
			var cssPath = Path.ChangeExtension(xhtmlPath, "css");
			var configDir = Path.GetDirectoryName(configuration.FilePath);
			var clerk = propertyTable.GetValue<RecordClerk>("ActiveClerk", null);
			var cache = propertyTable.GetValue<LcmCache>("cache", null);
			// Don't display letter headers if we're showing a preview in the Edit tool or we're not sorting by headword
			var wantLetterHeaders = (entryCount > 1 || !IsLexEditPreviewOnly(publicationDecorator)) && (RecordClerk.IsClerkSortingByHeadword(clerk));
			using (var xhtmlWriter = XmlWriter.Create(xhtmlPath))
			using (var cssWriter = new StreamWriter(cssPath, false, Encoding.UTF8))
			{
				var readOnlyPropertyTable = new ReadOnlyPropertyTable(propertyTable);
				var custCssPath = CssGenerator.CopyCustomCssAndGetPath(Path.GetDirectoryName(xhtmlPath), configDir);
				var settings = new ConfiguredLcmGenerator.GeneratorSettings(cache, readOnlyPropertyTable, true, true, Path.GetDirectoryName(xhtmlPath),
					ConfiguredLcmGenerator.IsEntryStyleRtl(readOnlyPropertyTable, configuration), Path.GetFileName(cssPath) == "configured.css");
				GenerateOpeningHtml(cssPath, custCssPath, settings, xhtmlWriter);
				Tuple<int, int> currentPageBounds = GetPageForCurrentEntry(settings, entryHvos, entriesPerPage);
				GenerateTopOfPageButtonsIfNeeded(settings, entryHvos, entriesPerPage, currentPageBounds, xhtmlWriter, cssWriter);
				string lastHeader = null;
				var itemsOnPage = currentPageBounds.Item2 - currentPageBounds.Item1;
				var entryContents = new Tuple<ICmObject, StringBuilder>[itemsOnPage + 1];
				var entryActions = new List<Action>();
				// For every entry in the page generate an action that will produce the xhtml document fragment for that entry
				for (var i = 0; i <= itemsOnPage; ++i)
				{
					var hvo = entryHvos.ElementAt(currentPageBounds.Item1 + i);
					var entry = cache.ServiceLocator.GetObject(hvo);
					var entryStringBuilder = new StringBuilder(100);
					entryContents[i] = new Tuple<ICmObject, StringBuilder>(entry, entryStringBuilder);

					var generateEntryAction = new Action(() =>
					{
						var entryContent = ConfiguredLcmGenerator.GenerateXHTMLForEntry(entry, configuration, publicationDecorator, settings);
						entryStringBuilder.Append(entryContent);
						if (progress != null)
							progress.Position++;
					});

					entryActions.Add(generateEntryAction);
				}
				if (progress != null)
					progress.Message = xWorksStrings.ksGeneratingDisplayFragments;
				// Generate all the document fragments (in parallel)
				ConfiguredLcmGenerator.SpawnEntryGenerationThreadsAndWait(entryActions, progress);
				// Generate the letter headers and insert the document fragments into the full xhtml file
				if (progress != null)
					progress.Message = xWorksStrings.ksArrangingDisplayFragments;

				var wsString = entryContents.Length > 0 ? ConfiguredLcmGenerator.GetWsForEntryType(entryContents[0].Item1, settings.Cache) : null;
				var col = FwUtils.GetCollatorForWs(wsString);

				foreach (var entryAndXhtml in entryContents)
				{
					if (wantLetterHeaders && !string.IsNullOrEmpty(entryAndXhtml.Item2.ToString()))
						GenerateLetterHeaderIfNeeded(entryAndXhtml.Item1, ref lastHeader, xhtmlWriter, col, settings);
					xhtmlWriter.WriteRaw(entryAndXhtml.Item2.ToString());
				}
				col?.Dispose();
				GenerateBottomOfPageButtonsIfNeeded(settings, entryHvos, entriesPerPage, currentPageBounds, xhtmlWriter);
				GenerateClosingHtml(xhtmlWriter);
				xhtmlWriter.Flush();

				if (progress != null)
					progress.Message = xWorksStrings.ksGeneratingStyleInfo;
				if (!IsLexEditPreviewOnly(publicationDecorator) && !IsExport(settings))
				{
					cssWriter.Write(CssGenerator.GenerateCssForSelectedEntry(settings.RightToLeft));
					ConfiguredLcmGenerator.CopyFileSafely(settings, Path.Combine(FwDirectoryFinder.FlexFolder, ImagesFolder, CurrentEntryMarker), CurrentEntryMarker);
				}
				cssWriter.Write(CssGenerator.GenerateCssFromConfiguration(configuration, readOnlyPropertyTable));
				cssWriter.Flush();
			}
		}

		private static bool IsExport(ConfiguredLcmGenerator.GeneratorSettings settings)
		{
			return !settings.ExportPath.StartsWith(Path.Combine(Path.GetTempPath(), "DictionaryPreview"));
		}

		internal static void GenerateLetterHeaderIfNeeded(ICmObject entry, ref string lastHeader, XmlWriter xhtmlWriter, Collator headwordWsCollator, ConfiguredLcmGenerator.GeneratorSettings settings)
		{
			// If performance is an issue these dummy's can be stored between calls
			var dummyOne = new Dictionary<string, ISet<string>>();
			var dummyTwo = new Dictionary<string, Dictionary<string, string>>();
			var dummyThree = new Dictionary<string, ISet<string>>();
			var cache = settings.Cache;
			var wsString = ConfiguredLcmGenerator.GetWsForEntryType(entry, settings.Cache);
			var firstLetter = ConfiguredExport.GetLeadChar(ConfiguredLcmGenerator.GetHeadwordForLetterHead(entry), wsString, dummyOne, dummyTwo, dummyThree,
				headwordWsCollator, cache);
			if (firstLetter != lastHeader && !string.IsNullOrEmpty(firstLetter))
			{
				var headerTextBuilder = new StringBuilder();
				var upperCase = new CaseFunctions(cache.ServiceLocator.WritingSystemManager.Get(wsString)).ToTitle(firstLetter);
				var lowerCase = firstLetter.Normalize();
				headerTextBuilder.Append(upperCase);
				if (lowerCase != upperCase)
				{
					headerTextBuilder.Append(' ');
					headerTextBuilder.Append(lowerCase);
				}
				xhtmlWriter.WriteStartElement("div");
				xhtmlWriter.WriteAttributeString("class", "letHead");
				xhtmlWriter.WriteStartElement("span");
				xhtmlWriter.WriteAttributeString("class", "letter");
				xhtmlWriter.WriteAttributeString("lang", wsString);
				var wsRightToLeft = cache.WritingSystemFactory.get_Engine(wsString).RightToLeftScript;
				if (wsRightToLeft != settings.RightToLeft)
					xhtmlWriter.WriteAttributeString("dir", wsRightToLeft ? "rtl" : "ltr");
				xhtmlWriter.WriteString(TsStringUtils.Compose(headerTextBuilder.ToString()));
				xhtmlWriter.WriteEndElement();
				xhtmlWriter.WriteEndElement();
				xhtmlWriter.WriteWhitespace(Environment.NewLine);

				lastHeader = firstLetter;
			}
		}

		/// <summary>
		/// Generates self-contained XHTML for a single entry for, eg, the preview panes in Lexicon Edit and the Dictionary Config dialog
		/// </summary>
		/// <returns>The HTML as a string</returns>
		public static string GenerateEntryHtmlWithStyles(ICmObject entry, DictionaryConfigurationModel configuration,
																		 DictionaryPublicationDecorator pubDecorator, XCore.PropertyTable propertyTable)
		{
			if (entry == null)
			{
				throw new ArgumentNullException("entry");
			}
			if (pubDecorator == null)
			{
				throw new ArgumentException("pubDecorator");
			}
			var configDir = Path.GetDirectoryName(configuration.FilePath);
			var projectPath = DictionaryConfigurationListener.GetProjectConfigurationDirectory(propertyTable);
			var previewCssPath = Path.Combine(projectPath, "Preview.css");
			var projType = new DirectoryInfo(configDir).Name;
			var cssName = projType == "Dictionary" ? "ProjectDictionaryOverrides.css" : "ProjectReversalOverrides.css";
			var custCssPath = Path.Combine(configDir, cssName);
			var stringBuilder = new StringBuilder();
			using (var writer = XmlWriter.Create(stringBuilder))
			using (var cssWriter = new StreamWriter(previewCssPath, false, Encoding.UTF8))
			{
				var readOnlyPropTable = new ReadOnlyPropertyTable(propertyTable);
				var exportSettings = new ConfiguredLcmGenerator.GeneratorSettings(readOnlyPropTable.GetValue<LcmCache>("cache"), readOnlyPropTable, false, false, null,
					ConfiguredLcmGenerator.IsEntryStyleRtl(readOnlyPropTable, configuration));
				GenerateOpeningHtml(previewCssPath, custCssPath, exportSettings, writer);
				var content = ConfiguredLcmGenerator.GenerateXHTMLForEntry(entry, configuration, pubDecorator, exportSettings);
				writer.WriteRaw(content);
				GenerateClosingHtml(writer);
				writer.Flush();
				cssWriter.Write(CssGenerator.GenerateCssFromConfiguration(configuration, readOnlyPropTable));
				cssWriter.Flush();
			}

			return stringBuilder.ToString();
		}

		private static void GenerateOpeningHtml(string cssPath, string custCssFile, ConfiguredLcmGenerator.GeneratorSettings exportSettings, XmlWriter xhtmlWriter)
		{
			xhtmlWriter.WriteDocType("html", PublicIdentifier, null, null);
			xhtmlWriter.WriteStartElement("html", "http://www.w3.org/1999/xhtml");
			xhtmlWriter.WriteAttributeString("lang", "utf-8");
			if (exportSettings.RightToLeft)
				xhtmlWriter.WriteAttributeString("dir", "rtl");
			xhtmlWriter.WriteStartElement("head");
			CreateLinkElement(cssPath, xhtmlWriter, exportSettings.ExportPath);
			CreateLinkElement(custCssFile, xhtmlWriter, exportSettings.ExportPath);
			// write out schema links for writing system metadata
			xhtmlWriter.WriteStartElement("link");
			xhtmlWriter.WriteAttributeString("href", "http://purl.org/dc/terms/");
			xhtmlWriter.WriteAttributeString("rel", "schema.DCTERMS");
			xhtmlWriter.WriteEndElement(); //</link>
			xhtmlWriter.WriteStartElement("link");
			xhtmlWriter.WriteAttributeString("href", "http://purl.org/dc/elements/1.1/");
			xhtmlWriter.WriteAttributeString("rel", "schema.DC");
			xhtmlWriter.WriteEndElement(); //</link>
			GenerateWritingSystemsMetadata(exportSettings, xhtmlWriter);
			xhtmlWriter.WriteStartElement("title");
			// cssPath should have the same filename as the current dictionary or reversal view
			xhtmlWriter.WriteString($"{Path.GetFileNameWithoutExtension(cssPath)} - {exportSettings.Cache.ProjectId.Name}");
			xhtmlWriter.WriteEndElement(); //</title>
			xhtmlWriter.WriteEndElement(); //</head>
			xhtmlWriter.WriteStartElement("body");
			if (exportSettings.RightToLeft)
				xhtmlWriter.WriteAttributeString("dir", "rtl");
			xhtmlWriter.WriteWhitespace(Environment.NewLine);
		}

		private static void CreateLinkElement(string cssFilePath, XmlWriter xhtmlWriter, string exportPath)
		{
			if (String.IsNullOrEmpty(cssFilePath) || !File.Exists(cssFilePath))
				return;

			var hrefValue = Path.GetFileName(cssFilePath);
			if (exportPath == null)
				//In some previews exportPath is null then we should use the full path for the link
				hrefValue = "file:///" + cssFilePath;

			xhtmlWriter.WriteStartElement("link");
			xhtmlWriter.WriteAttributeString("href", hrefValue);
			xhtmlWriter.WriteAttributeString("rel", "stylesheet");
			xhtmlWriter.WriteEndElement(); //</link>
		}

		private static void GenerateWritingSystemsMetadata(ConfiguredLcmGenerator.GeneratorSettings exportSettings, XmlWriter xhtmlWriter)
		{
			var lp = exportSettings.Cache.LangProject;
			var wsList = lp.CurrentAnalysisWritingSystems.Union(lp.CurrentVernacularWritingSystems.Union(lp.CurrentPronunciationWritingSystems));
			foreach (var ws in wsList)
			{
				xhtmlWriter.WriteStartElement("meta");
				xhtmlWriter.WriteAttributeString("name", "DC.language");
				xhtmlWriter.WriteAttributeString("content", String.Format("{0}:{1}", ws.LanguageTag, ws.LanguageName));
				xhtmlWriter.WriteAttributeString("scheme", "DCTERMS.RFC5646");
				xhtmlWriter.WriteEndElement();
				xhtmlWriter.WriteStartElement("meta");
				xhtmlWriter.WriteAttributeString("name", ws.LanguageTag);
				xhtmlWriter.WriteAttributeString("content", ws.DefaultFontName);
				xhtmlWriter.WriteAttributeString("scheme", "language to font");
				xhtmlWriter.WriteEndElement();
			}
		}

		private static void GenerateClosingHtml(XmlWriter xhtmlWriter)
		{
			xhtmlWriter.WriteWhitespace(Environment.NewLine);
			xhtmlWriter.WriteEndElement(); //</body>
			xhtmlWriter.WriteEndElement(); //</html>
		}

		private static string GetPreferredPreviewPath(DictionaryConfigurationModel config, LcmCache cache, bool isSingleEntryPreview)
		{
			var basePath = Path.Combine(Path.GetTempPath(), "DictionaryPreview", cache.ProjectId.Name);
			FileUtils.EnsureDirectoryExists(basePath);

			var confName = XhtmlDocView.MakeFilenameSafeForHtml(Path.GetFileNameWithoutExtension(config.FilePath));
			var fileName = isSingleEntryPreview ? confName + "-Preview" : confName;
			return Path.Combine(basePath, fileName);
		}

		private static bool IsLexEditPreviewOnly(DictionaryPublicationDecorator decorator)
		{
			return decorator == null;
		}

		private static void GenerateTopOfPageButtonsIfNeeded(ConfiguredLcmGenerator.GeneratorSettings settings, int[] entryHvos, int entriesPerPage, Tuple<int, int> currentPageBounds, XmlWriter xhtmlWriter, StreamWriter cssWriter)
		{
			var pageRanges = GetPageRanges(entryHvos, entriesPerPage);
			if (pageRanges.Count <= 1)
			{
				return;
			}
			GeneratePageButtons(settings, entryHvos, pageRanges, currentPageBounds, xhtmlWriter);
			cssWriter.Write(CssGenerator.GenerateCssForPageButtons());
		}

		private static void GenerateBottomOfPageButtonsIfNeeded(ConfiguredLcmGenerator.GeneratorSettings settings, int[] entryHvos, int entriesPerPage,
			Tuple<int, int> currentPageBounds, XmlWriter xhtmlWriter)
		{
			var pageRanges = GetPageRanges(entryHvos, entriesPerPage);
			if (pageRanges.Count <= 1)
			{
				return;
			}
			GeneratePageButtons(settings, entryHvos, pageRanges, currentPageBounds, xhtmlWriter);
		}

		public static List<string> GenerateNextFewEntries(DictionaryPublicationDecorator publicationDecorator, int[] entryHvos,
			string currentConfigPath, ConfiguredLcmGenerator.GeneratorSettings settings, Tuple<int, int> oldCurrentPageRange, Tuple<int, int> oldAdjacentPageRange,
			int entriesToAddCount, out Tuple<int, int> currentPage, out Tuple<int, int> adjacentPage)
		{
			GenerateAdjustedPageButtons(entryHvos, settings, oldCurrentPageRange, oldAdjacentPageRange,
				entriesToAddCount, out currentPage, out adjacentPage);
			var entries = new List<string>();
			DictionaryConfigurationModel currentConfig = new DictionaryConfigurationModel(currentConfigPath, settings.Cache);
			if (oldCurrentPageRange.Item1 > oldAdjacentPageRange.Item1)
			{
				var firstEntry = Math.Max(0, oldCurrentPageRange.Item1 - entriesToAddCount);
				for (var i = firstEntry; i < oldCurrentPageRange.Item1; ++i)
				{
					entries.Add(ConfiguredLcmGenerator.GenerateXHTMLForEntry(settings.Cache.ServiceLocator.ObjectRepository.GetObject(entryHvos[i]),
						currentConfig, publicationDecorator, settings));
				}
			}
			else
			{
				var lastEntry = Math.Min(oldAdjacentPageRange.Item2, oldCurrentPageRange.Item2 + entriesToAddCount);
				for (var i = oldCurrentPageRange.Item2 + 1; i <= lastEntry; ++i)
				{
					entries.Add(ConfiguredLcmGenerator.GenerateXHTMLForEntry(settings.Cache.ServiceLocator.ObjectRepository.GetObject(entryHvos[i]),
						currentConfig, publicationDecorator, settings));
				}
			}
			return entries;
		}

		internal static void GenerateAdjustedPageButtons(int[] entryHvos, ConfiguredLcmGenerator.GeneratorSettings settings, Tuple<int, int> currentPageRange, Tuple<int, int> adjacentPageRange,
			int entriesToAddCount, out Tuple<int, int> newCurrentPageRange, out Tuple<int, int> newAdjacentPageRange)
		{
			int currentPageStart;
			int currentPageEnd;
			var adjPageStart = -1;
			var adjPageEnd = -1;
			newAdjacentPageRange = null;
			var goingUp = currentPageRange.Item1 < adjacentPageRange.Item1;
			if (goingUp)
			{
				// If the current page range has swallowed up the adjacentPageRange
				if (currentPageRange.Item2 + entriesToAddCount >= adjacentPageRange.Item2)
				{
					currentPageStart = currentPageRange.Item1;
					currentPageEnd = adjacentPageRange.Item2;
				}
				else
				{
					currentPageStart = currentPageRange.Item1;
					currentPageEnd = currentPageRange.Item2 + entriesToAddCount;
					adjPageStart = adjacentPageRange.Item1 + entriesToAddCount;
					adjPageEnd = adjacentPageRange.Item2;
				}
			}
			else
			{
				// If the current page range has swallowed up the adjacentPageRange
				if (currentPageRange.Item1 - entriesToAddCount <= adjacentPageRange.Item1)
				{
					currentPageStart = Math.Max(currentPageRange.Item1 - entriesToAddCount, 0);
					currentPageEnd = currentPageRange.Item2;
				}
				else
				{
					adjPageStart = adjacentPageRange.Item1;
					adjPageEnd = adjacentPageRange.Item2 - entriesToAddCount;
					currentPageStart = currentPageRange.Item1 - entriesToAddCount;
					currentPageEnd = currentPageRange.Item2;
				}
			}
			newCurrentPageRange = new Tuple<int, int>(currentPageStart, currentPageEnd);
			if (adjPageStart != -1)
			{
				newAdjacentPageRange = new Tuple<int, int>(adjPageStart, adjPageEnd);
			}
		}

		private static void GeneratePageButtons(ConfiguredLcmGenerator.GeneratorSettings settings, int[] entryHvos, List<Tuple<int, int>> pageRanges, Tuple<int, int> currentPageBounds, XmlWriter xhtmlWriter)
		{
			xhtmlWriter.WriteStartElement("div");
			xhtmlWriter.WriteAttributeString("class", "pages");
			xhtmlWriter.WriteAttributeString("width", "100%");
			foreach (var page in pageRanges)
			{
				GeneratePageButton(settings, entryHvos, pageRanges, currentPageBounds, xhtmlWriter, page);
			}
			xhtmlWriter.WriteEndElement();
		}

		private static void GeneratePageButton(ConfiguredLcmGenerator.GeneratorSettings settings, int[] entryHvos, List<Tuple<int, int>> pageRanges,
			Tuple<int, int> currentPageBounds, XmlWriter xhtmlWriter, Tuple<int, int> page)
		{
			xhtmlWriter.WriteStartElement("span");
			xhtmlWriter.WriteAttributeString("class", "pagebutton");
			xhtmlWriter.WriteAttributeString("startIndex", page.Item1.ToString());
			xhtmlWriter.WriteAttributeString("endIndex", page.Item2.ToString());
			xhtmlWriter.WriteAttributeString("firstEntryGuid",
				settings.Cache.ServiceLocator.ObjectRepository.GetObject(entryHvos[page.Item1]).Guid.ToString());
			if (page.Equals(currentPageBounds))
			{
				xhtmlWriter.WriteAttributeString("id", "currentPageButton");
			}
			var wsString = ConfiguredLcmGenerator.GetWsForEntryType(settings.Cache.ServiceLocator.GetObject(entryHvos[page.Item1]), settings.Cache);
			xhtmlWriter.WriteAttributeString("lang", wsString);
			xhtmlWriter.WriteString(GeneratePageButtonText(entryHvos[page.Item1], entryHvos[page.Item2], settings, page.Item1 == 0));
			xhtmlWriter.WriteEndElement();
		}

		private static string GeneratePageButtonText(int firstEntryId, int lastEntryId, ConfiguredLcmGenerator.GeneratorSettings settings, bool isFirst)
		{
			var firstEntry = settings.Cache.ServiceLocator.GetObject(firstEntryId);
			var lastEntry = settings.Cache.ServiceLocator.GetObject(lastEntryId);
			var firstLetters = GetIndexLettersOfHeadword(ConfiguredLcmGenerator.GetHeadwordForLetterHead(firstEntry), isFirst);
			var lastLetters = GetIndexLettersOfHeadword(ConfiguredLcmGenerator.GetHeadwordForLetterHead(lastEntry));
			return firstEntryId == lastEntryId ? firstLetters : firstLetters + " .. " + lastLetters;
		}

		/// <summary>
		/// Get the page for the current entry, represented by the range of entries on the page containing the current entry
		/// </summary>
		private static Tuple<int, int> GetPageForCurrentEntry(ConfiguredLcmGenerator.GeneratorSettings settings, int[] entryHvos, int entriesPerPage)
		{
			var currentEntryHvo = 0;
			var clerk = settings.PropertyTable.GetValue<RecordClerk>("ActiveClerk");
			if (clerk != null)
			{
				currentEntryHvo = clerk.CurrentObjectHvo;
			}
			var pages = GetPageRanges(entryHvos, entriesPerPage);
			if (currentEntryHvo != 0)
			{
				var currentEntryIndex = Array.IndexOf(entryHvos, currentEntryHvo);
				foreach (Tuple<int, int> page in pages)
				{
					if (currentEntryIndex >= page.Item1 && currentEntryIndex < page.Item2)
					{
						return page;
					}
				}
			}
			return pages[0];
		}

		/// <summary>
		/// Return the first two letters of headword (or just one letter if headword is one character long, or if justFirstLetter is true
		/// </summary>
		private static string GetIndexLettersOfHeadword(string headWord, bool justFirstLetter = false)
		{
			// I don't know if we can have an empty headword. If we can then return empty string instead of crashing.
			if (headWord.Length == 0)
				return String.Empty;
			var length = ConfiguredExport.GetLetterLengthAt(headWord, 0);
			if (headWord.Length > length && !justFirstLetter)
			{
				length += ConfiguredExport.GetLetterLengthAt(headWord, length);
			}
			return TsStringUtils.Compose(headWord.Substring(0, length));
		}

		private static List<Tuple<int, int>> GetPageRanges(int[] entryHvos, int entriesPerPage)
		{
			if (entriesPerPage <= 0)
			{
				throw new ArgumentException(@"Bad page size", "entriesPerPage");
			}

			// Rather than complicate the logic below, handle the special cases of no entries and single entry first
			if (entryHvos.Length <= 1)
			{
				return new List<Tuple<int, int>>() { new Tuple<int, int>(0, entryHvos.Length - 1) };
			}

			var pageRanges = new List<Tuple<int, int>>();
			if (entryHvos.Length % entriesPerPage != 0) // If we didn't luck out and have exactly full pages
			{
				// If the last page is less than 10% of the max entries per page just add them to the last page
				if (entryHvos.Length % entriesPerPage <= entriesPerPage / 10)
				{
					// Generate a last page including the 10% or less overflow entries
					pageRanges.Add(new Tuple<int, int>(Math.Max(entryHvos.Length - entryHvos.Length % entriesPerPage - entriesPerPage, 0), entryHvos.Length - 1));
				}
				else
				{
					// Generate the page with the last entries
					pageRanges.Add(new Tuple<int, int>(Math.Max(entryHvos.Length - entryHvos.Length % entriesPerPage, 0),
						entryHvos.Length - 1));
				}
			}
			else
			{
				// Generate the page with the last 'entriesPerPage' number of entries
				pageRanges.Add(new Tuple<int, int>(entryHvos.Length - entriesPerPage, entryHvos.Length - 1));
			}
			while (pageRanges[0].Item1 != 0)
			{
				pageRanges.Insert(0, new Tuple<int, int>(Math.Max(0, pageRanges[0].Item1 - entriesPerPage), pageRanges[0].Item1 - 1));
			}
			return pageRanges;
		}

		public string GenerateWsPrefixWithString(ConfiguredLcmGenerator.GeneratorSettings settings, bool displayAbbreviation, int wsId, string content)
		{
			var bldr = new StringBuilder();
			using (var xw = XmlWriter.Create(bldr, new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment }))
			{
				if (displayAbbreviation)
				{
					xw.WriteStartElement("span");
					xw.WriteAttributeString("class", CssGenerator.WritingSystemPrefix);
					var prefix = ((CoreWritingSystemDefinition)settings.Cache.WritingSystemFactory.get_EngineOrNull(wsId)).Abbreviation;
					xw.WriteString(prefix);
					xw.WriteEndElement();
				}
				xw.WriteRaw(content);
				xw.Flush();
				return bldr.ToString();
			}
		}

		public string GenerateAudioLinkContent(string classname, string srcAttribute, string caption, string safeAudioId)
		{
			var bldr = new StringBuilder();
			using (var xw = XmlWriter.Create(bldr, new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment }))
			{
				xw.WriteStartElement("audio");
				xw.WriteAttributeString("id", safeAudioId);
				xw.WriteStartElement("source");
				xw.WriteAttributeString("src", srcAttribute);
				xw.WriteRaw("");
				xw.WriteFullEndElement();
				xw.WriteEndElement();
				xw.WriteStartElement("a");
				xw.WriteAttributeString("class", classname);
				xw.WriteAttributeString("href", "#" + safeAudioId);
				xw.WriteAttributeString("onclick", "document.getElementById('" + safeAudioId + "').play()");
				if (!String.IsNullOrEmpty(caption))
					xw.WriteString(caption);
				else
					xw.WriteRaw("");
				xw.WriteFullEndElement();
				xw.Flush();
				return bldr.ToString();
			}
		}

		public string WriteProcessedObject(bool isBlock, string elementContent, string className)
		{
			return WriteProcessedContents(isBlock, elementContent, className);
		}

		public string WriteProcessedCollection(bool isBlock, string elementContent, string className)
		{
			return WriteProcessedContents(isBlock, elementContent, className);
		}

		private string WriteProcessedContents(bool asBlock, string xmlContent, string className)
		{
			if (!String.IsNullOrEmpty(xmlContent))
			{
				var bldr = new StringBuilder();
				using (var xw = XmlWriter.Create(bldr, new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment }))
				{
					xw.WriteStartElement(asBlock ? "div" : "span");
					if (!String.IsNullOrEmpty(className))
						xw.WriteAttributeString("class", className);
					xw.WriteRaw(xmlContent);
					xw.WriteEndElement();
					xw.Flush();
					return bldr.ToString();
				}
			}
			return String.Empty;
		}

		public string GenerateGramInfoBeforeSensesContent(string content)
		{
			var bldr = new StringBuilder();
			using (var xw = XmlWriter.Create(bldr, new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment }))
			{
				xw.WriteStartElement("span");
				xw.WriteAttributeString("class", "sharedgrammaticalinfo");
				xw.WriteRaw(content);
				xw.WriteEndElement();
				xw.Flush();
				return bldr.ToString();
			}
		}

		public string GenerateGroupingNode(object field, ConfigurableDictionaryNode config,
			DictionaryPublicationDecorator publicationDecorator, ConfiguredLcmGenerator.GeneratorSettings settings,
			Func<object, ConfigurableDictionaryNode, DictionaryPublicationDecorator, ConfiguredLcmGenerator.GeneratorSettings, string> childContentGenrator)
		{
			var bldr = new StringBuilder();
			using (var xw = XmlWriter.Create(bldr, new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment }))
			{
				xw.WriteStartElement("span");
				xw.WriteAttributeString("class", CssGenerator.GetClassAttributeForConfig(config));

				var innerBuilder = new StringBuilder();
				foreach (var child in config.ReferencedOrDirectChildren)
				{
					var childContent = childContentGenrator(field, child, publicationDecorator, settings);
					innerBuilder.Append(childContent);
				}
				var innerContents = innerBuilder.ToString();
				if (String.IsNullOrEmpty(innerContents))
					return String.Empty;
				xw.WriteRaw(innerContents);
				xw.WriteEndElement(); // </span>
				xw.Flush();
			}
			return bldr.ToString();
		}

		public IFragmentWriter CreateWriter(StringBuilder bldr)
		{
			return new XmlFragmentWriter(XmlWriter.Create(bldr, new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment }));
		}

		public class XmlFragmentWriter : IFragmentWriter
		{
			public XmlWriter Writer { get; }
			public XmlFragmentWriter(XmlWriter xmlWriter)
			{
				Writer = xmlWriter;
			}

			public void Dispose()
			{
				Writer.Dispose();
			}

			public void Flush()
			{
				Writer.Flush();
			}
		}

		public void StartMultiRunString(IFragmentWriter writer, string writingSystem)
		{
			var xw = ((XmlFragmentWriter)writer).Writer;
			xw.WriteStartElement("span");
			xw.WriteAttributeString("lang", writingSystem);
		}

		public void EndMultiRunString(IFragmentWriter writer)
		{

			((XmlFragmentWriter)writer).Writer.WriteEndElement(); // </span> (lang)
		}

		public void StartBiDiWrapper(IFragmentWriter writer, bool rightToLeft)
		{
			var xw = ((XmlFragmentWriter)writer).Writer;
			xw.WriteStartElement("span"); // set direction on a nested span to preserve Context's position and direction.
			xw.WriteAttributeString("dir", rightToLeft ? "rtl" : "ltr");
		}

		public void EndBiDiWrapper(IFragmentWriter writer)
		{

			((XmlFragmentWriter)writer).Writer.WriteEndElement(); // </span> (dir)
		}

		public void StartRun(IFragmentWriter writer, string writingSystem)
		{
			var xw = ((XmlFragmentWriter)writer).Writer;
			xw.WriteStartElement("span");
			xw.WriteAttributeString("lang", writingSystem);
		}

		public void EndRun(IFragmentWriter writer)
		{

			((XmlFragmentWriter)writer).Writer.WriteEndElement(); // span
		}

		public void SetRunStyle(IFragmentWriter writer, string css)
		{
			((XmlFragmentWriter)writer).Writer.WriteAttributeString("style", css);
		}

		public void StartLink(IFragmentWriter writer, Guid destination)
		{
			var xw = ((XmlFragmentWriter)writer).Writer;
			xw.WriteStartElement("a");
			xw.WriteAttributeString("href", "#g" + destination);
		}

		public void EndLink(IFragmentWriter writer)
		{
			((XmlFragmentWriter)writer).Writer.WriteEndElement(); // </a>
		}

		public void AddToRunContent(IFragmentWriter writer, string txtContent)
		{
			((XmlFragmentWriter)writer).Writer.WriteString(txtContent);
		}

		public void AddLineBreakInRunContent(IFragmentWriter writer)
		{
			var xw = ((XmlFragmentWriter)writer).Writer;
			xw.WriteStartElement("br");
			xw.WriteEndElement();
		}

		public void StartTable(IFragmentWriter writer)
		{
			((XmlFragmentWriter)writer).Writer.WriteStartElement("table");
		}

		public void AddTableTitle(IFragmentWriter writer, string content)
		{
			var xw = ((XmlFragmentWriter)writer).Writer;
			xw.WriteStartElement("caption");
			xw.WriteRaw(content);
			xw.WriteEndElement(); // </caption>
		}

		public void StartTableBody(IFragmentWriter writer)
		{
			((XmlFragmentWriter)writer).Writer.WriteStartElement("tbody");
		}

		public void StartTableRow(IFragmentWriter writer)
		{
			((XmlFragmentWriter)writer).Writer.WriteStartElement("tr");
		}

		/// <summary>
		/// Adds a &lt;td&gt; element (or &lt;th&gt; if isHead is true).
		/// If isRightAligned is true, adds the appropriate style element.
		/// </summary>
		public void AddTableCell(IFragmentWriter writer, bool isHead, int colSpan, HorizontalAlign alignment, string content)
		{
			var xw = ((XmlFragmentWriter)writer).Writer;
			xw.WriteStartElement(isHead ? "th" : "td");
			if (colSpan > 1)
			{
				xw.WriteAttributeString("colspan", colSpan.ToString());
			}
			switch (alignment)
			{
				case HorizontalAlign.NotSet:
					break;
				case HorizontalAlign.Left:
					xw.WriteAttributeString("style", "text-align: left;");
					break;
				case HorizontalAlign.Center:
					xw.WriteAttributeString("style", "text-align: center;");
					break;
				case HorizontalAlign.Right:
					xw.WriteAttributeString("style", "text-align: right;");
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(alignment), alignment, null);
			}
			xw.WriteRaw(content);
			// WriteFullEndElement in case there is no content
			xw.WriteFullEndElement(); // </td> or </th>
		}

		public void EndTableRow(IFragmentWriter writer)
		{
			((XmlFragmentWriter)writer).Writer.WriteEndElement(); // should be </tr>
		}

		public void EndTableBody(IFragmentWriter writer)
		{
			// WriteFullEndElement in case there is no content
			((XmlFragmentWriter)writer).Writer.WriteFullEndElement(); // should be </tbody>
		}

		public void EndTable(IFragmentWriter writer)
		{
			((XmlFragmentWriter)writer).Writer.WriteEndElement(); // should be </table>
		}

		public void StartEntry(IFragmentWriter writer, string className, Guid entryGuid, int index)
		{
			var xw = ((XmlFragmentWriter)writer).Writer;
			xw.WriteStartElement("div");
			xw.WriteAttributeString("class", className);
			xw.WriteAttributeString("id", "g" + entryGuid);
		}

		public void AddEntryData(IFragmentWriter writer, List<string> pieces)
		{
			pieces.ForEach(((XmlFragmentWriter)writer).Writer.WriteRaw);
		}

		public void EndEntry(IFragmentWriter writer)
		{
			EndObject(writer);
		}

		public void AddCollection(IFragmentWriter writer, bool isBlockProperty,
			string className, string content)
		{
			var xw = ((XmlFragmentWriter)writer).Writer;
			xw.WriteStartElement(isBlockProperty ? "div" : "span");
			xw.WriteAttributeString("class", className);
			xw.WriteRaw(content);
			xw.WriteEndElement();
		}

		public void BeginObjectProperty(IFragmentWriter writer, bool isBlockProperty,
			string className)
		{
			var xw = ((XmlFragmentWriter)writer).Writer;
			xw.WriteStartElement(isBlockProperty ? "div" : "span");
			xw.WriteAttributeString("class", className);
		}

		public void EndObject(IFragmentWriter writer)
		{
			((XmlFragmentWriter)writer).Writer.WriteEndElement(); // </div> or </span>
		}

		public void WriteProcessedContents(IFragmentWriter writer, string contents)
		{
			((XmlFragmentWriter)writer).Writer.WriteRaw(contents);
		}

		/// <summary/>
		/// <param name="pictureGuid">This is used as an id in the xhtml and must be unique.</param>
		public string AddImage(string classAttribute, string srcAttribute, string pictureGuid)
		{
			var bldr = new StringBuilder();
			using (var xw = XmlWriter.Create(bldr, new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment }))
			{
				xw.WriteStartElement("img");
				xw.WriteAttributeString("class", classAttribute);
				xw.WriteAttributeString("src", srcAttribute);
				xw.WriteAttributeString("id", "g" + pictureGuid);
				xw.WriteEndElement();
				xw.Flush();
				return bldr.ToString();
			}
		}

		public string AddImageCaption(string captionContent)
		{
			var bldr = new StringBuilder();
			using (var xw = XmlWriter.Create(bldr, new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment }))
			{
				xw.WriteStartElement("div");
				xw.WriteAttributeString("class", "captionContent");
				xw.WriteRaw(captionContent);
				xw.WriteEndElement();
				xw.Flush();
				return bldr.ToString();
			}
		}

		public string GenerateSenseNumber(string formattedSenseNumber)
		{
			var bldr = new StringBuilder();
			using (var xw = XmlWriter.Create(bldr, new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment }))
			{
				xw.WriteStartElement("span");
				xw.WriteAttributeString("class", "sensenumber");
				xw.WriteString(formattedSenseNumber);
				xw.WriteEndElement();
				xw.Flush();
				return bldr.ToString();
			}
		}

		public string AddLexReferences(bool generateLexType, string lexTypeContent, string className,
			string referencesContent, bool typeBefore)
		{
			var bldr = new StringBuilder(100);
			// Generate the factored ref types element (if before).
			if (generateLexType && typeBefore)
			{
				bldr.Append(WriteProcessedObject(false, lexTypeContent, className));
			}
			// Then add all the contents for the LexReferences (e.g. headwords)
			bldr.Append(referencesContent);
			// Generate the factored ref types element (if after).
			if (generateLexType && !typeBefore)
			{
				bldr.Append(WriteProcessedObject(false, lexTypeContent, className));
			}

			return bldr.ToString();
		}

		public void BeginCrossReference(IFragmentWriter writer, bool isBlockProperty, string classAttribute)
		{
			BeginObjectProperty(writer, isBlockProperty, classAttribute);
		}

		public void EndCrossReference(IFragmentWriter writer)
		{
			EndObject(writer);
		}

		public string WriteProcessedSenses(bool isBlock, string sensesContent, string classAttribute, string sharedGramInfo)
		{
			return WriteProcessedObject(isBlock, sharedGramInfo + sensesContent, classAttribute);
		}

		public string AddAudioWsContent(string className, Guid linkTarget, string fileContent)
		{
			// No additional wrapping required for the xhtml
			return fileContent;
		}

		public string GenerateErrorContent(StringBuilder badStrBuilder)
		{
			return $"<span>\u0FFF\u0FFF\u0FFF<!-- Error generating content for string: '{badStrBuilder}'" +
				   $" invalid surrogate pairs replaced with \\u0fff --></span>";
		}

		public string GenerateVideoLinkContent(string className, string mediaId,
			string srcAttribute, string caption)
		{
			var bldr = new StringBuilder();
			using (var xw = XmlWriter.Create(bldr, new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment }))
			{
				// This creates a link that will open the video in the same window as the dictionary view/preview
				// refreshing will bring it back to the dictionary
				xw.WriteStartElement("a");
				xw.WriteAttributeString("id", mediaId);
				xw.WriteAttributeString("class", className);
				xw.WriteAttributeString("href", srcAttribute);
				if (!string.IsNullOrEmpty(caption))
					xw.WriteString(caption);
				else
					xw.WriteRaw("");
				xw.WriteFullEndElement();
				xw.Flush();
				return bldr.ToString();
			}
		}

		public string AddCollectionItem(bool isBlock, string collectionItemClass, string content)
		{
			var bldr = new StringBuilder();
			using (var xw = XmlWriter.Create(bldr, new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment }))
			{
				xw.WriteStartElement(isBlock ? "div" : "span");
				xw.WriteAttributeString("class", collectionItemClass);
				xw.WriteRaw(content);
				xw.WriteEndElement();
				xw.Flush();
				return bldr.ToString();
			}
		}

		public string AddProperty(string className, bool isBlockProperty, string content)
		{
			var bldr = new StringBuilder();
			using (var xw = XmlWriter.Create(bldr,
				new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment }))
			{
				xw.WriteStartElement(isBlockProperty ? "div" : "span");
				xw.WriteAttributeString("class", className);
				xw.WriteString(content);
				xw.WriteEndElement();
				xw.Flush();
				return bldr.ToString();
			}
		}

		public string AddSenseData(string senseNumberSpan, bool isBlock, Guid ownerGuid,
			string senseContent, string className)
		{
			var bldr = new StringBuilder();
			using (var xw = XmlWriter.Create(bldr, new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment }))
			{
				// Wrap the number and sense combination in a sensecontent span so that both can be affected by DisplayEachSenseInParagraph
				xw.WriteStartElement("span");
				xw.WriteAttributeString("class", "sensecontent");
				xw.WriteRaw(senseNumberSpan);
				xw.WriteStartElement(isBlock ? "div" : "span");
				xw.WriteAttributeString("class", className);
				xw.WriteAttributeString("entryguid", "g" + ownerGuid);
				xw.WriteRaw(senseContent);
				xw.WriteEndElement();   // element name for property
				xw.WriteEndElement();   // </span>
				xw.Flush();
				return bldr.ToString();
			}
		}

		/// <summary>
		/// The new webonary api takes json data for the entries and stores it in MongoDB. To use the css that we
		/// generate from FLEx that data needs to be retrieved and plugged into xhtml which has the same structure
		/// that our css generator expects. This method generates a template with placeholders for the data.
		/// We generate a template specific to the configuration we use to upload and make one template per model Part
		/// </summary>
		public static List<string> GenerateXHTMLTemplatesForConfigurationModel(DictionaryConfigurationModel model, LcmCache cache)
		{
			var xhtmlTemplates = new List<string>();
			foreach (var part in model.Parts.Where(pt => pt.IsEnabled))
			{
				xhtmlTemplates.Add(GenerateXHTMLTemplateForConfigNode(part, cache));
			}
			return xhtmlTemplates;
		}

		private static string GenerateXHTMLTemplateForConfigNode(ConfigurableDictionaryNode node, LcmCache cache, string pathToField = null, bool isReferenceNode = false)
		{
			var bldr = new StringBuilder();
			var importantThing = CssGenerator.GetClassAttributeForConfig(node);
			var pathToFieldRoot = pathToField == null ? "" : $"{pathToField}{(string.IsNullOrEmpty(pathToField) ? "" : ".")}{importantThing}";
			using (var xw = XmlWriter.Create(bldr, new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment }))
			{
				xw.WriteStartElement(ConfiguredLcmGenerator.IsBlockProperty(node) ? "div" : "span");
				xw.WriteAttributeString("class", CssGenerator.GetClassAttributeForConfig(node));

				var wsOpts = node.DictionaryNodeOptions as DictionaryNodeWritingSystemOptions;
				if (wsOpts != null)
				{
					var allSelectedLangs = new List<string>();
					foreach (var lang in wsOpts.Options.Where(opt => opt.IsEnabled))
					{
						var magicWs = WritingSystemServices.GetMagicWsIdFromName(lang.Id);
						if (magicWs == 0)
						{
							allSelectedLangs.Add(lang.Id);
						}
						else
						{
							switch (magicWs)
							{
								case WritingSystemServices.kwsAnal:
								case WritingSystemServices.kwsAnals:
								case WritingSystemServices.kwsFirstAnal:
								{
									allSelectedLangs.AddRange(cache.LangProject.CurAnalysisWss.Split(' '));
									break;
								}
								case WritingSystemServices.kwsVern:
								case WritingSystemServices.kwsVerns:
								case WritingSystemServices.kwsFirstVern:
								{
									allSelectedLangs.AddRange(cache.LangProject.CurVernWss.Split(' '));
									break;
								}
								case WritingSystemServices.kwsPronunciation:
								case WritingSystemServices.kwsPronunciations:
								case WritingSystemServices.kwsFirstPronunciation:
								{
									allSelectedLangs.AddRange(cache.LangProject.CurPronunWss.Split(' '));
									break;
								}
								case WritingSystemServices.kwsAnalVerns:
								case WritingSystemServices.kwsFirstAnalOrVern:
								{
									allSelectedLangs.AddRange(cache.LangProject.CurAnalysisWss.Split(' '));
									allSelectedLangs.AddRange(cache.LangProject.CurVernWss.Split(' '));
									break;
								}
								case WritingSystemServices.kwsVernAnals:
								case WritingSystemServices.kwsFirstVernOrAnal:
								{
									allSelectedLangs.AddRange(cache.LangProject.CurVernWss.Split(' '));
									allSelectedLangs.AddRange(cache.LangProject.CurAnalysisWss.Split(' '));
									break;
								}
							}
						}
					}

					foreach (var lang in allSelectedLangs)
					{
						xw.WriteStartElement("span");
						xw.WriteAttributeString("lang", lang);
						xw.WriteRaw(WrapInAnchorElementIfHeadword(node, pathToFieldRoot, $"%{pathToFieldRoot}.[lang={lang}].value%"));
						xw.WriteEndElement();
					}
				}

				if (node.ReferencedOrDirectChildren != null)
				{
					// Avoid infinite recursion (needs improvement)
					var childCollection = isReferenceNode ? node.Children : node.ReferencedOrDirectChildren;
					foreach (var child in childCollection)
					{
						xw.WriteRaw(GenerateXHTMLTemplateForConfigNode(child, cache, pathToFieldRoot, isReferenceNode || !string.IsNullOrEmpty(child.ReferenceItem)));
					}
				}
				xw.WriteEndElement();
				xw.Flush();
				return bldr.ToString();
			}
		}

		private static string WrapInAnchorElementIfHeadword(ConfigurableDictionaryNode node, string pathToFieldRoot, string templateContent)
		{
			return !node.IsHeadWord ? templateContent : $"<a href='%{pathToFieldRoot}.guid%'>{templateContent}</a>";
		}
	}
}