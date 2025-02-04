// Copyright (c) 2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Icu.Collation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web.UI.WebControls;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// This class generates a json representation of a configured dictionary.
	/// The functions are driven by the ConfiguredLcmGenerator. The goal is to provide
	/// a robust implementation that will always generate correct .json given any <code>LCMCache</code> and
	/// <code>DictionaryConfigurationModel</code>
	/// </summary>
	/// <remarks>All methods in this class need to be thread safe</remarks>
	public class LcmJsonGenerator : ILcmContentGenerator
	{
		private LcmCache Cache { get; }
		private readonly ThreadLocal<StringBuilder> m_runBuilder = new ThreadLocal<StringBuilder>(()=> new StringBuilder());
		private Collator m_headwordWsCollator;
		public LcmJsonGenerator(LcmCache cache)
		{
			Cache = cache;
		}

		public IFragment GenerateWsPrefixWithString(ConfigurableDictionaryNode config, ConfiguredLcmGenerator.GeneratorSettings settings,
			bool displayAbbreviation, int wsId, IFragment content)
		{
			return content;
		}

		public IFragment GenerateAudioLinkContent(ConfigurableDictionaryNode config, string classname, string srcAttribute, string caption,
			string safeAudioId)
		{
			/*"audio": {
				"fileClass": "mos-Zxxx-x-audio",
				"id": "g635754050803599765ãadga",
				"src": "AudioVisual/635754050803599765ãadga.mp3"
			}*/
			dynamic audioObject = new JObject();
			audioObject.id = safeAudioId;
			audioObject.src = srcAttribute.Replace("\\", "/"); // expecting relative paths only
			return WriteProcessedObject(null, false, new StringFragment(audioObject.ToString()), "value");
		}

		public IFragment GenerateVideoLinkContent(ConfigurableDictionaryNode config, string className, string mediaId,
			string srcAttribute,
			string caption)
		{
			// TODO: Work out any required changes for webonary to play the video with the webonary team
			dynamic videoObject = new JObject();
			videoObject.id = mediaId;
			videoObject.src = srcAttribute.Replace("\\", "/"); // expecting relative paths only
			return WriteProcessedObject(null, false, new StringFragment(videoObject.ToString()), "value");
		}

		public IFragment WriteProcessedObject(ConfigurableDictionaryNode config, bool isBlock, IFragment elementContent, string className)
		{
			if (elementContent.ToString().StartsWith("{"))
				return WriteProcessedContents(elementContent, className, string.Empty, ",");

			((StringFragment)elementContent).TrimEnd(',');
			return WriteProcessedContents(elementContent, className, "{", "},");
		}

		public IFragment WriteProcessedCollection(ConfigurableDictionaryNode config, bool isBlock, IFragment elementContent, string className)
		{
			((StringFragment)elementContent).TrimEnd(',');
			return WriteProcessedContents(elementContent, className, "[", "],");
		}

		private IFragment WriteProcessedContents(IFragment elementContent, string className, string begin, string end)
		{
			if (elementContent.IsNullOrEmpty())
				return new StringFragment();

			var bldr = new StringBuilder();
			var fragment = new StringFragment(bldr);

			if (!string.IsNullOrEmpty(className))
			{
				bldr.Append($"\"{className}\": ");
			}

			bldr.Append(begin);
			bldr.Append(elementContent.ToString().TrimEnd(','));
			bldr.Append(end);
			return fragment;
		}

		public IFragment GenerateGramInfoBeforeSensesContent(IFragment content, ConfigurableDictionaryNode config)
		{
			// The grammatical info is generated as a json property on 'senses'
			return content;
		}

		public IFragment GenerateGroupingNode(ConfigurableDictionaryNode config, object field, string className, 
			DictionaryPublicationDecorator publicationDecorator, ConfiguredLcmGenerator.GeneratorSettings settings,
			Func<object, ConfigurableDictionaryNode, DictionaryPublicationDecorator, ConfiguredLcmGenerator.GeneratorSettings, IFragment> childContentGenerator)
		{
			//TODO: Decide how to handle grouping nodes in the json api
			return new StringFragment();
		}

		public IFragment AddCollectionItem(ConfigurableDictionaryNode config, bool isBlock, string className, IFragment content, bool first)
		{
			var fragment = new StringFragment();
			fragment.StrBuilder.Append(content.IsNullOrEmpty() ? string.Empty : $"{{{content}}},");
			return fragment;
		}

		public IFragment AddProperty(ConfigurableDictionaryNode config, ReadOnlyPropertyTable propTable, string className, bool isBlockProperty, string content, string writingSystem)
		{
			var fragment = new StringFragment($"\"{className}\": \"{content}\",");
			return fragment;
		}

		public IFragment CreateFragment()
		{
			return new StringFragment();
		}

		public IFragment CreateFragment(string str)
		{
			return new StringFragment(str);
		}

		public IFragmentWriter CreateWriter(IFragment bldr)
		{
			return new JsonFragmentWriter(((StringFragment)bldr).StrBuilder);
		}

		public void StartMultiRunString(IFragmentWriter writer, ConfigurableDictionaryNode config, string writingSystem)
		{
		}

		public void EndMultiRunString(IFragmentWriter writer)
		{
		}

		public void StartBiDiWrapper(IFragmentWriter writer, ConfigurableDictionaryNode config, bool rightToLeft)
		{
		}

		public void EndBiDiWrapper(IFragmentWriter writer)
		{
		}

		public void StartRun(IFragmentWriter writer, ConfigurableDictionaryNode config, ReadOnlyPropertyTable propTable, string writingSystem, bool first)
		{
			var jsonWriter = (JsonFragmentWriter)writer;
			jsonWriter.StartObject();
			jsonWriter.InsertJsonProperty("lang", writingSystem);
		}

		public void AddToRunContent(IFragmentWriter writer, string txtContent)
		{
			m_runBuilder.Value.Append(txtContent);
		}

		public void EndRun(IFragmentWriter writer)
		{
			((JsonFragmentWriter)writer).InsertJsonProperty("value", m_runBuilder.ToString());
			((JsonFragmentWriter)writer).EndObject();
			((JsonFragmentWriter)writer).InsertRawJson(",");
			m_runBuilder.Value.Clear();
		}

		public void SetRunStyle(IFragmentWriter writer, ConfigurableDictionaryNode config, ReadOnlyPropertyTable propertyTable, string writingSystem, string runStyle, bool error)
		{
			if (!string.IsNullOrEmpty(runStyle))
			{
				var cache = propertyTable.GetValue<LcmCache>("cache", null);
				var cssStyle = CssGenerator.GenerateCssStyleFromLcmStyleSheet(runStyle,
					cache.WritingSystemFactory.GetWsFromStr(writingSystem), propertyTable);
				string css = cssStyle?.ToString();
				if (!string.IsNullOrEmpty(css))
					((JsonFragmentWriter)writer).InsertJsonProperty("style", css);
			}
		}

		public void StartLink(IFragmentWriter writer, ConfigurableDictionaryNode config, Guid destination)
		{
			((JsonFragmentWriter)writer).InsertJsonProperty("guid", "g" + destination);
		}

		public void StartLink(IFragmentWriter writer, ConfigurableDictionaryNode config, string externalLink)
		{
			((JsonFragmentWriter)writer).InsertJsonProperty("linkUrl", externalLink);
		}

		public void EndLink(IFragmentWriter writer)
		{
		}

		public void AddLineBreakInRunContent(IFragmentWriter writer, ConfigurableDictionaryNode config)
		{
			m_runBuilder.Value.Append("\n");
		}

		public void StartTable(IFragmentWriter writer, ConfigurableDictionaryNode config)
		{
			// TODO: decide on a useful json representation for tables
		}

		public void AddTableTitle(IFragmentWriter writer, IFragment content)
		{
			// TODO: decide on a useful json representation for tables
		}

		public void StartTableBody(IFragmentWriter writer)
		{
			// TODO: decide on a useful json representation for tables
		}

		public void StartTableRow(IFragmentWriter writer)
		{
			// TODO: decide on a useful json representation for tables
		}

		public void AddTableCell(IFragmentWriter writer, bool isHead, int colSpan, HorizontalAlign alignment, IFragment content)
		{
			// TODO: decide on a useful json representation for tables
		}

		public void EndTableRow(IFragmentWriter writer)
		{
			// TODO: decide on a useful json representation for tables
		}

		public void EndTableBody(IFragmentWriter writer)
		{
			// TODO: decide on a useful json representation for tables
		}

		public void EndTable(IFragmentWriter writer, ConfigurableDictionaryNode config)
		{
			// TODO: decide on a useful json representation for tables
		}

		public void StartEntry(IFragmentWriter xw, ConfigurableDictionaryNode config, string className, Guid entryGuid, int index, RecordClerk clerk)
		{
			var jsonWriter = (JsonFragmentWriter)xw;
			jsonWriter.StartObject();
			jsonWriter.InsertJsonProperty("xhtmlTemplate", className);
			jsonWriter.InsertJsonProperty("guid", "g" + entryGuid);
			// get the index character (letter header) for this entry
			var entry = Cache.ServiceLocator.GetObject(entryGuid);
			var headwordWs = ConfiguredLcmGenerator.GetWsForEntryType(entry, Cache);

			if (!jsonWriter.collatorCache.TryGetValue(headwordWs, out var col))
			{
				col = FwUtils.GetCollatorForWs(headwordWs);

				jsonWriter.collatorCache[headwordWs] = col;
			}

			var indexChar = ConfiguredExport.GetLeadChar(
				ConfiguredLcmGenerator.GetSortWordForLetterHead(entry, clerk),
				headwordWs,
				new Dictionary<string, Dictionary<string, ConfiguredExport.CollationLevel>>(),
				new Dictionary<string, Dictionary<string, string>>(),
				new Dictionary<string, ISet<string>>(), col, Cache);
			jsonWriter.InsertJsonProperty("letterHead", indexChar);
			jsonWriter.InsertJsonProperty("sortIndex", index);
			jsonWriter.InsertRawJson(",");
		}

		public void AddEntryData(IFragmentWriter xw, List<ConfiguredLcmGenerator.ConfigFragment> pieces)
		{
			foreach (ConfiguredLcmGenerator.ConfigFragment piece in pieces)
				((JsonFragmentWriter)xw).InsertRawJson(piece.Frag);
		}

		public void EndEntry(IFragmentWriter xw)
		{
			((JsonFragmentWriter)xw).EndObject();
		}

		public void AddCollection(IFragmentWriter writer, ConfigurableDictionaryNode config, bool isBlockProperty, string className, IFragment content)
		{
			((JsonFragmentWriter)writer).InsertPropertyName(className);
			BeginArray(writer);
			WriteProcessedContents(writer, config, content);
			EndArray(writer);
		}

		private void BeginArray(IFragmentWriter writer)
		{
			((JsonFragmentWriter)writer).StartArray();
		}

		private void EndArray(IFragmentWriter writer)
		{
			((JsonFragmentWriter)writer).EndArray();
		}

		public void BeginObjectProperty(IFragmentWriter writer, ConfigurableDictionaryNode config, bool isBlockProperty,
			string className)
		{
			((JsonFragmentWriter)writer).InsertPropertyName(className);
			((JsonFragmentWriter)writer).StartObject();
		}

		public void EndObject(IFragmentWriter writer)
		{
			((JsonFragmentWriter)writer).EndObject();
		}

		public void WriteProcessedContents(IFragmentWriter writer, ConfigurableDictionaryNode config, IFragment contents)
		{
			if (!contents.IsNullOrEmpty())
			{
				// Try not to double up, but do try to end content with a ',' for building up objects
				string curStr = contents.ToString();
				StringBuilder bldr = ((StringFragment)contents).StrBuilder;
				bldr.Clear();
				bldr.Append(curStr.TrimEnd(',') + ",");
				((JsonFragmentWriter)writer).InsertRawJson(contents);
			}
		}

		public IFragment AddImage(ConfigurableDictionaryNode config, string classAttribute, string srcAttribute, string pictureGuid)
		{
			var bldr = new StringBuilder();
			var fragment = new StringFragment();
			fragment.StrBuilder = bldr;

			var sw = new StringWriter(bldr);
			using (var xw = new JsonTextWriter(sw))
			{
				xw.WritePropertyName("guid");
				xw.WriteValue("g" + pictureGuid);
				xw.WritePropertyName("src");
				xw.WriteValue(srcAttribute.Replace("\\", "/")); // expecting relative paths only
				xw.Flush();
				return fragment;
			}
		}

		public IFragment AddImageCaption(ConfigurableDictionaryNode config, IFragment captionContent)
		{
			return new StringFragment(captionContent.ToString());
		}

		public IFragment GenerateSenseNumber(ConfigurableDictionaryNode config, string formattedSenseNumber, string wsId)
		{
			return new StringFragment(formattedSenseNumber);
		}

		public IFragment AddLexReferences(ConfigurableDictionaryNode config, bool generateLexType, IFragment lexTypeContent, string className,
			IFragment referencesContent, bool typeBefore)
		{
			var bldr = new StringBuilder();
			var fragment = new StringFragment(bldr);

			var sw = new StringWriter(bldr);
			using (var xw = new JsonTextWriter(sw))
			{
				xw.WriteStartObject();
				// Write properties related to the factored type (if any and if before).
				if (generateLexType && typeBefore)
				{
					xw.WritePropertyName("referenceType");
					xw.WriteValue(lexTypeContent.ToString());
				}
				// Write an array with the references.
				xw.WritePropertyName("references");
				xw.WriteStartArray();
				xw.WriteRaw(referencesContent.ToString());
				xw.WriteEndArray();
				// Write properties related to the factored type (if any and if after).
				if (generateLexType && !typeBefore)
				{
					xw.WritePropertyName("referenceType");
					xw.WriteValue(lexTypeContent.ToString());
				}

				xw.WriteEndObject();
				xw.WriteRaw(",");
				xw.Flush();
				return fragment;
			}
		}

		public void BeginCrossReference(IFragmentWriter writer, ConfigurableDictionaryNode config, bool isBlockProperty, string classAttribute)
		{
			// In json the context is enough. We don't need the extra 'span' or 'div' with the item name
			// If the consumer needs to match up (to use our css) they can assume the child is the collection singular
			((JsonFragmentWriter)writer).StartObject();
		}

		public void EndCrossReference(IFragmentWriter writer)
		{
			EndObject(writer);
			((JsonFragmentWriter)writer).InsertRawJson(",");
		}

		public void BetweenCrossReferenceType(IFragment content, ConfigurableDictionaryNode node, bool firstItem)
		{
		}

		/// <summary>
		/// Generates data for all senses of an entry. For better processing of json add sharedGramInfo as a separate property object
		/// </summary>
		public IFragment WriteProcessedSenses(ConfigurableDictionaryNode config, bool isBlock, IFragment sensesContent, string classAttribute, IFragment sharedGramInfo)
		{
			return new StringFragment($"{sharedGramInfo.ToString()}{WriteProcessedCollection(config, isBlock, sensesContent, classAttribute)}");
		}

		public IFragment AddAudioWsContent(string wsId, Guid linkTarget, IFragment fileContent)
		{
			return new StringFragment($"{{\"guid\":\"g{linkTarget}\",\"lang\":\"{wsId}\",{fileContent}}}");
		}

		public IFragment GenerateErrorContent(StringBuilder badStrBuilder)
		{
			// We can't generate comments in json - But adding unicode tofu in front of the cleaned bad string should help
			// highlight the problem content without crashing the user or blocking the rest of the export
			return new StringFragment($"\\u+0FFF\\u+0FFF\\u+0FFF{badStrBuilder}");
		}

		public IFragment AddSenseData(ConfigurableDictionaryNode config, IFragment senseNumberSpan, Guid ownerGuid, IFragment senseContent, bool first)
		{
			var bldr = new StringBuilder();
			var fragment = new StringFragment(bldr);

			var sw = new StringWriter(bldr);
			using (var xw = new JsonTextWriter(sw))
			{
				xw.WriteStartObject();
				if (!senseNumberSpan.IsNullOrEmpty())
				{
					xw.WritePropertyName("senseNumber");
					xw.WriteValue(senseNumberSpan.ToString());
				}
				xw.WritePropertyName("guid");
				xw.WriteValue("g" + ownerGuid);
				xw.WriteRaw("," + senseContent.ToString().TrimEnd(','));
				xw.WriteEndObject();
				xw.WriteRaw(",");
				xw.Flush();

				return fragment;
			}
		}

		public class JsonFragmentWriter : IFragmentWriter
		{
			private JsonTextWriter jsonWriter;
			private StringWriter stringWriter;
			private bool isDisposed;
			internal Dictionary<string, Collator> collatorCache = new Dictionary<string, Collator>();

			public JsonFragmentWriter(StringBuilder bldr)
			{
				stringWriter = new StringWriter(bldr);
				jsonWriter = new JsonTextWriter(stringWriter);
			}

			public void Dispose()
			{
				foreach (var cachEntry in collatorCache.Values)
				{
					cachEntry?.Dispose();
				}
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			protected virtual void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
				if (!isDisposed)
				{
					jsonWriter.Close();
					stringWriter.Dispose();
					isDisposed = true;
				}
			}

			~JsonFragmentWriter()
			{
				Dispose(false);
			}

			public void Flush()
			{
				jsonWriter.Flush();
			}

			public void StartObject()
			{
				jsonWriter.WriteStartObject();
			}

			public void EndObject()
			{
				jsonWriter.WriteEndObject();
			}

			public void StartArray()
			{
				jsonWriter.WriteStartArray();
			}

			public void EndArray()
			{
				jsonWriter.WriteEndArray();
			}

			public void InsertPropertyName(string propName)
			{
				jsonWriter.WritePropertyName(propName);
			}

			public void InsertJsonProperty(string propName, string propValue)
			{
				jsonWriter.WritePropertyName(propName);
				jsonWriter.WriteValue(propValue);
			}

			public void InsertJsonProperty(string propName, int propValue)
			{
				jsonWriter.WritePropertyName(propName);
				jsonWriter.WriteValue(propValue);
			}

			public void InsertRawJson(string jsonContent)
			{
				jsonWriter.WriteRaw(jsonContent);
			}

			public void InsertRawJson(IFragment jsonContent)
			{
				jsonWriter.WriteRaw(jsonContent.ToString());
			}
		}

		/// <summary>
		/// This method will generate a list of arrays of json entries.
		/// Each array will contain the number of entries given to batchSize except possibly the last one.
		/// <remarks>
		/// Due to time constraints webonary version 1.5 will need the xhtml to display the entries correctly on Webonary.
		/// This is an engineering compromise and should be removed when work happens on webonary 2.0.
		/// The template which is being generated is the direction to move and the displayXhtml property should be deprecated.
		/// </remarks>
		/// </summary>
		/// <remarks>
		/// Webonary processes entries in parallel and therefore needs to have a sort index on each entry. Perhaps in the future, Webonary
		/// could index the entries after upload and before processing.
		/// </remarks>
		public static List<JArray> SavePublishedJsonWithStyles(int[] entriesToSave, DictionaryPublicationDecorator publicationDecorator, int batchSize,
			DictionaryConfigurationModel configuration, PropertyTable propertyTable, string jsonPath, IThreadedProgress progress, out int[] entryIds)
		{
			var entryCount = entriesToSave.Length;
			var cssPath = Path.ChangeExtension(jsonPath, "css");
			var cache = propertyTable.GetValue<LcmCache>("cache", null);
			var entryIdsList = new List<int>();

			// Don't display letter headers if we're showing a preview in the Edit tool or we're not sorting by headword
			using (var cssWriter = new StreamWriter(cssPath, false, Encoding.UTF8))
			{
				var readOnlyPropertyTable = new ReadOnlyPropertyTable(propertyTable);
				var settings = new ConfiguredLcmGenerator.GeneratorSettings(cache, readOnlyPropertyTable, true, true, Path.GetDirectoryName(jsonPath),
					ConfiguredLcmGenerator.IsEntryStyleRtl(readOnlyPropertyTable, configuration), Path.GetFileName(cssPath) == "configured.css") { ContentGenerator = new LcmJsonGenerator(cache)};
				settings.StylesGenerator.AddGlobalStyles(configuration, readOnlyPropertyTable);
				var displayXhtmlSettings = new ConfiguredLcmGenerator.GeneratorSettings(cache, readOnlyPropertyTable, true, true, Path.GetDirectoryName(jsonPath),
						ConfiguredLcmGenerator.IsEntryStyleRtl(readOnlyPropertyTable, configuration), Path.GetFileName(cssPath) == "configured.css");
				// Use the same StyleGenerator for both GeneratorSettings to prevent having two that
				// could contain different data for unique names. The unique names can be generated
				// in different orders.
				displayXhtmlSettings.StylesGenerator = settings.StylesGenerator;

				var entryContents = new Tuple<ICmObject, StringBuilder, StringBuilder>[entryCount];
				var entryActions = new List<Action>();
				// For every entry in the page generate an action that will produce the xhtml document fragment for that entry
				for (var i = 0; i < entryCount; ++i)
				{
					var index = i;
					var hvo = entriesToSave[i];
					var entry = cache.ServiceLocator.GetObject(hvo);
					var entryStringBuilder = new StringBuilder(100);
					var displayXhtmlBuilder = new StringBuilder(100);
					entryContents[i] = new Tuple<ICmObject, StringBuilder, StringBuilder>(entry, entryStringBuilder, displayXhtmlBuilder);

					var generateEntryAction = new Action(() =>
					{
						var entryContent = ConfiguredLcmGenerator.GenerateContentForEntry(entry, configuration,
							publicationDecorator, settings, index);
						entryStringBuilder.Append(entryContent);
						var displayXhtmlContent = ConfiguredLcmGenerator.GenerateContentForEntry(entry, configuration,
							publicationDecorator, displayXhtmlSettings, index);
						displayXhtmlBuilder.Append(displayXhtmlContent);
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
				var stringBuilder = new StringBuilder(100);
				// split the array into batch size chunks using fancy inscrutable linq
				var batchedContents = entryContents.Select((s, i) => entryContents.Skip(i * batchSize).Take(batchSize)).Where(a => a.Any());
				var generatedEntries = new List<JArray>();
				foreach (var batch in batchedContents)
				{
					using (var jsonStringWriter = new StringWriter(stringBuilder))
					using (var jsonWriter = new JsonTextWriter(jsonStringWriter))
					{
						jsonWriter.WriteStartArray();
						foreach (var entryData in batch.Where(ed => ed.Item2.Length > 0))
						{
							dynamic entryObject = JsonConvert.DeserializeObject(entryData.Item2.ToString());
							entryObject.displayXhtml = entryData.Item3.ToString();
							jsonWriter.WriteRaw(entryObject.ToString());
							jsonWriter.WriteRaw(",");
							entryIdsList.Add(entryData.Item1.Hvo);
						}
						jsonWriter.WriteEndArray();
						jsonWriter.Flush();
					}
					generatedEntries.Add((JArray)JsonConvert.DeserializeObject(stringBuilder.ToString(), new JsonSerializerSettings { Formatting = Formatting.None }));
					stringBuilder.Clear();
				}

				if (progress != null)
					progress.Message = xWorksStrings.ksGeneratingStyleInfo;

				cssWriter.Write(((CssGenerator)settings.StylesGenerator).GetStylesString());
				cssWriter.Flush();

				entryIds = entryIdsList.ToArray();
				return generatedEntries;
			}
		}

		public static JObject GenerateDictionaryMetaData(string siteName,
			IEnumerable<string> templateFileNames,
			IEnumerable<DictionaryConfigurationModel> reversals,
			int[] entryHvos,
			string exportPath,
			LcmCache cache,
			RecordClerk clerk)
		{
			dynamic dictionaryMetaData = new JObject();
			dictionaryMetaData._id = siteName;
			dynamic mainLanguageData = new JObject();
			mainLanguageData.title = cache.LangProject.DefaultVernacularWritingSystem.DisplayLabel;
			mainLanguageData.lang = cache.LangProject.DefaultVernacularWritingSystem.Id;
			//mainLanguageData.title = Enhance: Add new field to dialog for title?
			var wsString = cache.WritingSystemFactory.GetStrFromWs(cache.DefaultVernWs);
			mainLanguageData.letters = JArray.FromObject(GenerateLetterHeaders(entryHvos, cache, clerk, wsString));
			var customDictionaryCss = CssGenerator.CopyCustomCssAndGetPath(exportPath, cache, false);
			var cssFiles = new JArray();
			cssFiles.Add("configured.css");
			if (!string.IsNullOrEmpty(customDictionaryCss))
			{
				cssFiles.Add(Path.GetFileName(customDictionaryCss));
			}
			mainLanguageData.cssFiles = cssFiles;
			dictionaryMetaData.mainLanguage = mainLanguageData;
			if (reversals.Any())
			{
				var reversalArray = new JArray();
				foreach (var reversal in reversals)
				{
					dynamic revJson = new JObject();
					revJson.lang = reversal.WritingSystem;
					revJson.title = reversal.Label;
					var custReversalCss = CssGenerator.CopyCustomCssAndGetPath(exportPath,  cache, true);
					revJson.cssFiles = new JArray(new object[] { $"reversal_{reversal.WritingSystem}.css", Path.GetFileName(custReversalCss) });
					reversalArray.Add(revJson);
				}
				dictionaryMetaData.reversalLanguages = reversalArray;
			}

			dictionaryMetaData.partsOfSpeech = GenerateProjectOwnedList(cache.LangProject.PartsOfSpeechOA.ReallyReallyAllPossibilities,
				cache.LangProject.DefaultAnalysisWritingSystem.Handle, cache.LangProject.DefaultAnalysisWritingSystem.Id);
			dictionaryMetaData.semanticDomains = GenerateProjectOwnedList(cache.LangProject.SemanticDomainListOA.ReallyReallyAllPossibilities,
				cache.LangProject.DefaultAnalysisWritingSystem.Handle, cache.LangProject.DefaultAnalysisWritingSystem.Id);
			dictionaryMetaData.xhtmlTemplates = JArray.FromObject(templateFileNames);
			return dictionaryMetaData;
		}


		public static JArray GenerateReversalLetterHeaders(string siteName, string writingSystem, int[] entryIds, LcmCache cache, RecordClerk clerk)
		{
			return JArray.FromObject(GenerateLetterHeaders(entryIds, cache, clerk, writingSystem));
		}

		/// <summary>
		/// This method generates a JArray with one object for each list item.
		/// The object contains the guid, name, and abbreviation for the given ws.
		/// It is assumed there is no internal style that is needed when using a list for metadata.
		/// </summary>
		private static JArray GenerateProjectOwnedList(ISet<ICmPossibility> flattenedList, int wsId, string wsLabel)
		{
			var listArray = new JArray();
			foreach (var poss in flattenedList)
			{
				dynamic listItem = new JObject();
				var abbreviation = poss.Abbreviation.get_String(wsId);
				var name = poss.Name.get_String(wsId);
				listItem.lang = wsLabel;
				listItem.abbreviation = abbreviation.Text;
				listItem.name = name.Text;
				listItem.guid = "g" + poss.Guid;
				listArray.Add(listItem);
			}
			return listArray;
		}

		private static List<string> GenerateLetterHeaders(int[] entriesToSave, LcmCache cache, RecordClerk clerk, string wsString)
		{
			// These maps act as a cache to improve performance for discovering the index character for each headword
			var wsDigraphMap = new Dictionary<string, Dictionary<string, ConfiguredExport.CollationLevel>>();
			var wsCharEquivalentMap = new Dictionary<string, Dictionary<string, string>>();
			var wsIgnorableMap = new Dictionary<string, ISet<string>>();
			var letters = new List<string>();
			var col = FwUtils.GetCollatorForWs(wsString);

			foreach (var entryHvo in entriesToSave)
			{
				var entry = cache.ServiceLocator.GetObject(entryHvo);
				var firstLetter = ConfiguredExport.GetLeadChar(ConfiguredLcmGenerator.GetSortWordForLetterHead(entry, clerk),
					wsString, wsDigraphMap, wsCharEquivalentMap, wsIgnorableMap, col, cache);
				if (letters.Contains(firstLetter))
					continue;
				letters.Add(firstLetter);
			}
			// Dispose of the collator if we created one
			col?.Dispose();
			return letters;
		}
	}
}
