// Copyright (c) 2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SIL.FieldWorks.Common.Controls;
using SIL.LCModel;
using SIL.LCModel.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// This class generates a json representation of a configured dictionary.
	/// The functions are driven by the ConfiguredLcmGenerator. The goal is to provide
	/// a robust implementation that will always generate correct .json given any <code>LCMCache</code> and
	/// <code>DictionaryConfigurationModel</code>
	/// </summary>
	public class LcmJsonGenerator : ILcmContentGenerator
	{
		private LcmCache Cache { get; }
		public LcmJsonGenerator(LcmCache cache)
		{
			Cache = cache;
		}

		public string GenerateWsPrefixWithString(ConfiguredLcmGenerator.GeneratorSettings settings,
			bool displayAbbreviation, int wsId, string content)
		{
			return content;
		}

		public string GenerateAudioLinkContent(string classname, string srcAttribute, string caption,
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
			return WriteProcessedObject(false, audioObject.ToString(), "value");
		}

		public string WriteProcessedObject(bool isBlock, string elementContent, string className)
		{
			if (elementContent.StartsWith("{"))
				return WriteProcessedContents(elementContent, className, string.Empty, ",");
			return WriteProcessedContents(elementContent.TrimEnd(','), className, "{", "},");
		}

		public string WriteProcessedCollection(bool isBlock, string elementContent, string className)
		{
			return WriteProcessedContents(elementContent.TrimEnd(','), className, "[", "],");
		}

		private string WriteProcessedContents(string elementContent, string className, string begin, string end)
		{
			if (string.IsNullOrEmpty(elementContent))
				return string.Empty;
			var bldr = new StringBuilder();
			if (!string.IsNullOrEmpty(className))
			{
				bldr.Append($"\"{className}\": ");
			}

			bldr.Append(begin);
			bldr.Append(elementContent.TrimEnd(','));
			bldr.Append(end);
			return bldr.ToString();
		}

		public string GenerateGramInfoBeforeSensesContent(string content)
		{
			// The grammatical info is generated as a json property on 'senses'
			return $"{content}";
		}

		public string GenerateGroupingNode(object field, ConfigurableDictionaryNode config,
			DictionaryPublicationDecorator publicationDecorator, ConfiguredLcmGenerator.GeneratorSettings settings,
			Func<object, ConfigurableDictionaryNode, DictionaryPublicationDecorator, ConfiguredLcmGenerator.GeneratorSettings, string> childContentGenerator)
		{
			//TODO: Decide how to handle grouping nodes in the json api
			return string.Empty;
		}

		public string AddCollectionItem(bool isBlock, string className, string content)
		{
			return $"{{{content}}},";
		}

		public string AddProperty(string className, bool isBlockProperty, string content)
		{
			return $"\"{className}\": \"{content}\",";
		}

		public IFragmentWriter CreateWriter(StringBuilder bldr)
		{
			return new JsonFragmentWriter(bldr);
		}

		public void StartMultiRunString(IFragmentWriter writer, string writingSystem)
		{
		}

		public void EndMultiRunString(IFragmentWriter writer)
		{
		}

		public void StartBiDiWrapper(IFragmentWriter writer, bool rightToLeft)
		{
			((JsonFragmentWriter)writer).InsertJsonProperty("rtl", rightToLeft.ToString());
		}

		public void EndBiDiWrapper(IFragmentWriter writer)
		{
		}

		public void StartRun(IFragmentWriter writer, string writingSystem)
		{
			var jsonWriter = (JsonFragmentWriter)writer;
			jsonWriter.StartObject();
			jsonWriter.InsertJsonProperty("lang", writingSystem);
		}

		public void AddToRunContent(IFragmentWriter writer, string txtContent)
		{
			((JsonFragmentWriter)writer).InsertJsonProperty("value", txtContent);
		}

		public void EndRun(IFragmentWriter writer)
		{
			((JsonFragmentWriter)writer).EndObject();
			((JsonFragmentWriter)writer).InsertRawJson(",");
		}

		public void SetRunStyle(IFragmentWriter writer, string css)
		{
			if(!string.IsNullOrEmpty(css))
				((JsonFragmentWriter)writer).InsertJsonProperty("style", css);
		}

		public void BeginLink(IFragmentWriter writer, Guid destination)
		{
			((JsonFragmentWriter)writer).InsertJsonProperty("guid", destination.ToString());
		}

		public void EndLink(IFragmentWriter writer)
		{
		}

		public void AddLineBreakInRunContent(IFragmentWriter writer)
		{
			throw new NotImplementedException("Line breaks in strings aren't supported in the json generator yet.");
		}

		public void BeginEntry(IFragmentWriter xw, string className, Guid entryGuid)
		{
			var jsonWriter = (JsonFragmentWriter)xw;
			jsonWriter.StartObject();
			jsonWriter.InsertJsonProperty("xhtmlTemplate", className);
			jsonWriter.InsertJsonProperty("guid", entryGuid.ToString());
			// get the index character (letter header) for this entry
			var entry = Cache.ServiceLocator.GetObject(entryGuid);
			var indexChar = ConfiguredExport.GetLeadChar(ConfiguredLcmGenerator.GetHeadwordForLetterHead(entry),
				ConfiguredLcmGenerator.GetWsForEntryType(entry, Cache),
				new Dictionary<string, ISet<string>>(),
				new Dictionary<string, Dictionary<string, string>>(),
				new Dictionary<string, ISet<string>>(), Cache);
			jsonWriter.InsertJsonProperty("letterHead", indexChar);
			jsonWriter.InsertRawJson(",");
		}

		public void AddEntryData(IFragmentWriter xw, List<string> pieces)
		{
			pieces.ForEach(((JsonFragmentWriter)xw).InsertRawJson);
		}

		public void EndEntry(IFragmentWriter xw)
		{
			((JsonFragmentWriter)xw).EndObject();
		}

		public void AddCollection(IFragmentWriter writer, bool isBlockProperty, string className, string content)
		{
			((JsonFragmentWriter)writer).InsertPropertyName(className);
			BeginArray(writer);
			WriteProcessedContents(writer, content);
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

		public void BeginObjectProperty(IFragmentWriter writer, bool isBlockProperty,
			string className)
		{
			((JsonFragmentWriter)writer).InsertPropertyName(className);
			((JsonFragmentWriter)writer).StartObject();
		}

		public void EndObject(IFragmentWriter writer)
		{
			((JsonFragmentWriter)writer).EndObject();
		}

		public void WriteProcessedContents(IFragmentWriter writer, string contents)
		{
			// Try not to double up, but do try to end content with a ',' for building up objects
			((JsonFragmentWriter)writer).InsertRawJson(contents.TrimEnd(',') + ",");
		}

		public string AddImage(string classAttribute, string srcAttribute, string pictureGuid)
		{
			var bldr = new StringBuilder();
			var sw = new StringWriter(bldr);
			using (var xw = new JsonTextWriter(sw))
			{
				xw.WritePropertyName("guid");
				xw.WriteValue(pictureGuid);
				xw.WritePropertyName("src");
				xw.WriteValue(srcAttribute.Replace("\\", "/")); // expecting relative paths only
				xw.Flush();
				return bldr.ToString();
			}
		}

		public string AddImageCaption(string captionContent)
		{
			return captionContent;
		}

		public string GenerateSenseNumber(string formattedSenseNumber)
		{
			return formattedSenseNumber;
		}

		public string AddLexReferences(bool generateLexType, string lexTypeContent, string className,
			string referencesContent)
		{
			var bldr = new StringBuilder();
			var sw = new StringWriter(bldr);
			using (var xw = new JsonTextWriter(sw))
			{
				xw.WriteStartObject();
				// Write properties related to the factored type (if any).
				if (!generateLexType)
				{
					xw.WritePropertyName("referenceType");
					xw.WriteValue(lexTypeContent);
				}
				// Write an array with the references.
				xw.WritePropertyName("references");
				xw.WriteStartArray();
				xw.WriteRaw(referencesContent);
				xw.WriteEndArray();
				xw.WriteEndObject();
				xw.WriteRaw(",");
				xw.Flush();
				return bldr.ToString();
			}
		}

		public void BeginCrossReference(IFragmentWriter writer, bool isBlockProperty, string classAttribute)
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

		/// <summary>
		/// Generates data for all senses of an entry. For better processing of json add sharedGramInfo as a separate property object
		/// </summary>
		public string WriteProcessedSenses(bool isBlock, string sensesContent, string classAttribute, string sharedGramInfo)
		{
			return $"{sharedGramInfo}{WriteProcessedCollection(isBlock, sensesContent, classAttribute)}";
		}

		public string AddAudioWsContent(string wsId, Guid linkTarget, string fileContent)
		{
			return $"{{\"guid\":\"{linkTarget}\",\"lang\":\"{wsId}\",{fileContent}}}";
		}

		public string AddSenseData(string senseNumberSpan, bool isBlock, Guid ownerGuid,
			string senseContent, string className)
		{
			var bldr = new StringBuilder();
			var sw = new StringWriter(bldr);
			using (var xw = new JsonTextWriter(sw))
			{
				xw.WriteStartObject();
				if (!string.IsNullOrEmpty(senseNumberSpan))
				{
					xw.WritePropertyName("senseNumber");
					xw.WriteValue(senseNumberSpan);
				}
				xw.WritePropertyName("guid");
				xw.WriteValue(ownerGuid);
				xw.WriteRaw("," + senseContent.TrimEnd(','));
				xw.WriteEndObject();
				xw.WriteRaw(",");
				xw.Flush();
				return bldr.ToString();
			}
		}

		public class JsonFragmentWriter : IFragmentWriter
		{
			private JsonTextWriter jsonWriter;
			private StringWriter stringWriter;
			private bool isDisposed;

			public JsonFragmentWriter(StringBuilder bldr)
			{
				stringWriter = new StringWriter(bldr);
				jsonWriter = new JsonTextWriter(stringWriter);
			}

			public void Dispose()
			{
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

			public void InsertRawJson(string jsonContent)
			{
				jsonWriter.WriteRaw(jsonContent);
			}
		}

		/// <summary>
		/// This method will generate a list of arrays of json entries.
		/// Each array will contain the number of entries given to batchSize except possibly the last one.
		/// <remarks>
		/// Due to time constraints webonary version 1.5 will need the xhtml to display the entries correctly on Webonary.
		/// This is an engineering compromise and should be removed when work happens on webonary 2.0
		/// The template which is being generated is the direction to move and the displayXhtml property should be deprecated.
		/// </remarks>
		/// </summary>
		public static List<JArray> SavePublishedJsonWithStyles(int[] entriesToSave, DictionaryPublicationDecorator publicationDecorator, int batchSize,
			DictionaryConfigurationModel configuration, PropertyTable propertyTable, string jsonPath, IThreadedProgress progress)
		{
			var entryCount = entriesToSave.Length;
			var cssPath = Path.ChangeExtension(jsonPath, "css");
			var cache = propertyTable.GetValue<LcmCache>("cache", null);
			// Don't display letter headers if we're showing a preview in the Edit tool or we're not sorting by headword
			using (var cssWriter = new StreamWriter(cssPath, false, Encoding.UTF8))
			{
				var readOnlyPropertyTable = new ReadOnlyPropertyTable(propertyTable);
				var settings = new ConfiguredLcmGenerator.GeneratorSettings(cache, readOnlyPropertyTable, true, true, Path.GetDirectoryName(jsonPath),
					ConfiguredLcmGenerator.IsNormalRtl(readOnlyPropertyTable), Path.GetFileName(cssPath) == "configured.css") { ContentGenerator = new LcmJsonGenerator(cache)};
				var displayXhtmlSettings = new ConfiguredLcmGenerator.GeneratorSettings(cache, readOnlyPropertyTable, true, true, Path.GetDirectoryName(jsonPath),
						ConfiguredLcmGenerator.IsNormalRtl(readOnlyPropertyTable), Path.GetFileName(cssPath) == "configured.css");
				var entryContents = new Tuple<ICmObject, StringBuilder, StringBuilder>[entryCount];
				var entryActions = new List<Action>();
				// For every entry in the page generate an action that will produce the xhtml document fragment for that entry
				for (var i = 0; i < entryCount; ++i)
				{
					var hvo = entriesToSave[i];
					var entry = cache.ServiceLocator.GetObject(hvo);
					var entryStringBuilder = new StringBuilder(100);
					var displayXhtmlBuilder = new StringBuilder(100);
					entryContents[i] = new Tuple<ICmObject, StringBuilder, StringBuilder>(entry, entryStringBuilder, displayXhtmlBuilder);

					var generateEntryAction = new Action(() =>
					{
						var entryContent = ConfiguredLcmGenerator.GenerateXHTMLForEntry(entry, configuration,
							publicationDecorator, settings);
						entryStringBuilder.Append(entryContent);
						var displayXhtmlContent = ConfiguredLcmGenerator.GenerateXHTMLForEntry(entry, configuration,
							publicationDecorator, displayXhtmlSettings);
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
				// TODO: Do this in a loop for the entries array split into batchSize
				using (var jsonStringWriter = new StringWriter(stringBuilder))
				using (var jsonWriter = new JsonTextWriter(jsonStringWriter))
				{
					jsonWriter.WriteStartArray();
					foreach (var entryData in entryContents)
					{
						dynamic entryObject = JsonConvert.DeserializeObject(entryData.Item2.ToString());
						entryObject.displayXhtml = entryData.Item3.ToString();
						jsonWriter.WriteRaw(entryObject.ToString());
						jsonWriter.WriteRaw(",");
					}
					jsonWriter.WriteEndArray();
					jsonWriter.Flush();
				}
				var expected = (JArray)JsonConvert.DeserializeObject(stringBuilder.ToString(), new JsonSerializerSettings { Formatting = Newtonsoft.Json.Formatting.None });

				if (progress != null)
					progress.Message = xWorksStrings.ksGeneratingStyleInfo;

				cssWriter.Write(CssGenerator.GenerateCssFromConfiguration(configuration, readOnlyPropertyTable));
				cssWriter.Flush();
				return new List<JArray> {expected};
			}
		}

		public static JObject GenerateDictionaryMetaData(string siteName,
			IEnumerable<string> tempateFileNames,
			IEnumerable<DictionaryConfigurationModel> reversals,
			int[] entryHvos,
			LcmCache cache)
		{
			dynamic dictionaryMetaData = new JObject();
			dictionaryMetaData._id = siteName;
			dynamic mainLanguageData = new JObject();
			mainLanguageData.lang = cache.LangProject.DefaultAnalysisWritingSystem.Id;
			//mainLanguageData.title = Enhance: Add new field to dialog for title?
			mainLanguageData.letters = JArray.FromObject(GenerateLetterHeaders(entryHvos, cache));
			dictionaryMetaData.mainLanguage = mainLanguageData;
			if (reversals.Any())
			{
				var reversalArray = new JArray();
				foreach (var reversal in reversals)
				{
					dynamic revJson = new JObject();
					revJson.lang = reversal.WritingSystem;
					revJson.title = reversal.Label;
					revJson.letters = new JArray(); // Generate letter headers from reversal and pass to this method
					revJson.cssFiles = new JArray(new[] { "reversal_lang.css" });
					reversalArray.Add(revJson);
				}
				dictionaryMetaData.reversalLanguages = reversalArray;
			}

			dictionaryMetaData.partsOfSpeech = GenerateProjectOwnedList(cache.LangProject.PartsOfSpeechOA.ReallyReallyAllPossibilities,
				cache.LangProject.DefaultAnalysisWritingSystem.Handle, cache.LangProject.DefaultAnalysisWritingSystem.Id);
			dictionaryMetaData.semanticDomains = GenerateProjectOwnedList(cache.LangProject.SemanticDomainListOA.ReallyReallyAllPossibilities,
				cache.LangProject.DefaultAnalysisWritingSystem.Handle, cache.LangProject.DefaultAnalysisWritingSystem.Id);
			dictionaryMetaData.xhtmlTemplates = JArray.FromObject(tempateFileNames);
			return dictionaryMetaData;
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
				listItem.guid = poss.Guid;
				listArray.Add(listItem);
			}
			return listArray;
		}

		private static List<string> GenerateLetterHeaders(int[] entriesToSave, LcmCache cache)
		{
			// These maps act as a cache to improve performance for discovering the index character for each headword
			var wsDigraphMap = new Dictionary<string, ISet<string>>();
			var wsCharEquivalentMap = new Dictionary<string, Dictionary<string, string>>();
			var wsIngorableMap = new Dictionary<string, ISet<string>>();
			var wsString = cache.WritingSystemFactory.GetStrFromWs(cache.DefaultVernWs);
			var letters = new List<string>();
			foreach (var entryHvo in entriesToSave)
			{
				var entry = cache.ServiceLocator.GetObject(entryHvo);
				var firstLetter = ConfiguredExport.GetLeadChar(((ILexEntry)entry).HomographForm, wsString, wsDigraphMap, wsCharEquivalentMap, wsIngorableMap,
					cache);
				if (letters.Contains(firstLetter))
					continue;
				letters.Add(firstLetter);
			}
			return letters;
		}
	}
}
