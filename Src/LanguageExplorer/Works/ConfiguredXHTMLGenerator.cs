// Copyright (c) 2014-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml;
using LanguageExplorer.Controls.XMLViews;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.FieldWorks.Filters;
using SIL.FieldWorks.Common.Framework;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Utils;
using FileUtils = SIL.LCModel.Utils.FileUtils;

namespace LanguageExplorer.Works
{
	/// <summary>
	/// This class groups the static methods used for generating XHTML, according to specified configurations, from Fieldworks model objects
	/// </summary>
	internal static class ConfiguredXHTMLGenerator
	{
		/// <summary>
		/// Click-to-play icon for media files
		/// </summary>
		internal const string LoudSpeaker = "\uD83D\uDD0A";
		internal const string MovieCamera = "\U0001F3A5";

		/// <summary>
		/// The Assembly that the model Types should be loaded from. Allows test code to introduce a test model.
		/// </summary>
		internal static string AssemblyFile { get; set; }

		/// <summary>
		/// Map of the Assembly to the file name, so that different tests can use different models
		/// </summary>
		internal static Dictionary<string, Assembly> AssemblyMap = new Dictionary<string, Assembly>();

		internal const string LookupComplexEntryType = "LookupComplexEntryType";

		private const string PublicIdentifier = @"-//W3C//DTD XHTML 1.1//EN";

		/// <summary>
		/// This is the limit for the number of entries allowed on a single page of the output (used only when generating internal previews)
		/// </summary>
		public const int EntriesPerPage = 1000;

		/// <summary>
		/// The number of entries to add to a page when the user asks to see 'a few more'
		/// </summary>
		/// <remarks>internal to facilitate unit tests</remarks>
		internal static int EntriesToAddCount { get; set; }

		internal const string CurrentEntryMarker = "blueBubble.png";
		private const string ImagesFolder = "Images";

		/// <summary>
		/// Static initializer setting the AssemblyFile to the default LCM dll.
		/// </summary>
		static ConfiguredXHTMLGenerator()
		{
			AssemblyFile = "SIL.LCModel";
			EntriesToAddCount = 5;
		}

		/// <summary>
		/// Generates self-contained XHTML for a single entry for, eg, the preview panes in Lexicon Edit and the Dictionary Config dialog
		/// </summary>
		/// <returns>The HTML as a string</returns>
		public static string GenerateEntryHtmlWithStyles(ICmObject entry, DictionaryConfigurationModel configuration, DictionaryPublicationDecorator pubDecorator, IPropertyTable propertyTable, LcmCache cache)
		{
			if (entry == null)
			{
				throw new ArgumentNullException(nameof(entry));
			}
			if (pubDecorator == null)
			{
				throw new ArgumentException(nameof(pubDecorator));
			}
			var configDir = Path.GetDirectoryName(configuration.FilePath);
			var projectPath = DictionaryConfigurationServices.GetProjectConfigurationDirectory(propertyTable);
			var previewCssPath = Path.Combine(projectPath, "Preview.css");
			var projType = new DirectoryInfo(configDir).Name;
			var cssName = projType == "Dictionary" ? "ProjectDictionaryOverrides.css" : "ProjectReversalOverrides.css";
			var custCssPath = Path.Combine(configDir, cssName);
			var stringBuilder = new StringBuilder();
			using (var writer = XmlWriter.Create(stringBuilder))
			using (var cssWriter = new StreamWriter(previewCssPath, false, Encoding.UTF8))
			{
				IReadonlyPropertyTable readOnlyPropertyTable = new ReadOnlyPropertyTable(propertyTable);
				var exportSettings = new GeneratorSettings(cache, readOnlyPropertyTable, false, false, null, IsNormalRtl(readOnlyPropertyTable));
				GenerateOpeningHtml(previewCssPath, custCssPath, exportSettings, writer);
				var content = GenerateXHTMLForEntry(entry, configuration, pubDecorator, exportSettings);
				writer.WriteRaw(content);
				GenerateClosingHtml(writer);
				writer.Flush();
				cssWriter.Write(CssGenerator.GenerateCssFromConfiguration(configuration, cache, FontHeightAdjuster.StyleSheetFromPropertyTable(readOnlyPropertyTable)));
				cssWriter.Flush();
			}

			return stringBuilder.ToString();
		}

		private static void GenerateOpeningHtml(string cssPath, string custCssFile, GeneratorSettings exportSettings, XmlWriter xhtmlWriter)
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
			// Use the WriteRaw, WriteFullEndElement hack to avoid a self closing tag which is invalid xhtml. This empty title is here to make more valid xhtml.
			xhtmlWriter.WriteRaw("");
			xhtmlWriter.WriteFullEndElement(); //</title>
			xhtmlWriter.WriteEndElement(); //</head>
			xhtmlWriter.WriteStartElement("body");
			if (exportSettings.RightToLeft)
				xhtmlWriter.WriteAttributeString("dir", "rtl");
			xhtmlWriter.WriteWhitespace(Environment.NewLine);
		}

		private static void CreateLinkElement(string cssFilePath, XmlWriter xhtmlWriter, string exportPath)
		{
			if (string.IsNullOrEmpty(cssFilePath) || !File.Exists(cssFilePath))
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

		private static void GenerateWritingSystemsMetadata(GeneratorSettings exportSettings, XmlWriter xhtmlWriter)
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

			var confName = DictionaryConfigurationServices.MakeFilenameSafeForHtml(Path.GetFileNameWithoutExtension(config.FilePath));
			var fileName = isSingleEntryPreview ? confName + "-Preview" : confName;
			return Path.Combine(basePath, fileName);
		}

		/// <summary>
		/// Saves the generated content in the Temp directory, to a unique but discoverable and somewhat stable location.
		/// </summary>
		/// <returns>The path to the XHTML file</returns>
		internal static string SavePreviewHtmlWithStyles(int[] entryHvos, DictionaryPublicationDecorator publicationDecorator, DictionaryConfigurationModel configuration, IPropertyTable propertyTable,
			LcmCache cache,
			IRecordList activeRecordList,
			IThreadedProgress progress = null, int entriesPerPage = EntriesPerPage)
		{
			var preferredPath = GetPreferredPreviewPath(configuration, cache, entryHvos.Length == 1);
			var xhtmlPath = Path.ChangeExtension(preferredPath, "xhtml");
			try
			{
				SavePublishedHtmlWithStyles(entryHvos, publicationDecorator, entriesPerPage, configuration, propertyTable, cache, activeRecordList, xhtmlPath, progress);
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
						SavePublishedHtmlWithStyles(entryHvos, publicationDecorator, entriesPerPage, configuration, propertyTable, cache, activeRecordList, xhtmlPath, progress);
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

		private static bool IsRecordListSortingByHeadword(IRecordList recordList)
		{
			if (recordList.SortName == null) return false;
			return recordList.SortName.StartsWith("Headword") || recordList.SortName.StartsWith("Lexeme Form") || recordList.SortName.StartsWith("Citation Form")
				|| recordList.SortName.StartsWith("Form") || recordList.SortName.StartsWith("Reversal Form");
		}

		private static bool IsNormalRtl(IReadonlyPropertyTable readOnlyPropertyTable)
		{
			// Right-to-Left for the overall layout is determined by Dictionary-Normal
			var dictionaryNormalStyle = new ExportStyleInfo(FontHeightAdjuster.StyleSheetFromPropertyTable(readOnlyPropertyTable).Styles["Dictionary-Normal"]);
			return dictionaryNormalStyle.DirectionIsRightToLeft == TriStateBool.triTrue; // default is LTR
		}

		/// <summary>
		/// Saves the generated content into the given xhtml and css file paths for all the entries in
		/// the given collection.
		/// </summary>
		internal static void SavePublishedHtmlWithStyles(int[] entryHvos, DictionaryPublicationDecorator publicationDecorator, int entriesPerPage,
			DictionaryConfigurationModel configuration, IPropertyTable propertyTable,
			LcmCache cache,
			IRecordList activeRecordList,
			string xhtmlPath, IThreadedProgress progress = null)
		{
			var entryCount = entryHvos.Length;
			var cssPath = Path.ChangeExtension(xhtmlPath, "css");
			var configDir = Path.GetDirectoryName(configuration.FilePath);
			// Don't display letter headers if we're showing a preview in the Edit tool or we're not sorting by headword
			var wantLetterHeaders = (entryCount > 1 || !IsLexEditPreviewOnly(publicationDecorator)) && (IsRecordListSortingByHeadword(activeRecordList));
			using (var xhtmlWriter = XmlWriter.Create(xhtmlPath))
			using (var cssWriter = new StreamWriter(cssPath, false, Encoding.UTF8))
			{
				IReadonlyPropertyTable readOnlyPropertyTable = new ReadOnlyPropertyTable(propertyTable);
				var fwStyleSheet = FontHeightAdjuster.StyleSheetFromPropertyTable(readOnlyPropertyTable);
				var custCssPath = string.Empty;
				var projType = string.IsNullOrEmpty(configDir) ? null : new DirectoryInfo(configDir).Name;
				if (!string.IsNullOrEmpty(projType))
				{
					var cssName = projType == "Dictionary" ? "ProjectDictionaryOverrides.css" : "ProjectReversalOverrides.css";
					custCssPath = CopyCustomCssToTempFolder(configDir, xhtmlPath, cssName);
				}
				var settings = new GeneratorSettings(cache, readOnlyPropertyTable, true, true, Path.GetDirectoryName(xhtmlPath), IsNormalRtl(readOnlyPropertyTable), Path.GetFileName(cssPath) == "configured.css");
				GenerateOpeningHtml(cssPath, custCssPath, settings, xhtmlWriter);
				var currentPageBounds = GetPageForCurrentEntry(settings, entryHvos, entriesPerPage, activeRecordList);
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
						var entryContent = GenerateXHTMLForEntry(entry, configuration, publicationDecorator, settings);
						entryStringBuilder.Append(entryContent);
						if (progress != null)
						{
							progress.Position++;
						}
					});

					entryActions.Add(generateEntryAction);
				}
				if (progress != null)
				{
					progress.Message = xWorksStrings.ksGeneratingDisplayFragments;
				}
				// Generate all the document fragments (in parallel)
				SpawnEntryGenerationThreadsAndWait(entryActions, progress);
				// Generate the letter headers and insert the document fragments into the full xhtml file
				if (progress != null)
				{
					progress.Message = xWorksStrings.ksArrangingDisplayFragments;
				}
				foreach (var entryAndXhtml in entryContents)
				{
					if (wantLetterHeaders && !string.IsNullOrEmpty(entryAndXhtml.Item2.ToString()))
					{
						GenerateLetterHeaderIfNeeded(entryAndXhtml.Item1, ref lastHeader, xhtmlWriter, settings);
					}
					xhtmlWriter.WriteRaw(entryAndXhtml.Item2.ToString());
				}
				GenerateBottomOfPageButtonsIfNeeded(settings, entryHvos, entriesPerPage, currentPageBounds, xhtmlWriter);
				GenerateClosingHtml(xhtmlWriter);
				xhtmlWriter.Flush();

				if (progress != null)
				{
					progress.Message = xWorksStrings.ksGeneratingStyleInfo;
				}
				if (!IsLexEditPreviewOnly(publicationDecorator) && !IsExport(settings))
				{
					cssWriter.Write(CssGenerator.GenerateCssForSelectedEntry(settings.RightToLeft));
					CopyFileSafely(settings, Path.Combine(FwDirectoryFinder.FlexFolder, ImagesFolder, CurrentEntryMarker), CurrentEntryMarker);
				}
				cssWriter.Write(CssGenerator.GenerateCssFromConfiguration(configuration, cache, fwStyleSheet));
				cssWriter.Flush();
			}
		}

		private static bool IsLexEditPreviewOnly(DictionaryPublicationDecorator decorator)
		{
			return decorator == null;
		}

		private static bool IsExport(GeneratorSettings settings)
		{
			return !settings.ExportPath.StartsWith(Path.Combine(Path.GetTempPath(), "DictionaryPreview"));
		}

		/// <summary>
		/// Method to copy the custom Css file from Project folder to the Temp folder for Fieldworks preview
		/// </summary>
		private static string CopyCustomCssToTempFolder(string projectPath, string xhtmlPath, string custCssFileName)
		{
			if (xhtmlPath == null || projectPath == null)
				return string.Empty;
			var custCssProjectPath = Path.Combine(projectPath, custCssFileName);
			if (!File.Exists(custCssProjectPath))
				return string.Empty;
			var custCssTempPath = Path.Combine(Path.GetDirectoryName(xhtmlPath), custCssFileName);
			File.Copy(custCssProjectPath, custCssTempPath, true);
			return custCssTempPath;
		}

		private static void GenerateTopOfPageButtonsIfNeeded(GeneratorSettings settings, int[] entryHvos, int entriesPerPage, Tuple<int, int> currentPageBounds, XmlWriter xhtmlWriter, StreamWriter cssWriter)
		{
			var pageRanges = GetPageRanges(entryHvos, entriesPerPage);
			if (pageRanges.Count <= 1)
			{
				return;
			}
			GeneratePageButtons(settings, entryHvos, pageRanges, currentPageBounds, xhtmlWriter);
			cssWriter.Write(CssGenerator.GenerateCssForPageButtons());
		}

		private static void GenerateBottomOfPageButtonsIfNeeded(GeneratorSettings settings, int[] entryHvos, int entriesPerPage,
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
			string currentConfigPath, GeneratorSettings settings, Tuple<int, int> oldCurrentPageRange, Tuple<int, int> oldAdjacentPageRange,
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
					entries.Add(GenerateXHTMLForEntry(settings.Cache.ServiceLocator.ObjectRepository.GetObject(entryHvos[i]),
						currentConfig, publicationDecorator, settings));
				}
			}
			else
			{
				var lastEntry = Math.Min(oldAdjacentPageRange.Item2, oldCurrentPageRange.Item2 + entriesToAddCount);
				for (var i = oldCurrentPageRange.Item2 + 1; i <= lastEntry; ++i)
				{
					entries.Add(GenerateXHTMLForEntry(settings.Cache.ServiceLocator.ObjectRepository.GetObject(entryHvos[i]),
						currentConfig, publicationDecorator, settings));
				}
			}
			return entries;
		}

		internal static void GenerateAdjustedPageButtons(int[] entryHvos, GeneratorSettings settings, Tuple<int, int> currentPageRange, Tuple<int, int> adjacentPageRange,
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

		private static void GeneratePageButtons(GeneratorSettings settings, int[] entryHvos, List<Tuple<int, int>> pageRanges, Tuple<int, int> currentPageBounds, XmlWriter xhtmlWriter)
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

		private static void GeneratePageButton(GeneratorSettings settings, int[] entryHvos, List<Tuple<int, int>> pageRanges,
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
			xhtmlWriter.WriteString(GeneratePageButtonText(entryHvos[page.Item1], entryHvos[page.Item2], settings, page.Item1 == 0));
			xhtmlWriter.WriteEndElement();
		}

		private static string GeneratePageButtonText(int firstEntryId, int lastEntryId, GeneratorSettings settings, bool isFirst)
		{
			var firstEntry = settings.Cache.ServiceLocator.GetObject(firstEntryId);
			var lastEntry = settings.Cache.ServiceLocator.GetObject(lastEntryId);
			var firstLetters = GetIndexLettersOfHeadword(GetHeadwordForLetterHead(firstEntry), isFirst);
			var lastLetters = GetIndexLettersOfHeadword(GetHeadwordForLetterHead(lastEntry));
			return firstEntryId == lastEntryId ? firstLetters : firstLetters + " .. " + lastLetters;
		}

		/// <summary>
		/// Get the page for the current entry, represented by the range of entries on the page containing the current entry
		/// </summary>
		private static Tuple<int, int> GetPageForCurrentEntry(GeneratorSettings settings, int[] entryHvos, int entriesPerPage, IRecordList activeRecordList)
		{
			var currentEntryHvo = 0;
			if (activeRecordList != null)
			{
				currentEntryHvo = activeRecordList.CurrentObjectHvo;
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
				return string.Empty;
			return TsStringUtils.Compose(headWord.Substring(0, headWord.Length <= 1 || justFirstLetter ? 1 : 2));
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

		private static bool IsCanceling(IThreadedProgress progress)
		{
			return progress != null && progress.IsCanceling;
		}

		/// <summary>
		/// This method uses a ThreadPool to execute the given individualActions in parallel.
		/// It waits for all the individualActions to complete and then returns.
		/// </summary>
		private static void SpawnEntryGenerationThreadsAndWait(List<Action> individualActions, IThreadedProgress progress)
		{
			var actionCount = individualActions.Count;
			//Note that our COM classes all implement the STA threading model, while the ThreadPool always uses MTA model threads.
			//I don't understand why using the ThreadPool sometimes works, but not always.  Expliciting allocating STA model
			//threads as done here works in all the cases that have been tried.  (Windows/Linux, program/unit test)  Unfortunately,
			//the speedup on Linux is minimal.
			var maxThreadCount = Math.Min(16, (int)(Environment.ProcessorCount * 1.5));
			maxThreadCount = Math.Min(maxThreadCount, actionCount);
			Exception exceptionThrown = null;
			var threadActionArray = new Action[maxThreadCount];
			using (var countDown = new CountdownEvent(maxThreadCount))
			{
				// Note that the loop index variable startIndex cannot be used in an action defined as a closure.  So we have to define all the
				// possible closures explicitly to achieve the parallelism reliably.  (Remember your theoretical computer science lessons
				// about lambda expressions and the various ways that variables are bound.  For some of us, that's been over 40 years!)
				// ReSharper disable AccessToDisposedClosure Justification: threads are guaranteed to finish before countDown is disposed
				for (var startIndex = 0; startIndex < maxThreadCount; startIndex++)
				{
					// bind a copy of the current value of the loop index to the closure,
					// instead of depending on startIndex which will change
					var index = startIndex;
					threadActionArray[index] = () =>
					{
						try { for (var j = index; j < actionCount && !IsCanceling(progress); j += maxThreadCount) individualActions[j](); }
						catch (Exception e) { exceptionThrown = e; }
						finally { countDown.Signal(); }
					};
				}
				// ReSharper restore AccessToDisposedClosure
				var threads = new List<Thread>(maxThreadCount);
				for (var i = 0; i < maxThreadCount; ++i)
				{
					var x = new Thread(new ThreadStart(threadActionArray[i]));
					x.SetApartmentState(ApartmentState.STA);
					x.Start();
					threads.Add(x);		// ensure thread doesn't get garbage collected prematurely.
				}
				countDown.Wait();
				threads.Clear();
				// Throwing the exception out here avoids hanging up the Green screen AND the progress dialog.
				// The only downside is we see only one exception. See LT-17244.
				if (exceptionThrown != null)
					throw new WorkerThreadException("Exception generating Configured XHTML", exceptionThrown);
			}
		}

		internal static void GenerateLetterHeaderIfNeeded(ICmObject entry, ref string lastHeader, XmlWriter xhtmlWriter, GeneratorSettings settings)
		{
			// If performance is an issue these dummy's can be stored between calls
			var dummyOne = new Dictionary<string, ISet<string>>();
			var dummyTwo = new Dictionary<string, Dictionary<string, string>>();
			var dummyThree = new Dictionary<string, ISet<string>>();
			var cache = settings.Cache;
			var wsString = cache.WritingSystemFactory.GetStrFromWs(cache.DefaultVernWs);
			if (entry is IReversalIndexEntry)
				wsString = ((IReversalIndexEntry)entry).SortKeyWs;
			var firstLetter = ConfiguredExport.GetLeadChar(GetHeadwordForLetterHead(entry), wsString, dummyOne, dummyTwo, dummyThree,
				cache);
			if (firstLetter != lastHeader && !string.IsNullOrEmpty(firstLetter))
			{
				var headerTextBuilder = new StringBuilder();
				var upperCase = Icu.ToTitle(firstLetter, wsString);
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
		/// To generating the letter headings, we need to check the first character of the "headword," which is a different
		/// field for ILexEntry and IReversalIndexEntry. Get the headword starting from entry-type-agnostic.
		/// </summary>
		/// <returns>the "headword" in NFD (the heading letter must be normalized to NFC before writing to XHTML, per LT-18177)</returns>
		private static string GetHeadwordForLetterHead(ICmObject entry)
		{
			var lexEntry = entry as ILexEntry;
			if (lexEntry == null)
			{
				var revEntry = entry as IReversalIndexEntry;
				return revEntry != null ? revEntry.ReversalForm.BestAnalysisAlternative.Text.TrimStart() : string.Empty;
			}
			return lexEntry.HomographForm.TrimStart();
		}

		/// <summary>
		/// Generating the xhtml representation for the given ICmObject using the given configuration node to select which data to write out
		/// If it is a Dictionary Main Entry or non-Dictionary entry, uses the first configuration node.
		/// If it is a Minor Entry, first checks whether the entry should be published as a Minor Entry; then, generates XHTML for each applicable
		/// Minor Entry configuration node.
		/// </summary>
		public static string GenerateXHTMLForEntry(ICmObject entryObj, DictionaryConfigurationModel configuration,
			DictionaryPublicationDecorator publicationDecorator, GeneratorSettings settings)
		{
			if (IsMainEntry(entryObj, configuration))
				return GenerateXHTMLForMainEntry(entryObj, configuration.Parts[0], publicationDecorator, settings);

			var entry = (ILexEntry)entryObj;
			return entry.PublishAsMinorEntry
				? GenerateXHTMLForMinorEntry(entry, configuration, publicationDecorator, settings)
				: string.Empty;
		}

		public static string GenerateXHTMLForMainEntry(ICmObject entry, ConfigurableDictionaryNode configuration,
			DictionaryPublicationDecorator publicationDecorator, GeneratorSettings settings)
		{
			if (configuration.DictionaryNodeOptions != null && ((ILexEntry)entry).ComplexFormEntryRefs.Any() && !IsListItemSelectedForExport(configuration, entry))
				return string.Empty;
			return GenerateXHTMLForEntry(entry, configuration, publicationDecorator, settings);
		}

		private static string GenerateXHTMLForMinorEntry(ICmObject entry, DictionaryConfigurationModel configuration,
			DictionaryPublicationDecorator publicationDecorator, GeneratorSettings settings)
		{
			// LT-15232: show minor entries using only the first applicable Minor Entry node (not more than once)
			var applicablePart = configuration.Parts.Skip(1).LastOrDefault(part => IsListItemSelectedForExport(part, entry));
			return applicablePart == null ? string.Empty : GenerateXHTMLForEntry(entry, applicablePart, publicationDecorator, settings);
		}

		/// <summary>
		/// If entry is a a Main Entry
		/// For Root-based configs, this means the entry is neither a Variant nor a Complex Form.
		/// For Lexeme-based configs, Complex Forms are considered Main Entries but Variants are not.
		/// </summary>
		internal static bool IsMainEntry(ICmObject entry, DictionaryConfigurationModel config)
		{
			var lexEntry = entry as ILexEntry;
			if (lexEntry == null // only LexEntries can be Minor; others (ReversalIndex, etc) are always Main.
				|| !lexEntry.EntryRefsOS.Any()) // owning an ILexEntryRef denotes Complex Forms or Variants (not owning any denotes Main Entries)
				return true;
			if (config.IsRootBased) // Root-based configs consider all Complex Forms and Variants to be Minor Entries
				return false;
			// Lexeme-Based and Hybrid configs consider Complex Forms to be Main Entries (Variants are still Minor Entries)
			return lexEntry.EntryRefsOS.Any(ler => ler.RefType == LexEntryRefTags.krtComplexForm);
		}

		/// <summary>Generates XHTML for an ICmObject for a specific ConfigurableDictionaryNode</summary>
		/// <remarks>the configuration node must match the entry type</remarks>
		internal static string GenerateXHTMLForEntry(ICmObject entry, ConfigurableDictionaryNode configuration, DictionaryPublicationDecorator publicationDecorator, GeneratorSettings settings)
		{
			if (settings == null || entry == null || configuration == null)
			{
				throw new ArgumentNullException();
			}
			// ReSharper disable LocalizableElement, because seriously, who cares about localized exceptions?
			if (string.IsNullOrEmpty(configuration.FieldDescription))
			{
				throw new ArgumentException("Invalid configuration: FieldDescription can not be null", "configuration");
			}
			if (entry.ClassID != settings.Cache.MetaDataCacheAccessor.GetClassId(configuration.FieldDescription))
			{
				throw new ArgumentException("The given argument doesn't configure this type", "configuration");
			}
			// ReSharper restore LocalizableElement
			if (!configuration.IsEnabled)
			{
				return string.Empty;
			}

			var pieces = configuration.ReferencedOrDirectChildren
				.Select(config => GenerateXHTMLForFieldByReflection(entry, config, publicationDecorator, settings))
				.Where(content => !string.IsNullOrEmpty(content)).ToList();
			if (pieces.Count == 0)
				return string.Empty;
			var bldr = new StringBuilder();
			using (var xw = XmlWriter.Create(bldr, new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment }))
			{
				xw.WriteStartElement("div");
				WriteClassNameAttributeForConfig(xw, configuration);
				xw.WriteAttributeString("id", "g" + entry.Guid);
				pieces.ForEach(xw.WriteRaw);
				xw.WriteEndElement(); // </div>
				xw.Flush();
				return Icu.Normalize(bldr.ToString(), Icu.UNormalizationMode.UNORM_NFC); // All content should be in NFC (LT-18177)
			}
		}

		/// <summary>
		/// This method will write out the class name attribute into the xhtml for the given configuration node
		/// taking into account the current information in ClassNameOverrides
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="configNode">used to look up any mapping overrides</param>
		private static void WriteClassNameAttributeForConfig(XmlWriter writer, ConfigurableDictionaryNode configNode)
		{
			var classAtt = CssGenerator.GetClassAttributeForConfig(configNode);
			if (configNode.ReferencedNode != null)
			{
				classAtt = $"{classAtt} {CssGenerator.GetClassAttributeForConfig(configNode.ReferencedNode)}";
			}
			writer.WriteAttributeString("class", classAtt);
		}

		internal static string GenerateXHTMLForFieldByReflection(object field, ConfigurableDictionaryNode config, DictionaryPublicationDecorator publicationDecorator, GeneratorSettings settings)
		{
			return ReallyGenerateXHTMLForFieldByReflection(field, config, publicationDecorator, settings);
		}

		/// <summary>
		/// This method will use reflection to pull data out of the given object based on the given configuration and
		/// write out appropriate XHTML.
		/// </summary>
		/// <remarks>We use a significant amount of boilerplate code for fields and subfields. Make sure you update both.</remarks>
		private static string ReallyGenerateXHTMLForFieldByReflection(object field, ConfigurableDictionaryNode config, DictionaryPublicationDecorator publicationDecorator, GeneratorSettings settings, SenseInfo info = new SenseInfo(), bool fUseReverseSubField = false)
		{
			if (!config.IsEnabled)
			{
				return string.Empty;
			}
			var cache = settings.Cache;
			var entryType = field.GetType();
			object propertyValue = null;
			if (config.DictionaryNodeOptions is DictionaryNodeGroupingOptions)
			{
				return GenerateXHTMLForGroupingNode(field, config, publicationDecorator, settings);
			}
			if (config.IsCustomField && config.SubField == null)
			{
				var customFieldOwnerClassName = GetClassNameForCustomFieldParent(config, (IFwMetaDataCacheManaged)settings.Cache.MetaDataCacheAccessor);
				if (!GetPropValueForCustomField(field, config, cache, customFieldOwnerClassName, config.FieldDescription, ref propertyValue))
				{
					return string.Empty;
				}
			}
			else
			{
				var property = entryType.GetProperty(config.FieldDescription);
				if (property == null)
				{
#if DEBUG
					var msg = $"Issue with finding {config.FieldDescription} for {entryType}";
					ShowConfigDebugInfo(msg, config);
#endif
					return string.Empty;
				}
				propertyValue = property.GetValue(field, new object[] { });
				GetSortedReferencePropertyValue(config, ref propertyValue, field);
			}
			// If the property value is null there is nothing to generate
			if (propertyValue == null)
			{
				return string.Empty;
			}
			if (!string.IsNullOrEmpty(config.SubField))
			{
				if (config.IsCustomField)
				{
					// Get the custom field value (in SubField) using the property which came from the field object
					if (!GetPropValueForCustomField(propertyValue, config, cache, ((ICmObject)propertyValue).ClassName,
						config.SubField, ref propertyValue))
					{
						return string.Empty;
					}
				}
				else
				{
					var subType = propertyValue.GetType();
					var subField = fUseReverseSubField ? "Reverse" + config.SubField : config.SubField;
					var subProp = subType.GetProperty(subField);
					if (subProp == null)
					{
#if DEBUG
						var msg = $"Issue with finding (subField) {subField} for (subType) {subType}";
						ShowConfigDebugInfo(msg, config);
#endif
						return string.Empty;
					}
					propertyValue = subProp.GetValue(propertyValue, new object[] { });
					GetSortedReferencePropertyValue(config, ref propertyValue, field);
				}
				// If the property value is null there is nothing to generate
				if (propertyValue == null)
					return string.Empty;
			}
			ICmFile fileProperty;
			ICmObject fileOwner;
			var typeForNode = config.IsCustomField
										? GetPropertyTypeFromReflectedTypes(propertyValue.GetType(), null)
										: GetPropertyTypeForConfigurationNode(config, propertyValue.GetType(), (IFwMetaDataCacheManaged)cache.MetaDataCacheAccessor);
			switch (typeForNode)
			{
				case PropertyType.CollectionType:
					if (!IsCollectionEmpty(propertyValue))
						return GenerateXHTMLForCollection(propertyValue, config, publicationDecorator, field, settings, info);
					return string.Empty;

				case PropertyType.MoFormType:
					return GenerateXHTMLForMoForm(propertyValue as IMoForm, config, settings);

				case PropertyType.CmObjectType:
					return GenerateXHTMLForICmObject(propertyValue as ICmObject, config, settings);

				case PropertyType.CmPictureType:
					fileProperty = propertyValue as ICmFile;
					fileOwner = field as ICmObject;
					return fileProperty != null && fileOwner != null
						? GenerateXHTMLForPicture(fileProperty, config, fileOwner, settings)
						: GenerateXHTMLForPictureCaption(propertyValue, config, settings);

				case PropertyType.CmPossibility:
					return GenerateXHTMLForPossibility(propertyValue, config, publicationDecorator, settings);

				case PropertyType.CmFileType:
					fileProperty = propertyValue as ICmFile;
					string internalPath = null;
					if (fileProperty?.InternalPath != null)
					{
						internalPath = fileProperty.InternalPath;
					}
					// fileProperty.InternalPath can have a backward slash so that gets replaced with a forward slash in Linux
#if __MonoCS__
					if(!string.IsNullOrEmpty(internalPath))
						internalPath = fileProperty.InternalPath.Replace('\\', '/');
#endif
						if (fileProperty != null && !string.IsNullOrEmpty(internalPath))
					{
						var srcAttr = GenerateSrcAttributeForMediaFromFilePath(internalPath, "AudioVisual", settings);
						if (IsVideo(fileProperty.InternalPath))
							return GenerateXHTMLForVideoFile(fileProperty.ClassName, srcAttr, MovieCamera);
						fileOwner = field as ICmObject;
						if (fileOwner != null)
						{
							// the XHTML id attribute must be unique. The owning ICmMedia has a unique guid.
							// The ICmFile is used for all references to the same file within the project, so its guid is not unique.
							return GenerateXHTMLForAudioFile(fileProperty.ClassName, fileOwner.Guid.ToString(), srcAttr, LoudSpeaker, settings);
						}
					}
					return string.Empty;
			}
			var bldr = new StringBuilder(GenerateXHTMLForValue(field, propertyValue, config, settings));
			if (config.ReferencedOrDirectChildren != null)
			{
				foreach (var child in config.ReferencedOrDirectChildren)
				{
					bldr.Append(GenerateXHTMLForFieldByReflection(propertyValue, child, publicationDecorator, settings));
				}
			}
			return bldr.ToString();
		}

		private static string GenerateXHTMLForGroupingNode(object field, ConfigurableDictionaryNode config,
			DictionaryPublicationDecorator publicationDecorator, GeneratorSettings settings)
		{
			if (config.ReferencedOrDirectChildren != null && config.ReferencedOrDirectChildren.Any(child => child.IsEnabled))
			{
				var bldr = new StringBuilder();
				using (var xw = XmlWriter.Create(bldr, new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment }))
				{
					xw.WriteStartElement("span");
					xw.WriteAttributeString("class", CssGenerator.GetClassAttributeForConfig(config));

					var innerBuilder = new StringBuilder();
					foreach (var child in config.ReferencedOrDirectChildren)
					{
						innerBuilder.Append(GenerateXHTMLForFieldByReflection(field, child, publicationDecorator, settings));
					}
					var innerContents = innerBuilder.ToString();
					if (string.IsNullOrEmpty(innerContents))
						return string.Empty;
					xw.WriteRaw(innerContents);
					xw.WriteEndElement(); // </span>
					xw.Flush();
				}
				return bldr.ToString();
			}
			return string.Empty;
		}

		/// <summary>
		/// Gets the value of the requested custom field associated with the fieldOwner object
		/// </summary>
		/// <returns>true if the custom field was valid and false otherwise</returns>
		/// <remarks>propertyValue can be null if the custom field is valid but no value is stored for the owning object</remarks>
		private static bool GetPropValueForCustomField(object fieldOwner, ConfigurableDictionaryNode config,
			LcmCache cache, string customFieldOwnerClassName, string customFieldName, ref object propertyValue)
		{
			int customFieldFlid = GetCustomFieldFlid(config, (IFwMetaDataCacheManaged)cache.MetaDataCacheAccessor, customFieldOwnerClassName, customFieldName);
			if (customFieldFlid != 0)
			{
				var customFieldType = cache.MetaDataCacheAccessor.GetFieldType(customFieldFlid);
				ICmObject specificObject;
				if (fieldOwner is ISenseOrEntry)
				{
					specificObject = ((ISenseOrEntry)fieldOwner).Item;
					if (!((IFwMetaDataCacheManaged)cache.MetaDataCacheAccessor).GetFields(specificObject.ClassID,
						true, (int)CellarPropertyTypeFilter.All).Contains(customFieldFlid))
					{
						return false;
					}
				}
				else
				{
					specificObject = (ICmObject)fieldOwner;
				}

				switch (customFieldType)
				{
					case (int)CellarPropertyType.ReferenceCollection:
					case (int)CellarPropertyType.OwningCollection:
					// Collections are stored essentially the same as sequences.
					case (int)CellarPropertyType.ReferenceSequence:
					case (int)CellarPropertyType.OwningSequence:
					{
						var sda = cache.MainCacheAccessor;
						// This method returns the hvo of the object pointed to
						var chvo = sda.get_VecSize(specificObject.Hvo, customFieldFlid);
						int[] contents;
						using (var arrayPtr = MarshalEx.ArrayToNative<int>(chvo))
						{
							sda.VecProp(specificObject.Hvo, customFieldFlid, chvo, out chvo, arrayPtr);
							contents = MarshalEx.NativeToArray<int>(arrayPtr, chvo);
						}
						// if the hvo is invalid set propertyValue to null otherwise get the object
						propertyValue = contents.Select(id => cache.LangProject.Services.GetObject(id));
						break;
					}
					case (int)CellarPropertyType.ReferenceAtomic:
					case (int)CellarPropertyType.OwningAtomic:
					{
						// This method returns the hvo of the object pointed to
						propertyValue = cache.MainCacheAccessor.get_ObjectProp(specificObject.Hvo, customFieldFlid);
						// if the hvo is invalid set propertyValue to null otherwise get the object
							propertyValue = (int)propertyValue > 0 ? cache.LangProject.Services.GetObject((int)propertyValue) : null;
						break;
					}
					case (int)CellarPropertyType.GenDate:
					{
						propertyValue = new GenDate(cache.MainCacheAccessor.get_IntProp(specificObject.Hvo, customFieldFlid));
						break;
					}

					case (int)CellarPropertyType.Time:
					{
						propertyValue = SilTime.ConvertFromSilTime(cache.MainCacheAccessor.get_TimeProp(specificObject.Hvo, customFieldFlid));
						break;
					}
					case (int)CellarPropertyType.MultiUnicode:
					case (int)CellarPropertyType.MultiString:
					{
						propertyValue = cache.MainCacheAccessor.get_MultiStringProp(specificObject.Hvo, customFieldFlid);
						break;
					}
					case (int)CellarPropertyType.String:
					{
						propertyValue = cache.MainCacheAccessor.get_StringProp(specificObject.Hvo, customFieldFlid);
						break;
					}
					case (int)CellarPropertyType.Integer:
					{
						propertyValue = cache.MainCacheAccessor.get_IntProp(specificObject.Hvo, customFieldFlid);
						break;
					}
				}
			}
			return true;
		}

		private static string GenerateXHTMLForVideoFile(string className, string srcAttribute, string caption)
		{
			if (String.IsNullOrEmpty(srcAttribute) && String.IsNullOrEmpty(caption))
				return String.Empty;
			var bldr = new StringBuilder();
			using (var xw = XmlWriter.Create(bldr, new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment }))
			{
				// This creates a link that will open the video in the same window as the dictionary view/preview
				// refreshing will bring it back to the dictionary
				xw.WriteStartElement("a");
				xw.WriteAttributeString("class", className);
				xw.WriteAttributeString("href", srcAttribute);
				if (!String.IsNullOrEmpty(caption))
					xw.WriteString(caption);
				else
					xw.WriteRaw("");
				xw.WriteFullEndElement();
				xw.Flush();
				return bldr.ToString();
			}
		}

		private static bool IsVideo(string fileName)
		{
			var extension = Path.GetExtension(fileName);
			switch (extension.ToLowerInvariant())
			{
				// any others we should detect?
				case ".mp4":
				case ".avi":
				case ".swf":
				case ".mov":
				case ".flv":
				case ".ogv":
				case ".3gp":
					return true;
			}
			return false;
		}

#if DEBUG
		private static HashSet<ConfigurableDictionaryNode> s_reportedNodes = new HashSet<ConfigurableDictionaryNode>();

		private static void ShowConfigDebugInfo(string msg, ConfigurableDictionaryNode config)
		{
			lock (s_reportedNodes)
			{
				Debug.WriteLine(msg);
				if (s_reportedNodes.Contains(config))
					return;
				s_reportedNodes.Add(config);
				while (config != null)
				{
					Debug.WriteLine("    Label={0}, FieldDescription={1}, SubField={2}", config.Label, config.FieldDescription, config.SubField ?? "");
					config = config.Parent;
				}
			}
		}
#endif

		private static void GetSortedReferencePropertyValue(ConfigurableDictionaryNode config, ref object propertyValue, object parent)
		{
			var options = config.DictionaryNodeOptions as DictionaryNodeListOptions;
			var unsortedReferences = propertyValue as IEnumerable<ILexReference>;
			if (options == null || unsortedReferences == null || !unsortedReferences.Any())
				return;
			// Calculate and store the ids for each of the references once for efficiency.
			var refsAndIds = new List<Tuple<ILexReference, string>>();
			foreach (var reference in unsortedReferences)
			{
				var id = reference.OwnerType.Guid.ToString();
				if (LexRefTypeTags.IsAsymmetric((LexRefTypeTags.MappingTypes)reference.OwnerType.MappingType))
					id = id + LexRefDirection(reference, parent);
				refsAndIds.Add(new Tuple<ILexReference, string>(reference, id));
			}
			// LT-17384: LexReferences are not ordered (they are put in some order each time FLEx starts), but we want to have a consistent order each
			// time we export the dictionary (even after restarting FLEx), so we sort them here.
			// LT-15764 We're actually going to sort the ConfigTargets of the LexReference objects later, so what this really accomplishes
			// is sorting all the LexReferences of the same type together based on the DictionaryNodeListOptions.
			var sortedReferences = new List<ILexReference>();
			// REVIEW (Hasso) 2016.03: this Where is redundant to the IsListItemSelectedForExport call in GenerateCollectionItemContent
			// REVIEW (cont): Filtering here is more performant; the other filter can be removed if it is verifiably redundant.
			foreach (var option in options.Options.Where(optn => optn.IsEnabled))
			{
				foreach (var duple in refsAndIds)
				{
					if (option.Id == duple.Item2 && !sortedReferences.Contains(duple.Item1))
					{
						sortedReferences.Add(duple.Item1);
					}
				}
			}
			propertyValue = sortedReferences;
		}

		/// <summary/>
		/// <returns>Returns the flid of the custom field identified by the configuration nodes FieldDescription
		/// in the class identified by <code>customFieldOwnerClassName</code></returns>
		private static int GetCustomFieldFlid(ConfigurableDictionaryNode config, IFwMetaDataCacheManaged metaDataCacheAccessor, string customFieldOwnerClassName, string customFieldName = null)
		{
			var fieldName = customFieldName ?? config.FieldDescription;
			var customFieldFlid = 0;
			if (metaDataCacheAccessor.FieldExists(customFieldOwnerClassName, fieldName, false))
			{
				customFieldFlid = metaDataCacheAccessor.GetFieldId(customFieldOwnerClassName, fieldName, false);
			}
			else if (customFieldOwnerClassName == "SenseOrEntry")
			{
				// ENHANCE (Hasso) 2016.06: take pity on the poor user who has defined identically-named Custom Fields on both Sense and Entry
				if (metaDataCacheAccessor.FieldExists("LexSense", config.FieldDescription, false))
				{
					customFieldFlid = metaDataCacheAccessor.GetFieldId("LexSense", fieldName, false);
				}
				else if (metaDataCacheAccessor.FieldExists("LexEntry", config.FieldDescription, false))
				{
					customFieldFlid = metaDataCacheAccessor.GetFieldId("LexEntry", fieldName, false);
				}
			}
			return customFieldFlid;
		}

		/// <summary>
		/// This method will return the string representing the class name for the parent
		/// node of a configuration item representing a custom field.
		/// </summary>
		private static string GetClassNameForCustomFieldParent(ConfigurableDictionaryNode customFieldNode, IFwMetaDataCacheManaged metaDataCacheAccessor)
		{
			Type unneeded;
			// If the parent node of the custom field represents a collection, calling GetTypeForConfigurationNode
			// with the parent node returns the collection type. We want the type of the elements in the collection.
			var parentNodeType = GetTypeForConfigurationNode(customFieldNode.Parent, metaDataCacheAccessor, out unneeded);
			if (parentNodeType == null)
			{
				Debug.Assert(parentNodeType != null, "Unable to find type for configuration node");
				return string.Empty;
			}
			if (IsCollectionType(parentNodeType))
			{
				parentNodeType = parentNodeType.GetGenericArguments()[0];
			}
			if (parentNodeType.IsInterface)
			{
				// Strip off the interface designation since custom fields are added to concrete classes
				return parentNodeType.Name.Substring(1);
			}
			return parentNodeType.Name;
		}

		private static string GenerateXHTMLForPossibility(object propertyValue, ConfigurableDictionaryNode config,
			DictionaryPublicationDecorator publicationDecorator, GeneratorSettings settings)
		{
			if (config.ReferencedOrDirectChildren == null || !config.ReferencedOrDirectChildren.Any(node => node.IsEnabled))
				return string.Empty;
			var bldr = new StringBuilder();
			foreach (var child in config.ReferencedOrDirectChildren)
			{
				var content = GenerateXHTMLForFieldByReflection(propertyValue, child, publicationDecorator, settings);
				bldr.Append(content);
			}
			if (bldr.Length > 0)
				return WriteRawElementContents("span", bldr.ToString(), config);
			return string.Empty;
		}

		private static string GenerateXHTMLForPictureCaption(object propertyValue, ConfigurableDictionaryNode config, GeneratorSettings settings)
		{
			// todo: get sense numbers and captions into the same div and get rid of this if else
			string content;
			if (config.DictionaryNodeOptions != null)
				content = GenerateXHTMLForStrings(propertyValue as IMultiString, config, settings);
			else
				content = GenerateXHTMLForString(propertyValue as ITsString, config, settings);
			if (!String.IsNullOrEmpty(content))
				return WriteRawElementContents("div", content, config);
			return String.Empty;
		}

		private static string GenerateXHTMLForPicture(ICmFile pictureFile, ConfigurableDictionaryNode config, ICmObject owner,
			GeneratorSettings settings)
		{
			var srcAttribute = GenerateSrcAttributeFromFilePath(pictureFile, settings.UseRelativePaths ? "pictures" : null, settings);
			if (!String.IsNullOrEmpty(srcAttribute))
			{
				var bldr = new StringBuilder();
				using (var xw = XmlWriter.Create(bldr, new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment }))
				{
					xw.WriteStartElement("img");
					WriteClassNameAttributeForConfig(xw, config);
					xw.WriteAttributeString("src", srcAttribute);
					// the XHTML id attribute must be unique. The owning ICmPicture has a unique guid.
					// The ICmFile is used for all references to the same file within the project, so its guid is not unique.
					xw.WriteAttributeString("id", GetSafeXHTMLId(owner.Guid.ToString()));
					xw.WriteEndElement();
					xw.Flush();
					return bldr.ToString();
				}
			}
			return String.Empty;
		}

		/// <summary>
		/// This method will generate a src attribute which will point to the given file from the xhtml.
		/// </summary>
		/// <para name="subfolder">If not null the path generated will be a relative path with the file in subfolder</para>
		private static string GenerateSrcAttributeFromFilePath(ICmFile file, string subFolder, GeneratorSettings settings)
		{
			string filePath;
			if (settings.UseRelativePaths && subFolder != null && file.InternalPath != null)
			{
				filePath = Path.Combine(subFolder, Path.GetFileName(MakeSafeFilePath(file.InternalPath)));
				if (settings.CopyFiles)
				{
					filePath = CopyFileSafely(settings, MakeSafeFilePath(file.AbsoluteInternalPath), filePath);
				}
			}
			else
			{
				filePath = MakeSafeFilePath(file.AbsoluteInternalPath);
			}
			return settings.UseRelativePaths ? filePath : new Uri(filePath).ToString();
		}

		private static string GenerateSrcAttributeForMediaFromFilePath(string filename, string subFolder, GeneratorSettings settings)
		{
			string filePath;
			var linkedFilesRootDir = settings.Cache.LangProject.LinkedFilesRootDir;
			var audioVisualFile = Path.GetDirectoryName(filename) == subFolder ?
				Path.Combine(linkedFilesRootDir, filename) :
				Path.Combine(linkedFilesRootDir, subFolder, filename);
			if (settings.UseRelativePaths && subFolder != null)
			{
				filePath = Path.Combine(subFolder, Path.GetFileName(MakeSafeFilePath(filename)));
				if (settings.CopyFiles)
				{
					filePath = CopyFileSafely(settings, MakeSafeFilePath(audioVisualFile), filePath);
				}
			}
			else
			{
				filePath = MakeSafeFilePath(audioVisualFile);
			}
			return settings.UseRelativePaths ? filePath : new Uri(filePath).ToString();
		}

		private static string CopyFileSafely(GeneratorSettings settings, string source, string relativeDestination)
		{
			if (!File.Exists(source))
				return relativeDestination;
			bool isWavExport = settings.IsWebExport && Path.GetExtension(relativeDestination).Equals(".wav");
			if (isWavExport)
				relativeDestination = Path.ChangeExtension(relativeDestination, ".mp3");
			var destination = Path.Combine(settings.ExportPath, relativeDestination);
			var subFolder = Path.GetDirectoryName(relativeDestination);
			FileUtils.EnsureDirectoryExists(Path.GetDirectoryName(destination));
			// If an audio file is referenced by multiple entries they could end up in separate threads.
			// Locking on the PropertyTable seems safe since it will be the same PropertyTable for each thread.
			lock (settings.ReadOnlyPropertyTable)
			{
				if (!File.Exists(destination))
				{
					// converts audio files to correct format during Webonary export
					if (isWavExport)
					{
						WavConverter.WavToMp3(source, destination);
					}
					else
					{
						FileUtils.Copy(source, destination);
					}
				}
				else if (!AreFilesIdentical(source, destination, isWavExport))
				{
					var fileWithoutExtension = Path.GetFileNameWithoutExtension(relativeDestination);
					var fileExtension = Path.GetExtension(relativeDestination);
					var copyNumber = 0;
					string newFileName;
					do
					{
						++copyNumber;
						newFileName = string.Format("{0}{1}{2}", fileWithoutExtension, copyNumber, fileExtension);
						destination = string.IsNullOrEmpty(subFolder)
							? Path.Combine(settings.ExportPath, newFileName)
							: Path.Combine(settings.ExportPath, subFolder, newFileName);
					} while (File.Exists(destination) && !AreFilesIdentical(source, destination, isWavExport));
					// converts audio files to correct format if necessary during Webonary export
					if (!isWavExport)
						FileUtils.Copy(source, destination);
					else
						WavConverter.WavToMp3(source, destination);
					// Change the filepath to point to the copied file
					relativeDestination = string.IsNullOrEmpty(subFolder) ? newFileName : Path.Combine(subFolder, newFileName);
				}
			}
			return relativeDestination;
		}

		private static bool AreFilesIdentical(string source, string destination, bool isWavExport)
		{
			if (!isWavExport)
				return FileUtils.AreFilesIdentical(source, destination);
			SaveFile exists = WavConverter.AlreadyExists(source, destination);
			if (exists == SaveFile.IdenticalExists)
				return true;
			return false;
		}

		private static string MakeSafeFilePath(string filePath)
		{
			if (Unicode.CheckForNonAsciiCharacters(filePath))
			{
				// Flex keeps the filename as NFD in memory because it is unicode. We need NFC to actually link to the file
				filePath = Icu.Normalize(filePath, Icu.UNormalizationMode.UNORM_NFC);
			}
			if (!FileUtils.IsFilePathValid(filePath))
			{
				return "__INVALID_FILE_NAME__";
			}
			return filePath;
		}

		private static Dictionary<ConfigurableDictionaryNode, PropertyType> _configNodeToTypeMap = new Dictionary<ConfigurableDictionaryNode, PropertyType>();

		/// <summary>
		/// Get the property type for a configuration node.  There is no other data available but the node itself.
		/// </summary>
		internal static PropertyType GetPropertyTypeForConfigurationNode(ConfigurableDictionaryNode config)
		{
			return GetPropertyTypeForConfigurationNode(config, null, null);
		}

		/// <summary>
		/// Get the property type for a configuration node, using a cache to help out if necessary.
		/// </summary>
		internal static PropertyType GetPropertyTypeForConfigurationNode(ConfigurableDictionaryNode config, IFwMetaDataCacheManaged metaDataCacheAccessor)
		{
			return GetPropertyTypeForConfigurationNode(config, null, metaDataCacheAccessor);
		}

		/// <summary>
		/// This method will reflectively return the type that represents the given configuration node as
		/// described by the ancestry and FieldDescription and SubField properties of each node in it.
		/// </summary>
		/// <returns></returns>
		internal static PropertyType GetPropertyTypeForConfigurationNode(ConfigurableDictionaryNode config, Type fieldTypeFromData, IFwMetaDataCacheManaged metaDataCacheAccessor)
		{
			Type parentType;
			var fieldType = GetTypeForConfigurationNode(config, metaDataCacheAccessor, out parentType) ?? fieldTypeFromData;
			return GetPropertyTypeFromReflectedTypes(fieldType, parentType);
		}

		private static PropertyType GetPropertyTypeFromReflectedTypes(Type fieldType, Type parentType)
		{
			if (fieldType == null)
			{
				return PropertyType.InvalidProperty;
			}
			if (typeof(IStText).IsAssignableFrom(fieldType))
			{
				return PropertyType.PrimitiveType;
			}
			if (IsCollectionType(fieldType))
			{
				return PropertyType.CollectionType;
			}
			if (typeof(ICmPicture).IsAssignableFrom(parentType) && typeof(ICmFile).IsAssignableFrom(fieldType))
			{
				return PropertyType.CmPictureType;
			}
			if (typeof(ICmFile).IsAssignableFrom(fieldType))
			{
				return PropertyType.CmFileType;
			}
			if (typeof(IMoForm).IsAssignableFrom(fieldType))
			{
				return PropertyType.MoFormType;
			}
			if (typeof(ICmPossibility).IsAssignableFrom(fieldType))
			{
				return PropertyType.CmPossibility;
			}
			if (typeof(ICmObject).IsAssignableFrom(fieldType))
			{
				return PropertyType.CmObjectType;
			}
			return PropertyType.PrimitiveType;
		}

		/// <summary>
		/// This method will return the Type that represents the data in the given configuration node.
		/// </summary>
		/// <param name="config">This node and it's lineage will be used to find the type</param>
		/// <param name="metaDataCacheAccessor">Used when dealing with custom field nodes</param>
		/// <param name="parentType">This will be set to the type of the parent of config which is sometimes useful to the callers</param>
		/// <returns></returns>
		internal static Type GetTypeForConfigurationNode(ConfigurableDictionaryNode config, IFwMetaDataCacheManaged metaDataCacheAccessor, out Type parentType)
		{
			if (config == null)
			{
				throw new ArgumentNullException(nameof(config), @"The configuration node must not be null.");
			}

			parentType = null;
			var lineage = new Stack<ConfigurableDictionaryNode>();
			// Build a list of the direct line up to the top of the configuration
			lineage.Push(config);
			var next = config;
			while (next.Parent != null)
			{
				next = next.Parent;
				// Grouping nodes are skipped because they do not represent properties of the model and break type finding
				if (!(next.DictionaryNodeOptions is DictionaryNodeGroupingOptions))
					lineage.Push(next);
			}
			// pop off the root configuration and read the FieldDescription property to get our starting point
			var assembly = GetAssemblyForFile(AssemblyFile);
			var rootNode = lineage.Pop();
			var lookupType = assembly.GetType(rootNode.FieldDescription);
			if (lookupType == null) // If the FieldDescription didn't load prepend the default model namespace and try again
			{
				lookupType = assembly.GetType("SIL.LCModel.DomainImpl." + rootNode.FieldDescription);
			}
			if (lookupType == null)
			{
				throw new ArgumentException(string.Format(xWorksStrings.InvalidRootConfigurationNode, rootNode.FieldDescription));
			}
			var fieldType = lookupType;

			// Traverse the configuration reflectively inspecting the types in parent to child order
			foreach (var node in lineage)
			{
				if (node.IsCustomField)
				{
					fieldType = GetCustomFieldType(lookupType, node, metaDataCacheAccessor);
				}
				else
				{
					var property = GetProperty(lookupType, node);
					if (property != null)
					{
						fieldType = property.PropertyType;
					}
					else
					{
						return null;
					}
					if (IsCollectionType(fieldType))
					{
						// When a node points to a collection all the child nodes operate on individual items in the
						// collection, so look them up in the type that the collection contains. e.g. IEnumerable<ILexEntry>
						// gives ILexEntry and IFdoVector<ICmObject> gives ICmObject
						lookupType = fieldType.GetGenericArguments()[0];
					}
					else
					{
						parentType = lookupType;
						lookupType = fieldType;
					}
				}
			}
			return fieldType;
		}

		private static Type GetCustomFieldType(Type lookupType, ConfigurableDictionaryNode config, IFwMetaDataCacheManaged metaDataCacheAccessor)
		{
			// FDO doesn't work with interfaces, just concrete classes so chop the I off any interface types
			var customFieldOwnerClassName = lookupType.Name.TrimStart('I');
			var customFieldFlid = GetCustomFieldFlid(config, metaDataCacheAccessor, customFieldOwnerClassName);
			if (customFieldFlid != 0)
			{
				var customFieldType = metaDataCacheAccessor.GetFieldType(customFieldFlid);
				switch (customFieldType)
				{
					case (int)CellarPropertyType.ReferenceSequence:
					case (int)CellarPropertyType.OwningSequence:
					case (int)CellarPropertyType.ReferenceCollection:
						{
							return typeof(ILcmVector);
						}
					case (int)CellarPropertyType.ReferenceAtomic:
					case (int)CellarPropertyType.OwningAtomic:
					{
						var destClassId = metaDataCacheAccessor.GetDstClsId(customFieldFlid);
						if (destClassId == StTextTags.kClassId)
						{
								return typeof(IStText);
						}
						return typeof(ICmObject);
					}
					case (int)CellarPropertyType.Time:
						{
							return typeof(DateTime);
						}
					case (int)CellarPropertyType.MultiUnicode:
						{
							return typeof(IMultiUnicode);
						}
					case (int)CellarPropertyType.MultiString:
						{
							return typeof(IMultiString);
						}
					case (int)CellarPropertyType.String:
						{
							return typeof(string);
						}
					default:
						return null;
				}
			}
			return null;
		}

		/// <summary>
		/// Loading an assembly is expensive so we cache the assembly once it has been loaded
		/// for enahanced performance.
		/// </summary>
		private static Assembly GetAssemblyForFile(string assemblyFile)
		{
			if (!AssemblyMap.ContainsKey(assemblyFile))
			{
				AssemblyMap[assemblyFile] = Assembly.Load(AssemblyFile);
			}
			return AssemblyMap[assemblyFile];
		}

		/// <summary>
		/// Return the property info from a given class and node. Will check interface heirarchy for the property
		/// if <code>lookupType</code> is an interface.
		/// </summary>
		/// <param name="lookupType"></param>
		/// <param name="node"></param>
		/// <returns></returns>
		private static PropertyInfo GetProperty(Type lookupType, ConfigurableDictionaryNode node)
		{
			string propertyOfInterest;
			PropertyInfo propInfo;
			var typesToCheck = new Stack<Type>();
			typesToCheck.Push(lookupType);
			do
			{
				var current = typesToCheck.Pop();
				propertyOfInterest = node.FieldDescription;
				// if there is a SubField we need to use the type of the FieldDescription
				// for the rest of this method so set current to the FieldDescription type.
				if (node.SubField != null)
				{
					var property = current.GetProperty(node.FieldDescription);
					propertyOfInterest = node.SubField;
					if (property != null)
					{
						current = property.PropertyType;
					}
				}
				propInfo = current.GetProperty(propertyOfInterest, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (propInfo == null)
				{
					foreach (var i in current.GetInterfaces())
					{
						typesToCheck.Push(i);
					}
				}
			} while (propInfo == null && typesToCheck.Count > 0);
			return propInfo;
		}

		private static string GenerateXHTMLForMoForm(IMoForm moForm, ConfigurableDictionaryNode config, GeneratorSettings settings)
		{
			// Don't export if there is no such data
			if (moForm == null)
				return string.Empty;
			if (config.ReferencedOrDirectChildren != null && config.ReferencedOrDirectChildren.Any())
			{
				throw new NotImplementedException("Children for MoForm types not yet supported.");
			}
			return GenerateXHTMLForStrings(moForm.Form, config, settings, moForm.Owner.Guid);
		}

		/// <summary>
		/// This method will generate the XHTML that represents a collection and its contents
		/// </summary>
		private static string GenerateXHTMLForCollection(object collectionField, ConfigurableDictionaryNode config,
			DictionaryPublicationDecorator pubDecorator, object collectionOwner, GeneratorSettings settings, SenseInfo info = new SenseInfo())
		{
			var bldr = new StringBuilder();
			IEnumerable collection;
			if (collectionField is IEnumerable)
			{
				collection = (IEnumerable)collectionField;
			}
			else if (collectionField is ILcmVector)
			{
				collection = ((ILcmVector)collectionField).Objects;
			}
			else
			{
				throw new ArgumentException("The given field is not a recognized collection");
			}
			var cmOwner = collectionOwner as ICmObject ?? ((ISenseOrEntry)collectionOwner).Item;

			if (config.DictionaryNodeOptions is DictionaryNodeSenseOptions)
			{
				bldr.Append(GenerateXHTMLForSenses(config, pubDecorator, settings, collection, info));
			}
			else
			{
				FilterAndSortCollectionIfNeeded(ref collection, pubDecorator, config.SubField ?? config.FieldDescription);
				ConfigurableDictionaryNode lexEntryTypeNode;
				if (IsVariantEntryType(config, out lexEntryTypeNode))
				{
					bldr.Append(GenerateXHTMLForILexEntryRefCollection(config, collection, cmOwner, pubDecorator, settings, lexEntryTypeNode, false));
				}
				else if (IsComplexEntryType(config, out lexEntryTypeNode))
				{
					bldr.Append(GenerateXHTMLForILexEntryRefCollection(config, collection, cmOwner, pubDecorator, settings, lexEntryTypeNode, true));
				}
				else if (IsPrimaryEntryReference(config, out lexEntryTypeNode))
				{
					// Order by guid (to order things consistently; see LT-17384).
					// Though perhaps another sort key would be better, such as ICmObject.SortKey or ICmObject.SortKey2.
					var lerCollection = collection.Cast<ILexEntryRef>().OrderBy(ler => ler.Guid).ToList();
					// Group by Type only if Type is selected for output.
					if (lexEntryTypeNode.IsEnabled && lexEntryTypeNode.ReferencedOrDirectChildren != null
						&& lexEntryTypeNode.ReferencedOrDirectChildren.Any(y => y.IsEnabled))
					{
						Debug.Assert(config.DictionaryNodeOptions == null,
							"double calls to GenerateXHTMLForILexEntryRefsByType don't play nicely with ListOptions. Everything will be generated twice (if it doesn't crash)");
						// Display typeless refs
						foreach (var entry in lerCollection.Where(item => !item.ComplexEntryTypesRS.Any() && !item.VariantEntryTypesRS.Any()))
							bldr.Append(GenerateCollectionItemContent(config, pubDecorator, entry, collectionOwner, settings, lexEntryTypeNode));
						// Display refs of each type
						GenerateXHTMLForILexEntryRefsByType(config, lerCollection, collectionOwner, pubDecorator, settings, bldr, lexEntryTypeNode,
							true); // complex
						GenerateXHTMLForILexEntryRefsByType(config, lerCollection, collectionOwner, pubDecorator, settings, bldr, lexEntryTypeNode,
							false); // variants
					}
					else
					{
						Debug.WriteLine("Unable to group " + config.FieldDescription + " by LexRefType; generating sequentially");
						foreach (var item in lerCollection)
							bldr.Append(GenerateCollectionItemContent(config, pubDecorator, item, collectionOwner, settings));
					}
				}
				else if (config.FieldDescription.StartsWith("Subentries"))
				{
					GenerateXHTMLForSubentries(config, collection, cmOwner, pubDecorator, settings, bldr);
				}
				else if (IsLexReferenceCollection(config))
				{
					GenerateXHTMLForILexReferenceCollection(config, collection.Cast<ILexReference>(), cmOwner, pubDecorator, settings, bldr);
				}
				else
				{
					foreach (var item in collection)
						bldr.Append(GenerateCollectionItemContent(config, pubDecorator, item, collectionOwner, settings));
				}
			}
			if (bldr.Length > 0)
				return WriteRawElementContents("span", bldr.ToString(), config);
			return string.Empty;
		}

		private static bool IsLexReferenceCollection(ConfigurableDictionaryNode config)
		{
			var opt = config.DictionaryNodeOptions as DictionaryNodeListOptions;
			return opt != null && (opt.ListId == DictionaryNodeListOptions.ListIds.Entry ||
				opt.ListId == DictionaryNodeListOptions.ListIds.Sense);
		}

		internal static bool IsFactoredReference(ConfigurableDictionaryNode node, out ConfigurableDictionaryNode typeChild)
		{
			var paraOptions = node.DictionaryNodeOptions as IParaOption;
			if (paraOptions != null && paraOptions.DisplayEachInAParagraph)
			{
				typeChild = null;
				return false;
			}
			return IsVariantEntryType(node, out typeChild) || IsComplexEntryType(node, out typeChild) || IsPrimaryEntryReference(node, out typeChild);
		}

		/// <summary>
		/// Whether the selected node represents Complex Entries.
		/// This does *not* include Subentries, because Subentries are (a) never factored and (b) ILexEntries instead of ILexEntryRefs.
		/// </summary>
		private static bool IsComplexEntryType(ConfigurableDictionaryNode config, out ConfigurableDictionaryNode complexEntryTypeNode)
		{
			complexEntryTypeNode = null;
			// REVIEW (Hasso)2017.01: better to check ListId==Complex && !FieldDesc.StartsWith("Subentries")?
			if ((config.FieldDescription == "VisibleComplexFormBackRefs" || config.FieldDescription == "ComplexFormsNotSubentries")
				&& config.ReferencedOrDirectChildren != null)
			{
				complexEntryTypeNode = config.ReferencedOrDirectChildren.FirstOrDefault(child => child.FieldDescription == "ComplexEntryTypesRS");
				return complexEntryTypeNode != null;
			}
			return false;
		}

		private static bool IsVariantEntryType(ConfigurableDictionaryNode config, out ConfigurableDictionaryNode variantEntryTypeNode)
		{
			variantEntryTypeNode = null;
			var variantOptions = config.DictionaryNodeOptions as DictionaryNodeListOptions;
			if (variantOptions != null && variantOptions.ListId == DictionaryNodeListOptions.ListIds.Variant)
			{
				variantEntryTypeNode = config.ReferencedOrDirectChildren.FirstOrDefault(x => x.FieldDescription == "VariantEntryTypesRS");
				return variantEntryTypeNode != null;
			}
			return false;
		}

		private static bool IsPrimaryEntryReference(ConfigurableDictionaryNode config, out ConfigurableDictionaryNode entryTypesNode)
		{
			entryTypesNode = null;
			if (config.FieldDescription == "MainEntryRefs" || config.FieldDescription == "EntryRefsWithThisMainSense")
			{
				entryTypesNode = config.ReferencedOrDirectChildren.FirstOrDefault(n => n.FieldDescription == "EntryTypes");
				return entryTypesNode != null;
			}
			return false;
		}

		private static string GenerateXHTMLForILexEntryRefCollection(ConfigurableDictionaryNode config, IEnumerable collection, ICmObject collectionOwner,
			DictionaryPublicationDecorator pubDecorator, GeneratorSettings settings, ConfigurableDictionaryNode typeNode, bool isComplex)
		{
			var bldr = new StringBuilder();

			var lerCollection = collection.Cast<ILexEntryRef>().ToList();
			// ComplexFormsNotSubentries is a filtered version of VisibleComplexFormBackRefs, so it doesn't have it's own VirtualOrdering.
			var fieldForVO = config.FieldDescription == "ComplexFormsNotSubentries" ? "VisibleComplexFormBackRefs" : config.FieldDescription;
			if (lerCollection.Count > 1 && !VirtualOrderingServices.HasVirtualOrdering(collectionOwner, fieldForVO))
			{
				// Order things (LT-17384) alphabetically (LT-17762) if and only if the user hasn't specified an order (LT-17918).
				var wsId = settings.Cache.ServiceLocator.WritingSystemManager.Get(lerCollection.First().SortKeyWs);
				var comparer = new WritingSystemComparer(wsId);
				lerCollection.Sort((left, right) => comparer.Compare(left.SortKey, right.SortKey));
			}

			// Group by Type only if Type is selected for output.
			if (typeNode.IsEnabled && typeNode.ReferencedOrDirectChildren != null && typeNode.ReferencedOrDirectChildren.Any(y => y.IsEnabled))
			{
				// Display typeless refs
				foreach (var entry in lerCollection.Where(item => !item.ComplexEntryTypesRS.Any() && !item.VariantEntryTypesRS.Any()))
					bldr.Append(GenerateCollectionItemContent(config, pubDecorator, entry, collectionOwner, settings, typeNode));
				// Display refs of each type
				GenerateXHTMLForILexEntryRefsByType(config, lerCollection, collectionOwner, pubDecorator, settings, bldr, typeNode, isComplex);
			}
			else
			{
				Debug.WriteLine("Unable to group " + config.FieldDescription + " by LexRefType; generating sequentially");
				foreach (var item in lerCollection)
					bldr.Append(GenerateCollectionItemContent(config, pubDecorator, item, collectionOwner, settings));
			}
			return bldr.ToString();
		}

		private static void GenerateXHTMLForILexEntryRefsByType(ConfigurableDictionaryNode config, List<ILexEntryRef> lerCollection, object collectionOwner, DictionaryPublicationDecorator pubDecorator,
			GeneratorSettings settings, StringBuilder bldr, ConfigurableDictionaryNode typeNode, bool isComplex)
		{
			var lexEntryTypes = isComplex
				? settings.Cache.LangProject.LexDbOA.ComplexEntryTypesOA.ReallyReallyAllPossibilities
				: settings.Cache.LangProject.LexDbOA.VariantEntryTypesOA.ReallyReallyAllPossibilities;
			// Order the types by their order in their list in the configuration options, if any (LT-18018).
			var listOptions = config.DictionaryNodeOptions as DictionaryNodeListOptions;
			var lexEntryTypesFiltered = listOptions == null
				? lexEntryTypes.Select(t => t.Guid)
				: listOptions.Options.Where(o => o.IsEnabled).Select(o => new Guid(o.Id));
			// Don't factor out Types when displaying in a paragraph
			var paraOptions = config.DictionaryNodeOptions as IParaOption;
			if (paraOptions != null && paraOptions.DisplayEachInAParagraph)
				typeNode = null;
			// Generate XHTML by Type
			foreach (var typeGuid in lexEntryTypesFiltered)
			{
				var innerBldr = new StringBuilder();
				foreach (var lexEntRef in lerCollection)
				{
					if (isComplex ? lexEntRef.ComplexEntryTypesRS.Any(t => t.Guid == typeGuid) : lexEntRef.VariantEntryTypesRS.Any(t => t.Guid == typeGuid))
					{
						innerBldr.Append(GenerateCollectionItemContent(config, pubDecorator, lexEntRef, collectionOwner, settings, typeNode));
					}
				}
				// Display the Type iff there were refs of this Type (and we are factoring)
				if (innerBldr.Length > 0 && typeNode != null)
				{
					var lexEntryType = lexEntryTypes.First(t => t.Guid.Equals(typeGuid));
					bldr.Append(WriteRawElementContents("span",
						GenerateCollectionItemContent(typeNode, pubDecorator, lexEntryType,
							lexEntryType.Owner, settings), typeNode));
				}
				bldr.Append(innerBldr);
			}
		}

		private static void GenerateXHTMLForSubentries(ConfigurableDictionaryNode config, IEnumerable collection, ICmObject collectionOwner,
			DictionaryPublicationDecorator pubDecorator, GeneratorSettings settings, StringBuilder bldr)
		{
			var listOptions = config.DictionaryNodeOptions as DictionaryNodeListOptions;
			var typeNode = config.ReferencedOrDirectChildren.FirstOrDefault(n => n.FieldDescription == LookupComplexEntryType);
			if (listOptions != null && typeNode != null && typeNode.IsEnabled
				&& typeNode.ReferencedOrDirectChildren != null && typeNode.ReferencedOrDirectChildren.Any(n => n.IsEnabled))
			{
				// Get a list of Subentries including their relevant ILexEntryRefs. We will remove each Subentry from the list as it is
				// generated to prevent multiple generations on the odd chance that a Subentry has multiple Complex Form Types
				var subentries = collection.Cast<ILexEntry>()
					.Select(le => new Tuple<ILexEntryRef, ILexEntry>(EntryRefForSubentry(le, collectionOwner), le)).ToList();

				// Generate any Subentries with no ComplexFormType
				for (var i = 0; i < subentries.Count; i++)
				{
					if (subentries[i].Item1 == null || !subentries[i].Item1.ComplexEntryTypesRS.Any())
					{
						bldr.Append(GenerateCollectionItemContent(config, pubDecorator, subentries[i].Item2, collectionOwner, settings));
						subentries.RemoveAt(i--);
					}
				}
				// Generate Subentries by ComplexFormType
				foreach (var typeGuid in listOptions.Options.Where(o => o.IsEnabled).Select(o => new Guid(o.Id)))
				{
					for (var i = 0; i < subentries.Count; i++)
					{
						if (subentries[i].Item1.ComplexEntryTypesRS.Any(t => t.Guid == typeGuid))
						{
							bldr.Append(GenerateCollectionItemContent(config, pubDecorator, subentries[i].Item2, collectionOwner, settings));
							subentries.RemoveAt(i--);
						}
					}
				}
			}
			else
			{
				Debug.WriteLine("Unable to group " + config.FieldDescription + " by LexRefType; generating sequentially");
				foreach (var item in collection)
					bldr.Append(GenerateCollectionItemContent(config, pubDecorator, item, collectionOwner, settings));
			}
		}

		/// <summary>
		/// Don't show examples or subentries that have been marked to exclude from publication.
		/// See https://jira.sil.org/browse/LT-15697 and https://jira.sil.org/browse/LT-16775.
		/// Consistently sort indeterminately-ordered collections. See https://jira.sil.org/browse/LT-17384
		/// </summary>
		private static void FilterAndSortCollectionIfNeeded(ref IEnumerable collection, DictionaryPublicationDecorator decorator, string fieldDescr)
		{
			if (collection is IEnumerable<ICmObject>)
			{
				var cmCollection = collection.Cast<ICmObject>();
				if (decorator != null)
					cmCollection = cmCollection.Where(item => !decorator.IsExcludedObject(item));
				if (IsCollectionInNeedOfSorting(fieldDescr))
					cmCollection = cmCollection.OrderBy(x => x.SortKey2);
				collection = cmCollection;
			}
			else if (collection is IEnumerable<ISenseOrEntry>)
			{
				var seCollection = collection.Cast<ISenseOrEntry>();
				if (decorator != null)
					seCollection = seCollection.Where(item => !decorator.IsExcludedObject(item.Item));
				if (IsCollectionInNeedOfSorting(fieldDescr))
					seCollection = seCollection.OrderBy(x => x.Item.SortKey2);
				collection = seCollection;
			}
		}

		/// <remarks>Variants and Complex Forms may also need sorting, but it is more efficient to check for them elsewhere</remarks>
		private static bool IsCollectionInNeedOfSorting(string fieldDescr)
		{
			// REVIEW (Hasso) 2016.09: should we check the CellarPropertyType?
			return fieldDescr.EndsWith("RC") || fieldDescr.EndsWith("OC"); // Reference Collection, Owning Collection (vs. Sequence)
		}

		/// <summary>
		/// This method will generate the XHTML that represents a senses collection and its contents
		/// </summary>
		private static string GenerateXHTMLForSenses(ConfigurableDictionaryNode config, DictionaryPublicationDecorator publicationDecorator,
			GeneratorSettings settings, IEnumerable senseCollection, SenseInfo info)
		{
			// Check whether all the senses have been excluded from publication.  See https://jira.sil.org/browse/LT-15697.
			var filteredSenseCollection = new List<ILexSense>();
			foreach (ILexSense item in senseCollection)
			{
				Debug.Assert(item != null);
				if (publicationDecorator != null && publicationDecorator.IsExcludedObject(item))
					continue;
				filteredSenseCollection.Add(item);
			}
			if (filteredSenseCollection.Count == 0)
				return string.Empty;
			var bldr = new StringBuilder();
			var isSubsense = config.Parent != null && config.FieldDescription == config.Parent.FieldDescription;
			string lastGrammaticalInfo, langId;
			var isSameGrammaticalInfo = IsAllGramInfoTheSame(config, filteredSenseCollection, isSubsense, out lastGrammaticalInfo, out langId);
			if (isSameGrammaticalInfo && !isSubsense)
			{
				bldr.Append(InsertGramInfoBeforeSenses(filteredSenseCollection.First(),
					config.ReferencedOrDirectChildren.FirstOrDefault(e => e.FieldDescription == "MorphoSyntaxAnalysisRA" && e.IsEnabled),
					publicationDecorator, settings));
			}
			//sensecontent sensenumber sense morphosyntaxanalysis mlpartofspeech en
			info.SenseCounter = 0; // This ticker is more efficient than computing the index for each sense individually
			var senseNode = (DictionaryNodeSenseOptions)config.DictionaryNodeOptions;
			if (senseNode != null)
				info.ParentSenseNumberingStyle = senseNode.ParentSenseNumberingStyle;

			// Calculating isThisSenseNumbered may make sense to do for each item in the foreach loop below, but because of how the answer
			// is determined, the answer for all sibling senses is the same as for the first sense in the collection.
			// So calculating outside the loop for performance.
			var isThisSenseNumbered = ShouldThisSenseBeNumbered(filteredSenseCollection[0], config, filteredSenseCollection);
			foreach (var item in filteredSenseCollection)
			{
				info.SenseCounter++;
				bldr.Append(GenerateSenseContent(config, publicationDecorator, item, isThisSenseNumbered, settings, isSameGrammaticalInfo, info));
			}
			return bldr.ToString();
		}

		/// <summary>
		/// Some behaviour discussed regarding whether to show a sense number while working on LT-17906 is as follows.
		///
		/// Does the numbering style for senses say to number it?
		///  - No? Don't number.
		/// Yes? Is this the only sense-level sense?
		///  - No? Number it.
		/// Yes? Is the box for 'Number even a single sense' checked?
		///  - Yes? Number it.
		/// No? Is there a subsense?
		///  - No? Don't number.
		/// Yes? Is the subsense showing (enabled in the config)?
		///  - No? Don't number.
		/// Yes? Does the style for the subsense say to number the subsense?
		///  - No? Don't number.
		///  - Yes? Number it.
		/// </summary>
		internal static bool ShouldThisSenseBeNumbered(ILexSense sense, ConfigurableDictionaryNode senseConfiguration,
			IEnumerable<ILexSense> siblingSenses)
		{
			var senseOptions = senseConfiguration.DictionaryNodeOptions as DictionaryNodeSenseOptions;
			if (string.IsNullOrEmpty(senseOptions.NumberingStyle))
				return false;
			if (siblingSenses.Count() > 1)
				return true;
			if (senseOptions.NumberEvenASingleSense)
				return true;
			if (sense.SensesOS.Count == 0)
				return false;
			if (!AreThereEnabledSubsensesWithNumberingStyle(senseConfiguration))
				return false;
			return true;
		}

		/// <summary>
		/// Does this sense node have a subsenses node that is enabled in the configuration and has numbering style?
		/// </summary>
		/// <param name="senseNode">sense node that might have subsenses</param>
		internal static bool AreThereEnabledSubsensesWithNumberingStyle(ConfigurableDictionaryNode senseNode)
		{
			if (senseNode == null)
				return false;
			return senseNode.Children.Any(child =>
				child.DictionaryNodeOptions is DictionaryNodeSenseOptions &&
				child.IsEnabled &&
				!string.IsNullOrEmpty(((DictionaryNodeSenseOptions)child.DictionaryNodeOptions).NumberingStyle));
		}

		private static string InsertGramInfoBeforeSenses(ILexSense item, ConfigurableDictionaryNode gramInfoNode,
			DictionaryPublicationDecorator publicationDecorator, GeneratorSettings settings)
		{
			var content = GenerateXHTMLForFieldByReflection(item, gramInfoNode, publicationDecorator, settings);
			if (string.IsNullOrEmpty(content))
				return string.Empty;
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

		private static bool IsAllGramInfoTheSame(ConfigurableDictionaryNode config, IEnumerable<ILexSense> collection, bool isSubsense,
			out string lastGrammaticalInfo, out string langId)
		{
			lastGrammaticalInfo = String.Empty;
			langId = String.Empty;
			var isSameGrammaticalInfo = false;
			if (config.FieldDescription == "SensesOS" || config.FieldDescription == "ReferringSenses")
			{
				var senseNode = (DictionaryNodeSenseOptions)config.DictionaryNodeOptions;
				if (senseNode == null)
					return false;
				if (senseNode.ShowSharedGrammarInfoFirst)
				{
					if (isSubsense)
					{
						// Add the owning sense to the collection that we want to check.
						var objs = new List<ILexSense>();
						objs.AddRange(collection);
						if (objs.Count == 0 || !(objs[0].Owner is ILexSense))
							return false;
						objs.Add((ILexSense)objs[0].Owner);
						if (!CheckIfAllGramInfoTheSame(config, objs, ref isSameGrammaticalInfo, ref lastGrammaticalInfo, ref langId))
							return false;
					}
					else
					{
						if (!CheckIfAllGramInfoTheSame(config, collection, ref isSameGrammaticalInfo, ref lastGrammaticalInfo, ref langId))
							return false;
					}
				}
			}
			return isSameGrammaticalInfo && !string.IsNullOrEmpty(lastGrammaticalInfo);
		}

		private static bool CheckIfAllGramInfoTheSame(ConfigurableDictionaryNode config, IEnumerable<ILexSense> collection,
			ref bool isSameGrammaticalInfo, ref string lastGrammaticalInfo, ref string langId)
		{
			foreach (var item in collection)
			{
				var requestedString = string.Empty;
				var owningObject = (ICmObject)item;
				var defaultWs = owningObject.Cache.WritingSystemFactory.get_EngineOrNull(owningObject.Cache.DefaultUserWs);
				langId = defaultWs.Id;
				var entryType = item.GetType();
				var grammaticalInfo = config.ReferencedOrDirectChildren.FirstOrDefault(e => e.FieldDescription == "MorphoSyntaxAnalysisRA" && e.IsEnabled);
				if (grammaticalInfo == null)
					return false;
				var property = entryType.GetProperty(grammaticalInfo.FieldDescription);
				var propertyValue = property.GetValue(item, new object[] { });
				if (propertyValue == null)
					return false;
				var child = grammaticalInfo.ReferencedOrDirectChildren.FirstOrDefault(e => e.IsEnabled && e.ReferencedOrDirectChildren.Count == 0);
				if (child == null)
					return false;
				entryType = propertyValue.GetType();
				property = entryType.GetProperty(child.FieldDescription);
				propertyValue = property.GetValue(propertyValue, new object[] { });
				if (propertyValue is ITsString)
				{
					ITsString fieldValue = (ITsString)propertyValue;
					requestedString = fieldValue.Text;
				}
				else
				{
					IMultiAccessorBase fieldValue = (IMultiAccessorBase)propertyValue;
					var bestStringValue = fieldValue.BestAnalysisAlternative.Text;
					if (bestStringValue != fieldValue.NotFoundTss.Text)
						requestedString = bestStringValue;
				}
				if (string.IsNullOrEmpty(lastGrammaticalInfo))
				{
					lastGrammaticalInfo = requestedString;
					isSameGrammaticalInfo = true;
				}
				else if (requestedString != lastGrammaticalInfo)
				{
					return false;
				}
			}
			return true;
		}

		private static string GenerateSenseContent(ConfigurableDictionaryNode config, DictionaryPublicationDecorator publicationDecorator,
			object item, bool isThisSenseNumbered, GeneratorSettings settings, bool isSameGrammaticalInfo, SenseInfo info)
		{
			var senseNumberSpan = GenerateSenseNumberSpanIfNeeded(config, isThisSenseNumbered, ref info);
			var bldr = new StringBuilder();
			if (config.ReferencedOrDirectChildren != null)
			{
				foreach (var child in config.ReferencedOrDirectChildren)
				{
					if (child.FieldDescription != "MorphoSyntaxAnalysisRA" || !isSameGrammaticalInfo)
					{
						bldr.Append(ReallyGenerateXHTMLForFieldByReflection(item, child, publicationDecorator, settings, info));
					}
				}
			}
			if (bldr.Length == 0)
				return string.Empty;
			var senseContent = bldr.ToString();
			bldr.Clear();
			using (var xw = XmlWriter.Create(bldr, new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment }))
			{
				// Wrap the number and sense combination in a sensecontent span so that can both be affected by DisplayEachSenseInParagraph
				xw.WriteStartElement("span");
				xw.WriteAttributeString("class", "sensecontent");
				xw.WriteRaw(senseNumberSpan);
				xw.WriteStartElement(GetElementNameForProperty(config));
				WriteCollectionItemClassAttribute(config, xw);
				xw.WriteAttributeString("entryguid", "g" + ((ICmObject)item).Owner.Guid.ToString());
				xw.WriteRaw(senseContent);
				xw.WriteEndElement();	// element name for property
				xw.WriteEndElement();	// </span>
				xw.Flush();
				return bldr.ToString();
			}
		}

		private static void GeneratePictureContent(ConfigurableDictionaryNode config, DictionaryPublicationDecorator publicationDecorator,
			object item, GeneratorSettings settings, StringBuilder bldr)
		{
			//Adding Thumbnail tag
			foreach (var child in config.ReferencedOrDirectChildren)
			{
				if (child.FieldDescription == "PictureFileRA")
				{
					var content = GenerateXHTMLForFieldByReflection(item, child, publicationDecorator, settings);
					bldr.Append(content);
					break;
				}
			}
			//Adding tags for Sense Number and Caption
			// Note: this SenseNumber comes from a field in the FDO model (not generated based on a DictionaryNodeSenseOptions).
			//  Should we choose in the future to generate the Picture's sense number using ConfiguredXHTMLGenerator based on a SenseOption,
			//  we will need to pass the SenseOptions to this point in the call tree.
			var captionBldr = new StringBuilder();
			foreach (var child in config.ReferencedOrDirectChildren)
			{
				if (child.FieldDescription != "PictureFileRA")
				{
					var content = GenerateXHTMLForFieldByReflection(item, child, publicationDecorator, settings);
					captionBldr.Append(content);
				}
			}
			if (captionBldr.Length == 0)
			return;
			//Adding div tag before Sense Number and Caption
			using (var xw = XmlWriter.Create(bldr, new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment }))
			{
				  xw.WriteStartElement("div");
				  xw.WriteAttributeString("class", "captionContent");
				  xw.WriteRaw(captionBldr.ToString());
				  xw.WriteEndElement();
			}
		}

		private static string GenerateCollectionItemContent(ConfigurableDictionaryNode config, DictionaryPublicationDecorator publicationDecorator,
			object item, object collectionOwner, GeneratorSettings settings, ConfigurableDictionaryNode factoredTypeField = null)
		{
			if (item is IMultiStringAccessor)
				return GenerateXHTMLForStrings((IMultiStringAccessor)item, config, settings);
			if ((config.DictionaryNodeOptions is DictionaryNodeListOptions && !IsListItemSelectedForExport(config, item, collectionOwner))
				|| config.ReferencedOrDirectChildren == null)
				return string.Empty;

			var bldr = new StringBuilder();
			var listOptions = config.DictionaryNodeOptions as DictionaryNodeListOptions;
			if (listOptions is DictionaryNodeListAndParaOptions)
			{
				foreach (var child in config.ReferencedOrDirectChildren.Where(child => !ReferenceEquals(child, factoredTypeField)))
				{
					bldr.Append(child.FieldDescription == LookupComplexEntryType
						? GenerateSubentryTypeChild(child, publicationDecorator, (ILexEntry)item, collectionOwner, settings)
						: GenerateXHTMLForFieldByReflection(item, child, publicationDecorator, settings));
				}
			}
			else if (config.DictionaryNodeOptions is DictionaryNodePictureOptions)
			{
				GeneratePictureContent(config, publicationDecorator, item, settings, bldr);
			}
			else
			{
				// If a type field has been factored out and generated then skip generating it here
				foreach (var child in config.ReferencedOrDirectChildren.Where(child => !ReferenceEquals(child, factoredTypeField)))
				{
					bldr.Append(GenerateXHTMLForFieldByReflection(item, child, publicationDecorator, settings));
				}
			}
			if (bldr.Length == 0)
				return string.Empty;
			var collectionContent = bldr.ToString();
			bldr.Clear();
			using (var xw = XmlWriter.Create(bldr, new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment }))
			{
				xw.WriteStartElement(GetElementNameForProperty(config));
				WriteCollectionItemClassAttribute(config, xw);
				xw.WriteRaw(collectionContent);
				xw.WriteEndElement();
				xw.Flush();
				return bldr.ToString();
			}
		}

		private static void GenerateXHTMLForILexReferenceCollection(ConfigurableDictionaryNode config,
			IEnumerable<ILexReference> collection, ICmObject cmOwner, DictionaryPublicationDecorator pubDecorator,
			GeneratorSettings settings, StringBuilder bldr)
		{
			// The collection of ILexReferences has already been sorted by type,
			// so we'll now group all the targets by LexRefType and sort their targets alphabetically before generating XHTML
			var organizedRefs = SortAndFilterLexRefsAndTargets(collection, cmOwner, config);
			// Now that we have things in the right order, try outputting one type at a time
			foreach (var referenceList in organizedRefs)
			{
				var xBldr = GenerateCrossReferenceChildren(config, pubDecorator, referenceList, cmOwner, settings);
				bldr.Append(xBldr);
			}
		}

		/// <returns>A list (by Type) of lists of Lex Reference Targets (tupled with their references)</returns>
		private static List<List<Tuple<ISenseOrEntry, ILexReference>>> SortAndFilterLexRefsAndTargets(
			IEnumerable<ILexReference> collection, ICmObject cmOwner, ConfigurableDictionaryNode config)
		{
			var orderedTargets = new List<List<Tuple<ISenseOrEntry, ILexReference>>>();
			var curType = new Tuple<ILexRefType, string>(null, null);
			var allTargetsForType = new List<Tuple<ISenseOrEntry, ILexReference>>();
			foreach (var lexReference in collection)
			{
				var type = new Tuple<ILexRefType, string>(lexReference.OwnerType, LexRefDirection(lexReference, cmOwner));
				if (!type.Item1.Equals(curType.Item1)
					|| (LexRefTypeTags.IsAsymmetric((LexRefTypeTags.MappingTypes)type.Item1.MappingType) && !type.Item2.Equals(curType.Item2)))
				{
					MoveTargetsToMasterList(cmOwner, curType.Item1, config, allTargetsForType, orderedTargets);
				}
				curType = type;
				if (LexRefTypeTags.IsAsymmetric((LexRefTypeTags.MappingTypes)curType.Item1.MappingType) &&
					LexRefDirection(lexReference, cmOwner) == ":r" && lexReference.ConfigTargets.Any())
				{
					// In the reverse direction of an asymmetric lexical reference, we want only the first item.
					// See https://jira.sil.org/browse/LT-16427.
					allTargetsForType.Add(new Tuple<ISenseOrEntry, ILexReference>(lexReference.ConfigTargets.First(t => !IsOwner(t, cmOwner)), lexReference));
				}
				else
				{
					allTargetsForType.AddRange(lexReference.ConfigTargets
						.Select(target => new Tuple<ISenseOrEntry, ILexReference>(target, lexReference)));
				}
			}
			MoveTargetsToMasterList(cmOwner, curType.Item1, config, allTargetsForType, orderedTargets);
			return orderedTargets;
		}

		private static void MoveTargetsToMasterList(ICmObject cmOwner, ILexRefType curType, ConfigurableDictionaryNode config,
			List<Tuple<ISenseOrEntry, ILexReference>> bucketList, List<List<Tuple<ISenseOrEntry, ILexReference>>> lexRefTargetList)
		{
			if (bucketList.Count == 0)
				return;
			if (!IsListItemSelectedForExport(config, bucketList.First().Item2, cmOwner))
			{
				bucketList.Clear();
				return;
			}

			// In a "Sequence" type lexical relation (e.g. days of the week), the current item should be displayed in its location in the sequence.
			if (!LexRefTypeTags.IsSequence((LexRefTypeTags.MappingTypes)curType.MappingType))
			{
				bucketList.RemoveAll(t => IsOwner(t.Item1, cmOwner));
				// "Unidirectional" relations, like Sequences, are user-orderable (but only sequences include their owner)
				if (!LexRefTypeTags.IsUnidirectional((LexRefTypeTags.MappingTypes)curType.MappingType))
					bucketList.Sort(CompareLexRefTargets);
			}
			lexRefTargetList.Add(new List<Tuple<ISenseOrEntry, ILexReference>>(bucketList));
			bucketList.Clear();
		}

		private static bool IsOwner(ISenseOrEntry target, ICmObject owner)
		{
			return target.Item.Guid.Equals(owner.Guid);
		}

		private static int CompareLexRefTargets(Tuple<ISenseOrEntry, ILexReference> lhs,
			Tuple<ISenseOrEntry, ILexReference> rhs)
		{
			return string.Compare(lhs.Item1.HeadWord.Text, rhs.Item1.HeadWord.Text, StringComparison.CurrentCultureIgnoreCase);
		}

		/// <returns>Content for Targets and nodes, except Type, which is returned in ref string typeXHTML</returns>
		private static string GenerateCrossReferenceChildren(ConfigurableDictionaryNode config, DictionaryPublicationDecorator publicationDecorator,
			List<Tuple<ISenseOrEntry, ILexReference>> referenceList, object collectionOwner, GeneratorSettings settings)
		{
			if (config.ReferencedOrDirectChildren == null)
				return string.Empty;
			var xBldr = new StringBuilder();
			using (var xw = XmlWriter.Create(xBldr, new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment }))
			{
				xw.WriteStartElement(GetElementNameForProperty(config));
				WriteCollectionItemClassAttribute(config, xw);
				var targetInfo = referenceList.FirstOrDefault();
				if (targetInfo == null)
					return string.Empty;
				var reference = targetInfo.Item2;
				if (LexRefTypeTags.IsUnidirectional((LexRefTypeTags.MappingTypes)reference.OwnerType.MappingType) &&
					LexRefDirection(reference, collectionOwner) == ":r")
				{
					return string.Empty;
				}
				foreach (var child in config.ReferencedOrDirectChildren.Where(c => c.IsEnabled))
				{
					switch (child.FieldDescription)
					{
						case "ConfigTargets":
							var contentBldr = new StringBuilder();
							foreach (var referenceListItem in referenceList)
							{
								var referenceItem = referenceListItem.Item2;
								var targetItem = referenceListItem.Item1;
								contentBldr.Append(GenerateCollectionItemContent(child, publicationDecorator, targetItem, referenceItem, settings));
							}
							if (contentBldr.Length > 0)
							{
								xw.WriteStartElement(GetElementNameForProperty(child));
								xw.WriteAttributeString("class", CssGenerator.GetClassAttributeForConfig(child));
								xw.WriteRaw(contentBldr.ToString());
								xw.WriteEndElement(); // targets
							}
							break;
						case "OwnerType":
							// OwnerType is a LexRefType, some of which are asymmetric (e.g. Part/Whole). If this Type is symmetric or we are currently
							// working in the forward direction, the generic code will work; however, if we are working on an asymmetric LexRefType
							// in the reverse direction, we need to display the ReverseName or ReverseAbbreviation instead of the Name or Abbreviation.
							if (LexRefTypeTags.IsAsymmetric((LexRefTypeTags.MappingTypes)reference.OwnerType.MappingType) && LexRefDirection(reference, collectionOwner) == ":r")
							{
								// Changing the SubField changes the default CSS Class name.
								// If there is no override, override with the default before changing the SubField.
								if (string.IsNullOrEmpty(child.CSSClassNameOverride))
									child.CSSClassNameOverride = CssGenerator.GetClassAttributeForConfig(child);
								// Flag to prepend "Reverse" to child.SubField when it is used.
								xw.WriteRaw(ReallyGenerateXHTMLForFieldByReflection(reference, child, publicationDecorator, settings, fUseReverseSubField: true));
							}
							else
							{
								xw.WriteRaw(GenerateXHTMLForFieldByReflection(reference, child, publicationDecorator, settings));
							}
							break;
						default:
							throw new NotImplementedException("The field " + child.FieldDescription + " is not supported on Cross References or Lexical Relations. Supported fields are OwnerType and ConfigTargets");
					}
				}
				xw.WriteEndElement(); // config
				xw.Flush();
			}
			return xBldr.ToString();
		}

		private static string GenerateSubentryTypeChild(ConfigurableDictionaryNode config, DictionaryPublicationDecorator publicationDecorator,
			ILexEntry subEntry, object mainEntryOrSense, GeneratorSettings settings)
		{
			if (!config.IsEnabled)
				return string.Empty;

			var complexEntryRef = EntryRefForSubentry(subEntry, mainEntryOrSense);
			return complexEntryRef == null
				? string.Empty
				: GenerateXHTMLForCollection(complexEntryRef.ComplexEntryTypesRS, config, publicationDecorator, subEntry, settings);
		}

		private static ILexEntryRef EntryRefForSubentry(ILexEntry subEntry, object mainEntryOrSense)
		{
			var mainEntry = mainEntryOrSense as ILexEntry ?? ((ILexSense)mainEntryOrSense).Entry;
			var complexEntryRef = subEntry.ComplexFormEntryRefs.FirstOrDefault(entryRef => entryRef.PrimaryLexemesRS.Contains(mainEntry) // subsubentries
																						|| entryRef.PrimaryEntryRoots.Contains(mainEntry)); // subs under sense
			return complexEntryRef;
		}

		private static string GenerateSenseNumberSpanIfNeeded(ConfigurableDictionaryNode senseConfigNode, bool isThisSenseNumbered, ref SenseInfo info)
		{
			if (!isThisSenseNumbered)
				return string.Empty;

			var senseOptions = senseConfigNode.DictionaryNodeOptions as DictionaryNodeSenseOptions;

			var formattedSenseNumber = GetSenseNumber(senseOptions.NumberingStyle, ref info);
			if (string.IsNullOrEmpty(formattedSenseNumber))
				return string.Empty;
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

		private static string GetSenseNumber(string numberingStyle, ref SenseInfo info)
		{
			string nextNumber;
			switch (numberingStyle)
			{
				case "%a":
				case "%A":
					nextNumber = GetAlphaSenseCounter(numberingStyle, info.SenseCounter);
					break;
				case "%i":
				case "%I":
					nextNumber = GetRomanSenseCounter(numberingStyle, info.SenseCounter);
					break;
				default: // handles %d and %O. We no longer support "%z" (1  b  iii) because users can hand-configure its equivalent
					nextNumber = info.SenseCounter.ToString();
					break;
			}
			info.SenseOutlineNumber = GenerateSenseOutlineNumber(info, nextNumber);
			return info.SenseOutlineNumber;
		}

		private static string GenerateSenseOutlineNumber(SenseInfo info, string nextNumber)
		{
			if (info.ParentSenseNumberingStyle == "%j")
				info.SenseOutlineNumber = string.Format("{0}{1}", info.SenseOutlineNumber, nextNumber);
			else if (info.ParentSenseNumberingStyle == "%.")
				info.SenseOutlineNumber = string.Format("{0}.{1}", info.SenseOutlineNumber, nextNumber);
			else
				info.SenseOutlineNumber = nextNumber;

			return info.SenseOutlineNumber;
		}

		private static string GetAlphaSenseCounter(string numberingStyle, int senseNumber)
		{
			var asciiBytes = 64; // char 'A'
			asciiBytes = asciiBytes + senseNumber;
			var nextNumber = ((char)(asciiBytes)).ToString();
			if (numberingStyle == "%a")
				nextNumber = nextNumber.ToLower();
			return nextNumber;
		}

		private static string GetRomanSenseCounter(string numberingStyle, int senseNumber)
		{
			string roman = string.Empty;
			roman = RomanNumerals.IntToRoman(senseNumber);
			if (numberingStyle == "%i")
				roman = roman.ToLower();
			return roman;
		}

		private static string GenerateXHTMLForICmObject(ICmObject propertyValue, ConfigurableDictionaryNode config, GeneratorSettings settings)
		{
			// Don't export if there is no such data
			if (propertyValue == null || config.ReferencedOrDirectChildren == null || !config.ReferencedOrDirectChildren.Any(node => node.IsEnabled))
				return string.Empty;
			var bldr = new StringBuilder();
			foreach (var child in config.ReferencedOrDirectChildren)
			{
				var content = GenerateXHTMLForFieldByReflection(propertyValue, child, null, settings);
				bldr.Append(content);
			}
			if (bldr.Length > 0)
				return WriteRawElementContents("span", bldr.ToString(), config);
			return String.Empty;
		}

		/// <summary>Write the class element in the span for an individual item in the collection</summary>
		private static void WriteCollectionItemClassAttribute(ConfigurableDictionaryNode config, XmlWriter writer)
		{
			var classAtt = CssGenerator.GetClassAttributeForCollectionItem(config);
			if (config.ReferencedNode != null)
				classAtt = string.Format("{0} {1}", classAtt, CssGenerator.GetClassAttributeForCollectionItem(config.ReferencedNode));
			writer.WriteAttributeString("class", classAtt);
		}

		/// <summary>
		/// This method is used to determine if we need to iterate through a property and generate xhtml for each item
		/// </summary>
		internal static bool IsCollectionType(Type entryType)
		{
			// The collections we test here are generic collection types (e.g. IEnumerable<T>). Note: This (and other code) does not work for arrays.
			// We do have at least one collection type with at least two generic arguments; hence `> 0` instead of `== 1`
			return entryType.GetGenericArguments().Length > 0 || typeof(ILcmVector).IsAssignableFrom(entryType);
		}

		internal static bool IsCollectionNode(ConfigurableDictionaryNode configNode, IFwMetaDataCacheManaged metaDataCacheAccessor)
		{
			return GetPropertyTypeForConfigurationNode(configNode, metaDataCacheAccessor) == PropertyType.CollectionType;
		}

		/// <summary>
		/// Determines if the user has specified that this item should generate content.
		/// <returns><c>true</c> if the user has ticked the list item that applies to this object</returns>
		/// </summary>
		internal static bool IsListItemSelectedForExport(ConfigurableDictionaryNode config, object listItem, object parent = null)
		{
			var listOptions = config.DictionaryNodeOptions as DictionaryNodeListOptions;
			if (listOptions == null)
				throw new ArgumentException(string.Format("This configuration node had no options and we were expecting them: {0} ({1})", config.DisplayLabel, config.FieldDescription), "config");

			var selectedListOptions = new List<Guid>();
			var forwardReverseOptions = new List<Tuple<Guid, string>>();
			foreach (var option in listOptions.Options.Where(optn => optn.IsEnabled))
			{
				var forwardReverseIndicator = option.Id.IndexOf(':');
				if (forwardReverseIndicator > 0)
				{
					var guid = new Guid(option.Id.Substring(0, forwardReverseIndicator));
					forwardReverseOptions.Add(new Tuple<Guid, string>(guid, option.Id.Substring(forwardReverseIndicator)));
				}
				else
				{
					selectedListOptions.Add(new Guid(option.Id));
				}
			}
			switch (listOptions.ListId)
			{
				case DictionaryNodeListOptions.ListIds.Variant:
				case DictionaryNodeListOptions.ListIds.Complex:
				case DictionaryNodeListOptions.ListIds.Minor:
				case DictionaryNodeListOptions.ListIds.Note:
					return IsListItemSelectedForExportInternal(listOptions.ListId, listItem, selectedListOptions);
				case DictionaryNodeListOptions.ListIds.Entry:
				case DictionaryNodeListOptions.ListIds.Sense:
					var lexRef = (ILexReference)listItem;
					var entryTypeGuid = lexRef.OwnerType.Guid;
					if (selectedListOptions.Contains(entryTypeGuid))
						return true;
					var entryTypeGuidAndDirection = new Tuple<Guid, string>(entryTypeGuid, LexRefDirection(lexRef, parent));
					return forwardReverseOptions.Contains(entryTypeGuidAndDirection);
				case DictionaryNodeListOptions.ListIds.None:
					return true;
				default:
					Debug.WriteLine("Unhandled list ID encountered: " + listOptions.ListId);
					return true;
			}
		}

		private static bool IsListItemSelectedForExportInternal(DictionaryNodeListOptions.ListIds listId,
			object listItem, IEnumerable<Guid> selectedListOptions)
		{
			var entryTypeGuids = new HashSet<Guid>();
			var entryRef = listItem as ILexEntryRef;
			var entry = listItem as ILexEntry;
			var entryType = listItem as ILexEntryType;
			var note = listItem as ILexExtendedNote;
			if (entryRef != null)
			{
				if (listId == DictionaryNodeListOptions.ListIds.Variant || listId == DictionaryNodeListOptions.ListIds.Minor)
					GetVariantTypeGuidsForEntryRef(entryRef, entryTypeGuids);
				if (listId == DictionaryNodeListOptions.ListIds.Complex || listId == DictionaryNodeListOptions.ListIds.Minor)
					GetComplexFormTypeGuidsForEntryRef(entryRef, entryTypeGuids);
			}
			else if (entry != null)
			{
				if (listId == DictionaryNodeListOptions.ListIds.Variant || listId == DictionaryNodeListOptions.ListIds.Minor)
					foreach (var variantEntryRef in entry.VariantEntryRefs)
						GetVariantTypeGuidsForEntryRef(variantEntryRef, entryTypeGuids);
				if (listId == DictionaryNodeListOptions.ListIds.Complex || listId == DictionaryNodeListOptions.ListIds.Minor)
					foreach (var complexFormEntryRef in entry.ComplexFormEntryRefs)
						GetComplexFormTypeGuidsForEntryRef(complexFormEntryRef, entryTypeGuids);
			}
			else if (entryType != null)
			{
				entryTypeGuids.Add(entryType.Guid);
			}
			else if (note != null)
			{
				if (listId == DictionaryNodeListOptions.ListIds.Note)
					GetExtendedNoteGuidsForEntryRef(note, entryTypeGuids);
			}
			return entryTypeGuids.Intersect(selectedListOptions).Any();
		}

		private static void GetVariantTypeGuidsForEntryRef(ILexEntryRef entryRef, HashSet<Guid> entryTypeGuids)
		{
			if (entryRef.VariantEntryTypesRS.Any())
				entryTypeGuids.UnionWith(entryRef.VariantEntryTypesRS.Select(guid => guid.Guid));
			else
				entryTypeGuids.Add(XmlViewsUtils.GetGuidForUnspecifiedVariantType());
		}

		private static void GetComplexFormTypeGuidsForEntryRef(ILexEntryRef entryRef, HashSet<Guid> entryTypeGuids)
		{
			if (entryRef.ComplexEntryTypesRS.Any())
				entryTypeGuids.UnionWith(entryRef.ComplexEntryTypesRS.Select(guid => guid.Guid));
			else
				entryTypeGuids.Add(XmlViewsUtils.GetGuidForUnspecifiedComplexFormType());
		}

		private static void GetExtendedNoteGuidsForEntryRef(ILexExtendedNote entryRef, HashSet<Guid> entryTypeGuids)
		{
			if (entryRef.ExtendedNoteTypeRA != null)
				entryTypeGuids.Add(entryRef.ExtendedNoteTypeRA.Guid);
			else
				entryTypeGuids.Add(XmlViewsUtils.GetGuidForUnspecifiedExtendedNoteType());
		}

		/// <returns>
		/// ":f" if we are working in the forward direction (the parent is the head of a tree or asymmetric pair);
		/// ":r" if we are working in the reverse direction (the parent is a subordinate in a tree or asymmetric pair).
		/// </returns>
		/// <remarks>This method does not determine symmetry; use <see cref="LexRefTypeTags.IsAsymmetric"/> for that.</remarks>
		private static string LexRefDirection(ILexReference lexRef, object parent)
		{
			return Equals(lexRef.TargetsRS[0], parent) ? ":f" : ":r";
		}

		/// <summary>
		/// Returns true if the given collection is empty (type determined at runtime)
		/// </summary>
		/// <param name="collection"></param>
		/// <exception cref="ArgumentException">if the object given is null, or not a handled collection</exception>
		/// <returns></returns>
		private static bool IsCollectionEmpty(object collection)
		{
			if (collection == null)
			{
				throw new ArgumentNullException("collection");
			}
			if (collection is IEnumerable)
			{
				return !(((IEnumerable)collection).Cast<object>().Any());
			}
			if (collection is ILcmVector)
			{
				return ((ILcmVector)collection).ToHvoArray().Length == 0;
			}
			throw new ArgumentException(@"Cannot test something that isn't a collection", "collection");
		}

		/// <summary>
		/// This method generates XHTML content for a given object
		/// </summary>
		/// <param name="field">This is the object that owns the property, needed to look up writing system info for virtual string fields</param>
		/// <param name="propertyValue">data to generate xhtml for</param>
		/// <param name="config"></param>
		/// <param name="settings"></param>
		private static string GenerateXHTMLForValue(object field, object propertyValue, ConfigurableDictionaryNode config, GeneratorSettings settings)
		{
			// If we're working with a headword, either for this entry or another one (Variant or Complex Form, etc.), store that entry's GUID
			// so we can generate a link to the main or minor entry for this headword.
			var guid = Guid.Empty;
			if (config.IsHeadWord)
			{
				if (field is ILexEntry)
					guid = ((ILexEntry)field).Guid;
				else if (field is ILexEntryRef)
					guid = ((ILexEntryRef)field).OwningEntry.Guid;
				else if (field is ISenseOrEntry)
					guid = ((ISenseOrEntry)field).EntryGuid;
				else if (field is ILexSense)
					guid = ((ILexSense)field).OwnerOfClass(LexEntryTags.kClassId).Guid;
				else
					Debug.WriteLine(String.Format("Need to find Entry Guid for {0}",
						field == null ? DictionaryConfigurationServices.BuildPathStringFromNode(config) : field.GetType().Name));
			}

			if (propertyValue is ITsString)
			{
				if (!TsStringUtils.IsNullOrEmpty((ITsString)propertyValue))
				{
					var content = GenerateXHTMLForString((ITsString)propertyValue, config, settings, guid);
					if (!String.IsNullOrEmpty(content))
						return WriteRawElementContents("span", content, config);
				}
				return String.Empty;
			}
			else if (propertyValue is IMultiStringAccessor)
			{
				return GenerateXHTMLForStrings((IMultiStringAccessor)propertyValue, config, settings, guid);
			}
			else if (propertyValue is int)
			{
				return WriteElementContents(propertyValue, config);
			}
			else if (propertyValue is DateTime)
			{
				return WriteElementContents(((DateTime)propertyValue).ToLongDateString(), config);
			}
			else if (propertyValue is GenDate)
			{
				return WriteElementContents(((GenDate)propertyValue).ToLongString(), config);
			}
			else if (propertyValue is IMultiAccessorBase)
			{
				if (field is ISenseOrEntry)
					return GenerateXHTMLForVirtualStrings(((ISenseOrEntry)field).Item, (IMultiAccessorBase)propertyValue, config, settings, guid);
				return GenerateXHTMLForVirtualStrings((ICmObject)field, (IMultiAccessorBase)propertyValue, config, settings, guid);
			}
			else if (propertyValue is String)
			{
				return WriteElementContents(propertyValue, config);
			}
			else if (propertyValue is IStText)
			{
				var bldr = new StringBuilder();
				foreach (var para in (propertyValue as IStText).ParagraphsOS)
				{
					IStTxtPara stp = para as IStTxtPara;
					if (stp == null)
						continue;
					var contentPara = GenerateXHTMLForString(stp.Contents, config, settings, guid);
					if (!String.IsNullOrEmpty(contentPara))
					{
						bldr.Append(contentPara);
						bldr.AppendLine();
					}
				}
				if (bldr.Length > 0)
				{
					// Do we not have/want a class from the config node?
					return WriteRawElementContents("div", bldr.ToString(), null);
				}
				return String.Empty;
			}
			else
			{
				if (propertyValue == null)
				{
					Debug.WriteLine(String.Format("Bad configuration node: {0}", DictionaryConfigurationServices.BuildPathStringFromNode(config)));
				}
				else
				{
					Debug.WriteLine(String.Format("What do I do with {0}?", propertyValue.GetType().Name));
				}
				return String.Empty;
			}
		}

		private static string WriteElementContents(object propertyValue, ConfigurableDictionaryNode config)
		{
			var content = propertyValue.ToString();
			if (!String.IsNullOrEmpty(content))
			{
				var bldr = new StringBuilder();
				using (var xw = XmlWriter.Create(bldr, new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment }))
				{
					xw.WriteStartElement(GetElementNameForProperty(config));
					WriteClassNameAttributeForConfig(xw, config);
					xw.WriteString(content);
					xw.WriteEndElement();
					xw.Flush();
					return bldr.ToString();
				}
			}
			return String.Empty;
		}

		private static string WriteRawElementContents(string elementName, string xmlContent, ConfigurableDictionaryNode config)
		{
			if (!String.IsNullOrEmpty(xmlContent))
			{
				var bldr = new StringBuilder();
				using (var xw = XmlWriter.Create(bldr, new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment }))
				{
					xw.WriteStartElement(elementName);
					if (config != null)
						WriteClassNameAttributeForConfig(xw, config);
					xw.WriteRaw(xmlContent);
					xw.WriteEndElement();
					xw.Flush();
					return bldr.ToString();
				}
			}
			return String.Empty;
		}

		private static string GenerateXHTMLForStrings(IMultiStringAccessor multiStringAccessor, ConfigurableDictionaryNode config,
			GeneratorSettings settings)
		{
			return GenerateXHTMLForStrings(multiStringAccessor, config, settings, Guid.Empty);
		}

		/// <summary>
		/// This method will generate an XHTML span with a string for each selected writing system in the
		/// DictionaryWritingSystemOptions of the configuration that also has data in the given IMultiStringAccessor
		/// </summary>
		private static string GenerateXHTMLForStrings(IMultiStringAccessor multiStringAccessor, ConfigurableDictionaryNode config,
			GeneratorSettings settings, Guid guid)
		{
			var wsOptions = config.DictionaryNodeOptions as DictionaryNodeWritingSystemOptions;
			if (wsOptions == null)
			{
				throw new ArgumentException(@"Configuration nodes for MultiString fields should have WritingSystemOptions", "config");
			}
			// TODO pH 2014.12: this can generate an empty span if no checked WS's contain data
			// gjm 2015.12 but this will help some (LT-16846)
			if (multiStringAccessor == null || multiStringAccessor.StringCount == 0)
				return String.Empty;
			var bldr = new StringBuilder();
			foreach (var option in wsOptions.Options)
			{
				if (!option.IsEnabled)
				{
					continue;
				}
				var wsId = WritingSystemServices.GetMagicWsIdFromName(option.Id);
				// The string for the specific wsId in the option, or the best string option in the accessor if the wsId is magic
				ITsString bestString;
				if (wsId == 0)
				{
					// This is not a magic writing system, so grab the user requested string
					wsId = settings.Cache.WritingSystemFactory.GetWsFromStr(option.Id);
					if (wsId == 0) // The config is bad or stale, but we don't need to crash in this instance.
					{
						Debug.WriteLine("Writing system requested that is not known in local store: {0}", option.Id);
						continue;
					}
					bestString = multiStringAccessor.get_String(wsId);
				}
				else
				{
					// Writing system is magic i.e. 'best vernacular' or 'first pronunciation'
					// use the method in the multi-string to get the right string and set wsId to the used one
					bestString = multiStringAccessor.GetAlternativeOrBestTss(wsId, out wsId);
				}
				var contentItem = GenerateWsPrefixAndString(config, settings, wsOptions, wsId, bestString, guid);

				if (!String.IsNullOrEmpty(contentItem))
					bldr.Append(contentItem);
			}
			if (bldr.Length > 0)
			{
				return WriteRawElementContents("span", bldr.ToString(), config);
			}
			return String.Empty;
		}

		/// <summary>
		/// This method will generate an XHTML span with a string for each selected writing system in the
		/// DictionaryWritingSystemOptions of the configuration that also has data in the given IMultiAccessorBase
		/// </summary>
		private static string GenerateXHTMLForVirtualStrings(ICmObject owningObject, IMultiAccessorBase multiStringAccessor,
																			ConfigurableDictionaryNode config, GeneratorSettings settings, Guid guid)
		{
			var wsOptions = config.DictionaryNodeOptions as DictionaryNodeWritingSystemOptions;
			if (wsOptions == null)
			{
				throw new ArgumentException(@"Configuration nodes for MultiString fields should have WritingSystemOptions", "config");
			}
			var bldr = new StringBuilder();
			foreach (var option in wsOptions.Options)
			{
				if (!option.IsEnabled)
				{
					continue;
				}
				var wsId = WritingSystemServices.GetMagicWsIdFromName(option.Id);
				// The string for the specific wsId in the option, or the best string option in the accessor if the wsId is magic
				if (wsId == 0)
				{
					// This is not a magic writing system, so grab the user requested string
					wsId = settings.Cache.WritingSystemFactory.GetWsFromStr(option.Id);
				}
				else
				{
					var defaultWs = owningObject.Cache.WritingSystemFactory.get_EngineOrNull(owningObject.Cache.DefaultUserWs);
					wsId = WritingSystemServices.InterpretWsLabel(owningObject.Cache, option.Id, (CoreWritingSystemDefinition)defaultWs,
																					owningObject.Hvo, multiStringAccessor.Flid, (CoreWritingSystemDefinition)defaultWs);
				}
				var requestedString = multiStringAccessor.get_String(wsId);
				bldr.Append(GenerateWsPrefixAndString(config, settings, wsOptions, wsId, requestedString, guid));
			}
			if (bldr.Length > 0)
			{
				return WriteRawElementContents("span", bldr.ToString(), config);
			}
			return String.Empty;
		}

		private static string GenerateWsPrefixAndString(ConfigurableDictionaryNode config, GeneratorSettings settings,
			DictionaryNodeWritingSystemOptions wsOptions, int wsId, ITsString requestedString, Guid guid)
		{
			if (String.IsNullOrEmpty(requestedString.Text))
			{
				return String.Empty;
			}
			var wsName = settings.Cache.WritingSystemFactory.get_EngineOrNull(wsId).Id;
			var content = GenerateXHTMLForString(requestedString, config, settings, guid, wsName);
			if (String.IsNullOrEmpty(content))
				return String.Empty;
			var bldr = new StringBuilder();
			using (var xw = XmlWriter.Create(bldr, new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment }))
			{
				if (wsOptions.DisplayWritingSystemAbbreviations)
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

		private static string GenerateXHTMLForString(ITsString fieldValue, ConfigurableDictionaryNode config,
			GeneratorSettings settings, string writingSystem = null)
		{
			return GenerateXHTMLForString(fieldValue, config, settings, Guid.Empty, writingSystem);
		}

		private static string GenerateXHTMLForString(ITsString fieldValue, ConfigurableDictionaryNode config,
			GeneratorSettings settings, Guid linkTarget, string writingSystem = null)
		{
			if (TsStringUtils.IsNullOrEmpty(fieldValue))
				return string.Empty;
			if (writingSystem != null && writingSystem.Contains("audio"))
			{
				var fieldText = fieldValue.Text;
				if (fieldText.Contains("."))
				{
					var audioId = fieldText.Substring(0, fieldText.IndexOf(".", StringComparison.Ordinal));
					var srcAttr = GenerateSrcAttributeForMediaFromFilePath(fieldText, "AudioVisual", settings);
					var content = GenerateXHTMLForAudioFile(writingSystem, audioId, srcAttr, string.Empty, settings);
					if (!string.IsNullOrEmpty(content))
						return WriteRawElementContents("span", content, null);
				}
			}
			else
			{
				// use the passed in writing system unless null
				// otherwise use the first option from the DictionaryNodeWritingSystemOptions or english if the options are null
				var bldr = new StringBuilder();
				using (var xw = XmlWriter.Create(bldr, new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment }))
				{
					var rightToLeft = settings.RightToLeft;
					if (fieldValue.RunCount > 1)
					{
						xw.WriteStartElement("span");
						writingSystem = writingSystem ?? GetLanguageFromFirstOption(config.DictionaryNodeOptions as DictionaryNodeWritingSystemOptions, settings.Cache);
						xw.WriteAttributeString("lang", writingSystem);
						var wsRtl = settings.Cache.WritingSystemFactory.get_Engine(writingSystem).RightToLeftScript;
						if (rightToLeft != wsRtl)
						{
							rightToLeft = wsRtl; // the outer WS direction will be used to identify embedded runs of the opposite direction.
							xw.WriteStartElement("span"); // set direction on a nested span to preserve Context's position and direction.
							xw.WriteAttributeString("dir", rightToLeft ? "rtl" : "ltr");
						}
					}
					for (int i = 0; i < fieldValue.RunCount; i++)
					{
						var text = fieldValue.get_RunText(i);
						var props = fieldValue.get_Properties(i);
						var style = props.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
						writingSystem = settings.Cache.WritingSystemFactory.GetStrFromWs(fieldValue.get_WritingSystem(i));
						GenerateSpanWithPossibleLink(settings, writingSystem, xw, style, text, linkTarget, rightToLeft);
					}
					if (fieldValue.RunCount > 1)
					{
						if (rightToLeft != settings.RightToLeft)
							xw.WriteEndElement(); // </span> (dir)
						xw.WriteEndElement(); // </span> (lang)
					}
					xw.Flush();
					return bldr.ToString();
				}
			}
			return string.Empty;
		}

		private static void GenerateSpanWithPossibleLink(GeneratorSettings settings, string writingSystem, XmlWriter writer, string style,
			string text, Guid linkDestination, bool rightToLeft)
		{
			writer.WriteStartElement("span");
			writer.WriteAttributeString("lang", writingSystem);
			var wsRtl = settings.Cache.WritingSystemFactory.get_Engine(writingSystem).RightToLeftScript;
			if (rightToLeft != wsRtl)
			{
				writer.WriteStartElement("span"); // set direction on a nested span to preserve Context's position and direction
				writer.WriteAttributeString("dir", wsRtl ? "rtl" : "ltr");
			}
			if (!string.IsNullOrEmpty(style))
			{
				var wsId = settings.Cache.WritingSystemFactory.GetWsFromStr(writingSystem);
				var cssStyle = CssGenerator.GenerateCssStyleFromLcmStyleSheet(style, wsId, settings.ReadOnlyPropertyTable.GetValue<LcmStyleSheet>("FlexStyleSheet"), settings.Cache.ServiceLocator.WritingSystemManager.get_EngineOrNull(wsId));
				var css = cssStyle.ToString();
				if (!string.IsNullOrEmpty(css))
				{
					writer.WriteAttributeString("style", css);
				}
			}
			if (linkDestination != Guid.Empty)
			{
				writer.WriteStartElement("a");
				writer.WriteAttributeString("href", "#g" + linkDestination);
			}
			const char txtlineSplit = (char)8232; //Line-Seperator Decimal Code
			if (text.Contains(txtlineSplit))
			{
				var txtContents = text.Split(txtlineSplit);
				for (int i = 0; i < txtContents.Count(); i++)
				{
					writer.WriteString(txtContents[i]);
					if (i == txtContents.Count() - 1)
						break;
					writer.WriteStartElement("br");
					writer.WriteEndElement();
				}
			}
			else
			{
				writer.WriteString(text);
			}
			if (linkDestination != Guid.Empty)
			{
				writer.WriteEndElement(); // </a>
			}
			if (rightToLeft != wsRtl)
			{
				writer.WriteEndElement(); // </span> (dir)
			}
			writer.WriteEndElement(); // </span> (lang)
		}

		/// <summary>
		/// This method Generate XHTML for Audio file
		/// </summary>
		/// <param name="classname">value for class attribute for audio tag</param>
		/// <param name="audioId">value for Id attribute for audio tag</param>
		/// <param name="srcAttribute">Source location path for audio file</param>
		/// <param name="caption">Innertext for hyperlink</param>
		/// <param name="settings"></param>
		/// <returns></returns>
		private static string GenerateXHTMLForAudioFile(string classname,
			string audioId, string srcAttribute, string caption, GeneratorSettings settings)
		{
			if (String.IsNullOrEmpty(audioId) && String.IsNullOrEmpty(srcAttribute) && String.IsNullOrEmpty(caption))
				return String.Empty;
			var safeAudioId = GetSafeXHTMLId(audioId);
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

		private static string GetSafeXHTMLId(string audioId)
		{
			// Prepend a letter, since some filenames start with digits, which gives an invalid id
			// Are there other characters that are unsafe in XHTML Ids or Javascript?
			return "g" + audioId.Replace(" ", "_").Replace("'", "_");
		}

		/// <summary>
		/// This method is intended to produce the xhtml element that we want for given configuration objects.
		/// </summary>
		/// <param name="config"></param>
		/// <returns></returns>
		private static string GetElementNameForProperty(ConfigurableDictionaryNode config)
		{
			//TODO: Improve this logic to deal with subentries if necessary
			if (config.FieldDescription.Equals("LexEntry") || config.DictionaryNodeOptions is DictionaryNodePictureOptions)
			{
				return "div";
			}
			return "span";
		}

		/// <summary>
		/// This method returns the lang attribute value from the first selected writing system in the given options.
		/// </summary>
		/// <param name="wsOptions"></param>
		/// <param name="cache"></param>
		/// <returns></returns>
		private static string GetLanguageFromFirstOption(DictionaryNodeWritingSystemOptions wsOptions, LcmCache cache)
		{
			const string defaultLang = "en";
			if (wsOptions == null)
				return defaultLang;
			foreach (var option in wsOptions.Options)
			{
				if (option.IsEnabled)
				{
					var wsId = WritingSystemServices.GetMagicWsIdFromName(option.Id);
					// if the writing system isn't a magic name just use it
					if (wsId == 0)
					{
						return option.Id;
					}
					// otherwise get a list of the writing systems for the magic name, and use the first one
					return WritingSystemServices.GetWritingSystemList(cache, wsId, true).First().Id;
				}
			}
			// paranoid fallback to first option of the list in case there are no enabled options
			return wsOptions.Options[0].Id;
		}

		internal static DictionaryPublicationDecorator GetPublicationDecoratorAndEntries(IPropertyTable propertyTable, out int[] entriesToSave, string dictionaryType, LcmCache cache, IRecordList activeRecordList)
		{
			if (cache == null)
			{
				throw new ArgumentException(@"No cache", nameof(cache));
			}
			if (activeRecordList == null)
			{
				throw new ArgumentException(@"No record list", nameof(activeRecordList));
			}

			ICmPossibility currentPublication;
			var currentPublicationString = propertyTable.GetValue("SelectedPublication", xWorksStrings.AllEntriesPublication);
			if (currentPublicationString == xWorksStrings.AllEntriesPublication)
			{
				currentPublication = null;
			}
			else
			{
				currentPublication =
					(from item in cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS
					 where item.Name.UserDefaultWritingSystem.Text == currentPublicationString
					 select item).FirstOrDefault();
			}
			var decorator = new DictionaryPublicationDecorator(cache, activeRecordList.VirtualListPublisher, activeRecordList.VirtualFlid, currentPublication);
			entriesToSave = decorator.GetEntriesToPublish(propertyTable, activeRecordList.VirtualFlid, dictionaryType);
			return decorator;
		}

		/// <remarks>
		/// Presently, this handles only Sense Info, but if other info needs to be handed down the call stack in the future, we could rename this
		/// </remarks>
		private struct SenseInfo
		{
			internal int SenseCounter { get; set; }
			internal string SenseOutlineNumber { get; set; }
			internal string ParentSenseNumberingStyle { get; set; }
		}
	}
}
