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
using DocumentFormat.OpenXml;
using Icu.Collation;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Utils;
using XCore;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace SIL.FieldWorks.XWorks
{
	// This alias is to be used when creating Wordprocessing Text objects,
	// since there are multiple different Text types across the packages we are using.
	using WP = DocumentFormat.OpenXml.Wordprocessing;

	public class LcmWordGenerator : ILcmContentGenerator, ILcmStylesGenerator
	{
		private LcmCache Cache { get; }
		public LcmWordGenerator(LcmCache cache)
		{
			Cache = cache;
		}

		public static void SavePublishedDocx(int[] entryHvos, DictionaryPublicationDecorator publicationDecorator, int batchSize, DictionaryConfigurationModel configuration,
			XCore.PropertyTable propertyTable, string filePath, IThreadedProgress progress = null)
		{
			using (MemoryStream mem = new MemoryStream())
			{
				DocFragment fragment = new DocFragment(mem);

				var entryCount = entryHvos.Length;
				var cssPath = System.IO.Path.ChangeExtension(filePath, "css");
				var clerk = propertyTable.GetValue<RecordClerk>("ActiveClerk", null);
				var cache = propertyTable.GetValue<LcmCache>("cache", null);


				var readOnlyPropertyTable = new ReadOnlyPropertyTable(propertyTable);
				var settings = new ConfiguredLcmGenerator.GeneratorSettings(cache, readOnlyPropertyTable, true, true, System.IO.Path.GetDirectoryName(filePath),
							ConfiguredLcmGenerator.IsEntryStyleRtl(readOnlyPropertyTable, configuration), System.IO.Path.GetFileName(cssPath) == "configured.css")
							{ ContentGenerator = new LcmWordGenerator(cache) };
				string lastHeader = null;
				var entryContents = new Tuple<ICmObject, IFragment>[entryCount];
				var entryActions = new List<Action>();

				// For every entry generate an action that will produce the doc fragment for that entry
				for (var i = 0; i < entryCount; ++i)
				{
					var hvo = entryHvos.ElementAt(i);
					var entry = cache.ServiceLocator.GetObject(hvo);
					var entryStringBuilder = new DocFragment();
					entryContents[i] = new Tuple<ICmObject, IFragment>(entry, entryStringBuilder);

					var generateEntryAction = new Action(() =>
					{
						var entryContent = ConfiguredLcmGenerator.GenerateContentForEntry(entry, configuration, publicationDecorator, settings);
						entryStringBuilder.Append(entryContent);
						if (progress != null)
							progress.Position++;
					});

					entryActions.Add(generateEntryAction);
				}

				// Generate all the document fragments (in parallel)
				if (progress != null)
					progress.Message = xWorksStrings.ksGeneratingDisplayFragments;
				ConfiguredLcmGenerator.SpawnEntryGenerationThreadsAndWait(entryActions, progress);

				// Generate the letter headers and insert the document fragments into the full file
				if (progress != null)
					progress.Message = xWorksStrings.ksArrangingDisplayFragments;
				var wsString = entryContents.Length > 0 ? ConfiguredLcmGenerator.GetWsForEntryType(entryContents[0].Item1, settings.Cache) : null;
				var col = FwUtils.GetCollatorForWs(wsString);

				foreach (var entry in entryContents)
				{
					if (!entry.Item2.IsNullOrEmpty())
					{
						IFragment letterHeader = GenerateLetterHeaderIfNeeded(entry.Item1,
							ref lastHeader, col, settings, clerk);

						// If needed, append letter header to the word doc
						if (!letterHeader.IsNullOrEmpty())
							fragment.Append(letterHeader);

						// TODO: when/how are styles applied to the letter headers?
						// Append the entry to the word doc
						fragment.Append(entry.Item2);
					}
				}
				col?.Dispose();

				if (progress != null)
					progress.Message = xWorksStrings.ksGeneratingStyleInfo;

				// TODO: Generate styles

				fragment.DocFrag.Dispose();

				// Create mode will overwrite any existing document at the given filePath;
				// this is expected behavior that the user is warned about
				// if they choose to export to an existing file.
				using (FileStream fileStream = new FileStream(filePath, System.IO.FileMode.Create))
				{
					mem.WriteTo(fileStream);
				}

			}
		}

		internal static IFragment GenerateLetterHeaderIfNeeded(ICmObject entry, ref string lastHeader, Collator headwordWsCollator, ConfiguredLcmGenerator.GeneratorSettings settings, RecordClerk clerk = null)
		{
			StringBuilder headerTextBuilder = ConfiguredLcmGenerator.GenerateLetterHeaderIfNeeded(entry, ref lastHeader,
				headwordWsCollator, settings, clerk);

			return new DocFragment(headerTextBuilder.ToString());

		}

		// ILcmStylesGenerator functions to implement
		public void AddGlobalStyles(DictionaryConfigurationModel model, ReadOnlyPropertyTable propertyTable)
		{
			//TODO
			return;
		}

		public string AddStyles(ConfigurableDictionaryNode node)
		{
			// TODO
			return "TODO: AddStyles";
		}

		public void Init(ReadOnlyPropertyTable propertyTable)
		{
			// TODO
			return;
		}

		// ILcmContentGenerator functions to implement
		public IFragment GenerateWsPrefixWithString(ConfiguredLcmGenerator.GeneratorSettings settings,
			bool displayAbbreviation, int wsId, IFragment content)
		{
			return content;
		}

		public IFragment GenerateAudioLinkContent(string classname, string srcAttribute, string caption, string safeAudioId)
		{
			// TODO
			return new DocFragment("TODO: generate audio link content");
		}
		public IFragment WriteProcessedObject(bool isBlock, IFragment elementContent, string className)
		{
			return WriteProcessedContents(elementContent, className);
		}
		public IFragment WriteProcessedCollection(bool isBlock, IFragment elementContent, string className)
		{
			return WriteProcessedContents(elementContent, className);
		}

		private IFragment WriteProcessedContents(IFragment elementContent, string className)
		{
			// TODO:
			// Currently we don't use the class name here.
			// We don't want to write the class name to the document,
			// but we may use it to set styles.
			// Do we need write it here, for it to be used when determining style?

			if (elementContent.IsNullOrEmpty())
				return new DocFragment();

			return elementContent;
		}

		public IFragment GenerateGramInfoBeforeSensesContent(IFragment content)
		{
			return content;
		}
		public IFragment GenerateGroupingNode(object field, string className, ConfigurableDictionaryNode config, DictionaryPublicationDecorator publicationDecorator, ConfiguredLcmGenerator.GeneratorSettings settings,
			Func<object, ConfigurableDictionaryNode, DictionaryPublicationDecorator, ConfiguredLcmGenerator.GeneratorSettings, IFragment> childContentGenerator)
		{
			//TODO: handle grouping nodes
			return new DocFragment("TODO: handle grouping nodes");
		}

		public IFragment AddSenseData(IFragment senseNumberSpan, bool isBlockProperty, Guid ownerGuid, string senseContent, string className)
		{
			var senseCont = new DocFragment(senseContent);
			// Add sense numbers if needed
			if (!senseNumberSpan.IsNullOrEmpty())
			{
				senseNumberSpan.Append(senseCont);
				return senseNumberSpan;
			}

			return senseCont;
		}
		public IFragment AddCollectionItem(bool isBlock, string collectionItemClass, IFragment content)
		{
			return content.IsNullOrEmpty() ? new DocFragment() : content;
		}
		public IFragment AddProperty(string className, bool isBlockProperty, string content)
		{
			return new DocFragment(content);
		}

		public IFragment CreateFragment()
		{
			return new DocFragment();
		}

		public IFragment CreateFragment(string str)
		{
			return new DocFragment(str);
		}

		public class DocFragment : IFragment
		{
			internal MemoryStream MemStr { get; }
			internal WordprocessingDocument DocFrag { get; }
			internal Body DocBody { get; }

			/// <summary>
			/// Constructs a new memory stream and creates an empty doc fragment
			/// that writes to that stream.
			/// </summary>
			public DocFragment()
			{
				MemStr = new MemoryStream();
				DocFrag = WordprocessingDocument.Open(MemStr, true);

				// Initialize the document and body.
				MainDocumentPart mainDocPart = DocFrag.AddMainDocumentPart();
				mainDocPart.Document = new WP.Document();
				DocBody = mainDocPart.Document.AppendChild(new WP.Body());
			}

			/// <summary>
			/// Initializes the memory stream from the argument and creates
			/// an empty doc fragment that writes to that stream.
			/// </summary>
			public DocFragment(MemoryStream str)
			{
				MemStr = str;
				DocFrag = WordprocessingDocument.Open(str, true);

				// Initialize the document and body.
				MainDocumentPart mainDocPart = DocFrag.AddMainDocumentPart();
				mainDocPart.Document = new WP.Document();
				DocBody = mainDocPart.Document.AppendChild(new WP.Body());
			}

			/// <summary>
			/// Constructs a new memory stream and creates a non-empty doc fragment,
			/// containing the given string, that writes to that stream.
			/// </summary>
			public DocFragment(string str) : this()
			{
				// Add text to the fragment
				Paragraph para = DocBody.AppendChild(new Paragraph());
				Run run = para.AppendChild(new Run());

				if (!string.IsNullOrEmpty(str))
				{
					// For spaces to show correctly, set preserve spaces on the text element
					WP.Text txt = new WP.Text(str);
					txt.Space = SpaceProcessingModeValues.Preserve;
					run.AppendChild(txt);
				}
				else
				{
					// For spaces to show correctly, set preserve spaces on the text element
					WP.Text txt = new WP.Text(String.Empty);
					txt.Space = SpaceProcessingModeValues.Preserve;
					run.AppendChild(txt);
				}
			}

			/// <summary>
			/// Returns content of the doc fragment as a string.
			/// Be careful using this as document styles won't be preserved in a string.
			/// This function is primarily used inside the Length() function
			/// to check the length of text in a doc fragment.
			/// </summary>
			public override string ToString()
			{
				if (IsNullOrEmpty())
				{
					return string.Empty;
				}

				return ToString(DocBody);
			}

			private string ToString(OpenXmlElement textBody)
			{
				var FragStr = new StringBuilder();
				foreach (var docSection in textBody.Elements())
				{
					switch (docSection.LocalName)
					{
						// Text
						case "t":
							FragStr.Append(docSection.InnerText);
							break;

						// Carriage return/page break
						case "cr":
						case "br":
							FragStr.AppendLine();
							break;

						// Tab
						case "tab":
							FragStr.Append("\t");
							break;

						// Paragraph
						case "p":
							FragStr.Append(ToString(docSection));
							FragStr.AppendLine();
							break;

						default:
							FragStr.Append(ToString(docSection));
							break;
					}
				}

				return FragStr.ToString();
			}

			public int Length()
			{
				string str = ToString();
				return str.Length;
			}

			/// <summary>
			/// Appends one doc fragment to another.
			/// Use this if styles have already been applied
			/// and if not attempting to append within the same paragraph.
			/// </summary>
			public void Append(IFragment frag)
			{

				foreach (Paragraph para in ((DocFragment)frag).DocBody.OfType<Paragraph>().ToList())
				{
					// Append each paragraph. It is necessary to deep clone the node to maintain its tree of document properties
					// and to ensure its styles will be maintained in the copy.
					this.DocBody.AppendChild(para.CloneNode(true));
				}
			}

			/// <summary>
			/// Appends a new run inside the last paragraph of the doc fragment.
			/// The run will be added to the end of the paragraph.
			/// </summary>
			public void Append(Run run)
			{
				// Deep clone the run b/c of its tree of properties and to maintain styles.
				Paragraph lastPar = GetLastParagraph();
				lastPar.AppendChild(run.CloneNode(true));
			}

			public void AppendBreak()
			{
				// Breaks are automatically added between different paragraphs.
				// A null op here is sufficient, unless we want line breaks within a paragraph or run.
			}

			public void AppendSpace()
			{
				Run lastRun = GetLastRun();
				WP.Text txt = new WP.Text(" ");
				// For spaces to show correctly, set preserve spaces on the text element
				txt.Space = SpaceProcessingModeValues.Preserve;
				lastRun.AppendChild(txt);
			}

			public bool IsNullOrEmpty()
			{
				// A docbody with no children is an empty document.
				if (MemStr == null || DocFrag == null || DocBody == null || !DocBody.HasChildren)
				{
					return true;
				}
				return false;
			}

			public void Clear()
			{
				// Clear() method is not used for the word generator.
				throw new NotImplementedException();
			}

			/// <summary>
			/// Returns last paragraph in the document if it contains any,
			/// else creates and returns a new paragraph.
			/// </summary>
			public Paragraph GetLastParagraph()
			{
				List<Paragraph> parList = DocBody.OfType<Paragraph>().ToList();
				if (parList.Any())
					return parList.Last();
				return GetNewParagraph();
			}

			/// <summary>
			/// Creates and returns a new paragraph.
			/// </summary>
			public Paragraph GetNewParagraph()
			{
				Paragraph newPar = DocBody.AppendChild(new Paragraph());
				return newPar;
			}

			/// <summary>
			/// Returns last run in the document if it contains any,
			/// else creates and returns a new run.
			/// </summary>
			private Run GetLastRun()
			{
				Paragraph lastPara = GetLastParagraph();
				List<Run> runList = lastPara.OfType<Run>().ToList();
				if (runList.Any())
					return runList.Last();

				return lastPara.AppendChild(new Run());
			}
		}

		public IFragmentWriter CreateWriter(IFragment frag)
		{
			return new WordFragmentWriter((DocFragment)frag);
		}

		public class WordFragmentWriter : IFragmentWriter
		{
			public DocFragment WordFragment { get; }
			private bool isDisposed;
			internal Dictionary<string, Collator> collatorCache = new Dictionary<string, Collator>();

			public WordFragmentWriter(DocFragment frag)
			{
				WordFragment = frag;
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
					WordFragment.DocFrag.Dispose();
					WordFragment.MemStr.Dispose();
					isDisposed = true;
				}
			}

			public void Flush()
			{
				WordFragment.MemStr.Flush();
			}

			public void Insert(IFragment frag)
			{
				WordFragment.Append(frag);
			}

			public void Insert(Run run)
			{
				WordFragment.Append(run);
			}

			/// <summary>
			/// Gets and returns the last run in the document, if one exists.
			/// Otherwise, creates and returns a new run.
			/// </summary>
			public Run GetCurrentRun()
			{
				List<Run> runList = WordFragment.DocBody.Descendants<Run>().ToList();
				if (runList.Any())
					return runList.Last();

				// If there is no run, create one
				Run lastRun = WordFragment.DocBody.AppendChild(new Run());
				return lastRun;
			}

			/// <summary>
			/// Get the last paragraph in the doc if it contains any,
			/// and add a new run to it.
			/// Else, create and add the run to a new paragraph.
			/// </summary>
			public void CreateRun()
			{
				Paragraph curPar = WordFragment.GetLastParagraph();
				curPar.AppendChild(new Run());
			}

			/*public void AddStyleToRun()
			{
				// Grab the latest run and add a style
				Run lastRun = GetCurrentRun();

				// TODO: add style

			}*/
		}

		public void StartMultiRunString(IFragmentWriter writer, string writingSystem)
		{
			return;
		}
		public void EndMultiRunString(IFragmentWriter writer)
		{
			return;
		}
		public void StartBiDiWrapper(IFragmentWriter writer, bool rightToLeft)
		{
			return;
		}
		public void EndBiDiWrapper(IFragmentWriter writer)
		{
			return;
		}

		/// <summary>
		/// Creates a new run that is appended to the doc's last paragraph,
		/// if one exists, or to a new paragraph otherwise.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="writingSystem"></param>
		public void StartRun(IFragmentWriter writer, string writingSystem)
		{
			((WordFragmentWriter)writer).CreateRun();
		}
		public void EndRun(IFragmentWriter writer)
		{
			// Ending the run should be a null op for word writer
			// Beginning a new run is sufficient to end the old run
			// and to ensure new styles/content are applied to the new run.
		}
		public void SetRunStyle(IFragmentWriter writer, string css)
		{
			// Grab the current run and set its style
			Run currentRun = ((WordFragmentWriter)writer).GetCurrentRun();

			// TODO: get the style indicated by the string css class
			// For now, use bold as a default style in order to test setting styles

			// If run already has properties, append the new style to run properties
			if (currentRun.RunProperties != null)
				currentRun.RunProperties.Append(new WP.Bold());

			// Otherwise create run properties and append the style
			else
			{
				currentRun.RunProperties = new WP.RunProperties();
				currentRun.RunProperties.Append(new WP.Bold());
			}
		}
		public void StartLink(IFragmentWriter writer, Guid destination)
		{
			return;
		}
		public void StartLink(IFragmentWriter writer, string externalDestination)
		{
			return;
		}
		public void EndLink(IFragmentWriter writer)
		{
			return;
		}

		/// <summary>
		/// Adds text to the last run in the doc, if one exists.
		/// Creates a new run from the text otherwise.
		/// </summary>
		public void AddToRunContent(IFragmentWriter writer, string txtContent)
		{
			// For spaces to show correctly, set preserve spaces on the new text element
			WP.Text txt = new WP.Text(txtContent);
			txt.Space = SpaceProcessingModeValues.Preserve;
			((WordFragmentWriter)writer).GetCurrentRun()
				.AppendChild(txt);
		}
		public void AddLineBreakInRunContent(IFragmentWriter writer)
		{
			((WordFragmentWriter)writer).GetCurrentRun()
				.AppendChild(new WP.Break());
		}
		public void StartTable(IFragmentWriter writer)
		{
			return;
		}
		public void AddTableTitle(IFragmentWriter writer, IFragment content)
		{
			return;
		}
		public void StartTableBody(IFragmentWriter writer)
		{
			return;
		}
		public void StartTableRow(IFragmentWriter writer)
		{
			return;
		}
		public void AddTableCell(IFragmentWriter writer, bool isHead, int colSpan, HorizontalAlign alignment, IFragment content)
		{
			return;
		}
		public void EndTableRow(IFragmentWriter writer)
		{
			return;
		}
		public void EndTableBody(IFragmentWriter writer)
		{
			return;
		}
		public void EndTable(IFragmentWriter writer)
		{
			return;
		}

		public void StartEntry(IFragmentWriter writer, string className, Guid entryGuid, int index, RecordClerk clerk)
		{
			// Each entry starts a new paragraph, and any entry data added will be added within the same paragraph.
			// Create a new paragraph for the entry.
			DocFragment wordDoc = ((WordFragmentWriter)writer).WordFragment;
			Paragraph entryPar = wordDoc.GetNewParagraph();

			// TODO: paragraph-level styles can be set here.
		}
		public void AddEntryData(IFragmentWriter writer, List<IFragment> pieces)
		{
			// TODO: In theory the pieces in the list here are already styled--where are run-level styles first set?
			foreach (IFragment piece in pieces)
			{
				WordFragmentWriter wordWriter = ((WordFragmentWriter)writer);

				// Each piece contains one run. These runs should reside in the same paragraph.
				// So we append each run instead of the IFragments directly.
				// Character formatting & style of each run will be preserved.
				List<Run> runs = ((DocFragment)piece).DocBody.Descendants<Run>().ToList();
				foreach (Run run in runs)
				{
					// For spaces to show correctly, set preserve spaces on the text element
					WP.Text txt = new WP.Text(" ");
					txt.Space = SpaceProcessingModeValues.Preserve;
					run.AppendChild(txt);
					wordWriter.Insert(run);
				}
			}
		}
		public void EndEntry(IFragmentWriter writer)
		{
			return;
		}
		public void AddCollection(IFragmentWriter writer, bool isBlockProperty, string className, string content)
		{
			return;
		}
		public void BeginObjectProperty(IFragmentWriter writer, bool isBlockProperty, string getCollectionItemClassAttribute)
		{
			return;
		}
		public void EndObject(IFragmentWriter writer)
		{
			return;
		}
		public void WriteProcessedContents(IFragmentWriter writer, IFragment contents)
		{
			if (contents.IsNullOrEmpty())
			{
				((WordFragmentWriter)writer).Insert(contents);
			}
		}
		public IFragment AddImage(string classAttribute, string srcAttribute, string pictureGuid)
		{
			return new DocFragment("TODO: add image");
		}
		public IFragment AddImageCaption(string captionContent)
		{
			return new DocFragment("TODO: add image caption");
		}
		public IFragment GenerateSenseNumber(string formattedSenseNumber, string senseNumberWs)
		{
			// TODO: for styles, do we need to do something with the writing system?
			return new DocFragment(formattedSenseNumber);
		}
		public IFragment AddLexReferences(bool generateLexType,IFragment lexTypeContent, string className, string referencesContent, bool typeBefore)
		{
			var fragment = new DocFragment();
			// Generate the factored ref types element (if before).
			if (generateLexType && typeBefore)
			{
				fragment.Append(WriteProcessedObject(false, lexTypeContent, className));
			}
			// Then add all the contents for the LexReferences (e.g. headwords)
			fragment.Append(new DocFragment(referencesContent));
			// Generate the factored ref types element (if after).
			if (generateLexType && !typeBefore)
			{
				fragment.Append(WriteProcessedObject(false, lexTypeContent, className));
			}

			return fragment;
		}
		public void BeginCrossReference(IFragmentWriter writer, bool isBlockProperty, string className)
		{
			return;
		}
		public void EndCrossReference(IFragmentWriter writer)
		{
			return;
		}
		public IFragment WriteProcessedSenses(bool isBlock, IFragment senseContent, string className, IFragment sharedGramInfo)
		{
			sharedGramInfo.Append(senseContent);
			return sharedGramInfo;
		}
		public IFragment AddAudioWsContent(string wsId, Guid linkTarget, IFragment fileContent)
		{
			return new DocFragment("TODO: add audiows content");
		}
		public IFragment GenerateErrorContent(StringBuilder badStrBuilder)
		{
			return new DocFragment($"Error generating content for string: '{badStrBuilder}'");
		}
		public IFragment GenerateVideoLinkContent(string className, string mediaId, string srcAttribute,
			string caption)
		{
			return new DocFragment("TODO: generate video link content");
		}
	}
}
