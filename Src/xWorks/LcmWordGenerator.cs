// Copyright (c) 2014-$year$ SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Icu.Collation;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.LCModel;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Utils;
using Style = DocumentFormat.OpenXml.Wordprocessing.Style;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.UI.WebControls;
using System.Windows.Media.Imaging;
using XCore;
using XmlDrawing = DocumentFormat.OpenXml.Drawing;
using DrawingWP = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using Pictures = DocumentFormat.OpenXml.Drawing.Pictures;
using System.Text.RegularExpressions;

namespace SIL.FieldWorks.XWorks
{
	// This alias is to be used when creating Wordprocessing Text objects,
	// since there are multiple different Text types across the packages we are using.
	using WP = DocumentFormat.OpenXml.Wordprocessing;

	public class LcmWordGenerator : ILcmContentGenerator, ILcmStylesGenerator
	{
		private LcmCache Cache { get; }
		private static WordStyleCollection s_styleCollection = new WordStyleCollection();
		private ReadOnlyPropertyTable _propertyTable;
		internal const int maxImageHeightInches = 1;
		internal const int maxImageWidthInches = 1;

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
				var generator = new LcmWordGenerator(cache);
				var readOnlyPropertyTable = new ReadOnlyPropertyTable(propertyTable);

				generator.Init(readOnlyPropertyTable);
				var settings = new ConfiguredLcmGenerator.GeneratorSettings(cache, readOnlyPropertyTable, false, true, System.IO.Path.GetDirectoryName(filePath),
							ConfiguredLcmGenerator.IsEntryStyleRtl(readOnlyPropertyTable, configuration), System.IO.Path.GetFileName(cssPath) == "configured.css")
							{ ContentGenerator = generator, StylesGenerator = generator};
				settings.StylesGenerator.AddGlobalStyles(configuration, readOnlyPropertyTable);
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

				var propStyleSheet = FontHeightAdjuster.StyleSheetFromPropertyTable(propertyTable);

				foreach (var entry in entryContents)
				{
					if (!entry.Item2.IsNullOrEmpty())
					{
						IFragment letterHeader = GenerateLetterHeaderIfNeeded(entry.Item1,
							ref lastHeader, col, settings, readOnlyPropertyTable, propStyleSheet, clerk);

						// If needed, append letter header to the word doc
						if (!letterHeader.IsNullOrEmpty())
							fragment.Append(letterHeader);

						// Append the entry to the word doc
						fragment.Append(entry.Item2);
					}
				}
				col?.Dispose();

				if (progress != null)
					progress.Message = xWorksStrings.ksGeneratingStyleInfo;

				// Generate styles
				StyleDefinitionsPart stylePart = fragment.mainDocPart.StyleDefinitionsPart;
				if (stylePart == null)
				{
					// Initialize word doc's styles xml
					stylePart = AddStylesPartToPackage(fragment.DocFrag);
					Styles styleSheet = new Styles();

					// Add generated styles into the stylesheet from the collection.
					var styles = s_styleCollection.GetStyles();
					foreach (var style in styles)
					{
						styleSheet.AppendChild(style.CloneNode(true));
					}

					// Clear the collection.
					s_styleCollection.Clear();

					// Clone styles from the stylesheet into the word doc's styles xml
					stylePart.Styles = ((Styles)styleSheet.CloneNode(true));
				}

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

		internal static IFragment GenerateLetterHeaderIfNeeded(ICmObject entry, ref string lastHeader, Collator headwordWsCollator, ConfiguredLcmGenerator.GeneratorSettings settings, ReadOnlyPropertyTable propertyTable, LcmStyleSheet mediatorStyleSheet, RecordClerk clerk = null)
		{
			StringBuilder headerTextBuilder = ConfiguredLcmGenerator.GenerateLetterHeaderIfNeeded(entry, ref lastHeader,
				headwordWsCollator, settings, clerk);

			// Create LetterHeader doc fragment and link it with the letter heading style.
			return DocFragment.GenerateLetterHeaderDocFragment(headerTextBuilder.ToString(), WordStylesGenerator.LetterHeadingDisplayName);
		}

		/*
		 * DocFragment Region
		 */
		#region DocFragment class
		public class DocFragment : IFragment
		{
			internal MemoryStream MemStr { get; }
			internal WordprocessingDocument DocFrag { get; }
			internal MainDocumentPart mainDocPart { get; }
			internal WP.Body DocBody { get; }
			internal string ParagraphStyle { get; private set; }

			/// <summary>
			/// Constructs a new memory stream and creates an empty doc fragment
			/// that writes to that stream.
			/// </summary>
			public DocFragment()
			{
				MemStr = new MemoryStream();
				DocFrag = WordprocessingDocument.Open(MemStr, true);

				// Initialize the document and body.
				mainDocPart = DocFrag.AddMainDocumentPart();
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
				mainDocPart = DocFrag.AddMainDocumentPart();
				mainDocPart.Document = new WP.Document();
				DocBody = mainDocPart.Document.AppendChild(new WP.Body());
			}

			/// <summary>
			/// Constructs a new memory stream and creates a non-empty doc fragment,
			/// containing the given string, that writes to that stream.
			/// </summary>
			public DocFragment(string str) : this()
			{
				// Only create run, and text objects if the string is nonempty
				if (!string.IsNullOrEmpty(str))
				{
					WP.Run run = DocBody.AppendChild(new WP.Run());

					// For spaces to show correctly, set preserve spaces on the text element
					WP.Text txt = new WP.Text(str);
					txt.Space = SpaceProcessingModeValues.Preserve;
					run.AppendChild(txt);
				}
			}

			/// <summary>
			/// Generate the document fragment for a letter header.
			/// </summary>
			/// <param name="str">Letter header string.</param>
			/// <param name="styleDisplayName">Letter header style name to display in Word.</param>
			internal static DocFragment GenerateLetterHeaderDocFragment(string str, string styleDisplayName)
			{
				var docFrag = new DocFragment();
				// Only create paragraph, run, and text objects if string is nonempty
				if (!string.IsNullOrEmpty(str))
				{
					WP.ParagraphProperties paragraphProps = new WP.ParagraphProperties(new ParagraphStyleId() { Val = styleDisplayName });
					WP.Paragraph para = docFrag.DocBody.AppendChild(new WP.Paragraph(paragraphProps));
					WP.Run run = para.AppendChild(new WP.Run());
					// For spaces to show correctly, set preserve spaces on the text element
					WP.Text txt = new WP.Text(str);
					txt.Space = SpaceProcessingModeValues.Preserve;
					run.AppendChild(txt);
				}
				return docFrag;
			}

			public static string GetWsStyleName(LcmCache cache, ConfigurableDictionaryNode config, string writingSystem)
			{
				string styleDisplayName = config.DisplayLabel;

				// If the config does not contain writing system options, then just return the style name.(An example is custom fields.)
				if (!(config.DictionaryNodeOptions is DictionaryNodeWritingSystemOptions))
				{
					return styleDisplayName;
				}

				return GenerateWsStyleName(cache, styleDisplayName, writingSystem);
			}

			public static string GenerateWsStyleName(LcmCache cache, string styleDisplayName, string writingSystem)
			{
				var wsStr = writingSystem;
				var possiblyMagic = WritingSystemServices.GetMagicWsIdFromName(writingSystem);
				// If it is magic, then get the associated ws.
				if (possiblyMagic != 0)
				{
					// Get a list of the writing systems for the magic name, and use the first one.
					wsStr = WritingSystemServices.GetWritingSystemList(cache, possiblyMagic, false).First().Id;
				}

				// If there is no base style, return just the ws style.
				if (string.IsNullOrEmpty(styleDisplayName))
					return WordStylesGenerator.GetWsString(wsStr);
				// If there is a base style, return the ws-specific version of that style.
				return styleDisplayName + WordStylesGenerator.GetWsString(wsStr);
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

						case "r":
							string docStr = ToString(docSection);
							if (string.IsNullOrEmpty(docStr))
								if (docSection.Descendants<Drawing>().Any())
									docStr = "[image run]";
							FragStr.Append(docStr);
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
			/// Use this if styles have already been applied.
			/// </summary>
			public void Append(IFragment frag)
			{
				foreach (OpenXmlElement elem in ((DocFragment)frag).DocBody.Elements().ToList())
				{
					if (elem.Descendants<DocumentFormat.OpenXml.Drawing.Blip>().Any())
					{
						// then need to append image in such a way that the relID is maintained
						this.DocBody.AppendChild(CloneImageRun(frag, elem));
						// wordWriter.WordFragment.AppendPhotoToParagraph(frag, elem, wordWriter.ForceNewParagraph);
					}

					// Append each element. It is necessary to deep clone the node to maintain its tree of document properties
					// and to ensure its styles will be maintained in the copy.
					else
						this.DocBody.AppendChild(elem.CloneNode(true));
				}
			}

			/// <summary>
			/// Append a table to the doc fragment.
			/// </summary>
			public void AppendTable(WP.Table table)
			{
				// Deep clone the run b/c of its tree of properties and to maintain styles.
				this.DocBody.AppendChild(table.CloneNode(true));
			}

			/// <summary>
			/// Append a paragraph to the doc fragment.
			/// </summary>
			public void AppendParagraph(WP.Paragraph para)
			{
				// Deep clone the run b/c of its tree of properties and to maintain styles.
				this.DocBody.AppendChild(para.CloneNode(true));
			}


			/// <summary>
			/// Appends a new run inside the last paragraph of the doc fragment--creates a new paragraph if none
			/// exists or if forceNewParagraph is true.
			/// The run will be added to the end of the paragraph.
			/// </summary>
			/// <param name="run">The run to append.</param>
			/// <param name="forceNewParagraph">Even if a paragraph exists, force the creation of a new paragraph.</param>
			public void AppendToParagraph(IFragment fragToCopy, OpenXmlElement run, bool forceNewParagraph)
			{
				WP.Paragraph lastPar = null;

				if (forceNewParagraph)
				{
					// When forcing a new paragraph use a 'continuation' style for the new paragraph.
					// The continuation style is based on the style used in the first paragraph.
					string style = null;
					WP.Paragraph firstParagraph = DocBody.OfType<WP.Paragraph>().FirstOrDefault();
					if (firstParagraph != null)
					{
						WP.ParagraphProperties paraProps = firstParagraph.OfType<WP.ParagraphProperties>().FirstOrDefault();
						if (paraProps != null)
						{
							ParagraphStyleId styleId = paraProps.OfType<WP.ParagraphStyleId>().FirstOrDefault();
							if (styleId != null && styleId.Val != null && styleId.Val.Value != null)
							{
								if (styleId.Val.Value.EndsWith(WordStylesGenerator.EntryStyleContinue))
								{
									style = styleId.Val.Value;
								}
								else
								{
									style = styleId.Val.Value + WordStylesGenerator.EntryStyleContinue;
								}
							}
						}
					}

					lastPar = GetNewParagraph();
					if (!string.IsNullOrEmpty(style))
					{
						WP.ParagraphProperties paragraphProps = new WP.ParagraphProperties(
							new ParagraphStyleId() { Val = style });
						lastPar.Append(paragraphProps);
					}
				}
				else
				{
					lastPar = GetLastParagraph();
				}

				// Deep clone the run b/c of its tree of properties and to maintain styles.
				lastPar.AppendChild(CloneRun(fragToCopy, run));
			}

			public OpenXmlElement CloneRun(IFragment fragToCopy, OpenXmlElement run)
			{
				if (run.Descendants<DocumentFormat.OpenXml.Drawing.Blip>().Any())
				{
					return CloneImageRun(fragToCopy, run);
				}

				return run.CloneNode(true);

			}

			/// <summary>
			/// Clones and returns a run containing an image.
			/// </summary>
			public OpenXmlElement CloneImageRun(IFragment fragToCopy, OpenXmlElement run)
			{
				var clonedRun = run.CloneNode(true);
				clonedRun.Descendants<DocumentFormat.OpenXml.Drawing.Blip>().ToList().ForEach(
					blip =>
					{
						var newRelation =
							CopyImage(DocFrag, blip.Embed, ((DocFragment)fragToCopy).DocFrag);
						// Update the relationship ID in the cloned blip element.
						blip.Embed = newRelation;
					});
				clonedRun.Descendants<DocumentFormat.OpenXml.Vml.ImageData>().ToList().ForEach(
					imageData =>
					{
						var newRelation = CopyImage(DocFrag, imageData.RelationshipId, ((DocFragment)fragToCopy).DocFrag);
						// Update the relationship ID in the cloned image data element.
						imageData.RelationshipId = newRelation;
					});
				return clonedRun;
			}

			/// <summary>
			/// Copies the image part of one document to another and returns the relationship ID of the copied image part.
			/// </summary>
			public static string CopyImage(WordprocessingDocument newDoc, string relId, WordprocessingDocument org)
			{
				if (org.MainDocumentPart == null || newDoc.MainDocumentPart == null)
				{
					throw new ArgumentNullException("MainDocumentPart is null.");
				}
				var p = org.MainDocumentPart.GetPartById(relId) as ImagePart;
				var newPart = newDoc.MainDocumentPart.AddPart(p);
				newPart.FeedData(p.GetStream());
				return newDoc.MainDocumentPart.GetIdOfPart(newPart);
			}

			/// <summary>
			/// Appends text to the last run inside the doc fragment.
			/// If no run exists, a new one will be created.
			/// </summary>
			public void Append(string text)
			{
				WP.Run lastRun = GetLastRun();
				WP.Text newText = new WP.Text(text);
				newText.Space = SpaceProcessingModeValues.Preserve;
				lastRun.Append(newText);
			}

			public void AppendBreak()
			{
				WP.Run lastRun = GetLastRun();
				lastRun.AppendChild(new WP.Break());
			}

			public void AppendSpace()
			{
				WP.Run lastRun = GetLastRun();
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
			public WP.Paragraph GetLastParagraph()
			{
				List<WP.Paragraph> parList = DocBody.OfType<WP.Paragraph>().ToList();
				if (parList.Any())
					return parList.Last();
				return GetNewParagraph();
			}

			/// <summary>
			/// Creates and returns a new paragraph.
			/// </summary>
			public WP.Paragraph GetNewParagraph()
			{
				WP.Paragraph newPar = DocBody.AppendChild(new WP.Paragraph());
				return newPar;
			}

			/// <summary>
			/// Returns last run in the document if it contains any,
			/// else creates and returns a new run.
			/// </summary>
			internal WP.Run GetLastRun()
			{
				List<WP.Run> runList = DocBody.OfType<WP.Run>().ToList();
				if (runList.Any())
					return runList.Last();

				return DocBody.AppendChild(new WP.Run());
			}
		}
		#endregion DocFragment class

		/*
		 * WordFragmentWriter Region
		 */
		#region WordFragmentWriter class
		public class WordFragmentWriter : IFragmentWriter
		{
			public DocFragment WordFragment { get; }
			private bool isDisposed;
			internal Dictionary<string, Collator> collatorCache = new Dictionary<string, Collator>();
			public bool ForceNewParagraph { get; set; } = false;

			public WordFragmentWriter(DocFragment frag)
			{
				WordFragment = frag;
			}

			public void Dispose()
			{
				// When writer is being disposed, dispose only the dictionary entries,
				// not the word doc fragment.
				// ConfiguredLcmGenerator consistently returns the fragment and disposes the writer,
				// which would otherwise result in a disposed fragment being accessed.

				if (!isDisposed)
				{
					foreach (var cachEntry in collatorCache.Values)
					{
						cachEntry?.Dispose();
					}

					GC.SuppressFinalize(this);
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

			internal WP.Table CurrentTable { get; set; }
			internal WP.TableRow CurrentTableRow { get; set; }
			internal IFragment TableTitleContent { get; set; }
			internal int TableColumns { get; set; }
			internal int RowColumns { get; set; }

			/// <summary>
			/// Add a new run to the WordFragment DocBody.
			/// </summary>
			public void AddRun(LcmCache cache, ConfigurableDictionaryNode config, ReadOnlyPropertyTable propTable, string writingSystem, bool first)
			{
				// Add Between text, if it is not the first item.
				if (!first &&
					config != null &&
					!string.IsNullOrEmpty(config.Between))
				{
					var betweenRun = CreateBeforeAfterBetweenRun(config.Between);
					WordFragment.DocBody.Append(betweenRun);
				}

				var run = new WP.Run();
				WordFragment.DocBody.AppendChild(run);

				if (config == null || writingSystem == null)
				{
					return;
				}

				string displayNameBase = DocFragment.GetWsStyleName(cache, config, writingSystem);
				if (!string.IsNullOrEmpty(displayNameBase))
				{
					// The calls to TryGetStyle() and AddStyle() need to be in the same lock.
					lock (s_styleCollection)
					{
						string uniqueDisplayName = null;
						if (s_styleCollection.TryGetStyle(config.Style, displayNameBase, out Style existingStyle))
						{
							uniqueDisplayName = existingStyle.StyleId;
						}
						// If the style is not in the collection, then add it.
						else
						{
							var wsString = WordStylesGenerator.GetWsString(writingSystem);

							// Get the style from the LcmStyleSheet, using the style name defined in the config.
							if (!string.IsNullOrEmpty(config.Style))
							{
								var wsId = cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr(writingSystem);
								Style style = WordStylesGenerator.GenerateWordStyleFromLcmStyleSheet(config.Style, wsId, propTable);
								if (style == null || style.Type != StyleValues.Character)
								{
									// If we hit this assert, then we might end up referencing a style that
									// does not get created.
									Debug.Assert(false);
									return;
								}

								style.Append(new BasedOn() { Val = wsString });
								style.StyleId = displayNameBase;
								style.StyleName.Val = style.StyleId;
								uniqueDisplayName = s_styleCollection.AddStyle(style, config.Style, style.StyleId);
							}
							// There is no style name defined in the config so generate a style that is identical to the writing system style
							// except that it contains a display name that the user wants to see in the Word Styles. (example: "Reverse Abbreviation[lang='en']")
							else
							{
								Style rootStyle = GetOrCreateCharacterStyle(wsString, wsString, propTable);
								if (rootStyle != null)
								{
									Style basedOnStyle = WordStylesGenerator.GenerateBasedOnCharacterStyle(new Style(), wsString, displayNameBase);
									if (basedOnStyle != null)
									{
										uniqueDisplayName = s_styleCollection.AddStyle(basedOnStyle, config.Style, basedOnStyle.StyleId);
									}
									else
									{
										// If we hit this assert, then we might end up referencing a style that
										// does not get created.
										Debug.Assert(false, "Could not generate BasedOn character style " + displayNameBase);
									}
								}
								else
								{
									// If we hit this assert, then we might end up referencing a style that
									// does not get created.
									Debug.Assert(false, "Could not create style for " + wsString);
								}
							}
						}

						run.Append(new RunProperties(new RunStyle() { Val = uniqueDisplayName }));
					}
				}
			}
		}
		#endregion WordFragmentWriter class

		/*
		 * Content Generator Region
		 */
		#region ILcmContentGenerator functions to implement
		public IFragment GenerateWsPrefixWithString(ConfigurableDictionaryNode config, ConfiguredLcmGenerator.GeneratorSettings settings,
			bool displayAbbreviation, int wsId, IFragment content)
		{
			if (displayAbbreviation)
			{
				//  Create the abbreviation run that uses the abbreviation style.
				// Note: Appending a space is similar to the code in CssGenerator.cs GenerateCssForWritingSystemPrefix() that adds
				//       a space after the abbreviation.
				string abbrev = ((CoreWritingSystemDefinition)settings.Cache.WritingSystemFactory.get_EngineOrNull(wsId)).Abbreviation + " ";
				var abbrevRun = CreateRun(abbrev, WordStylesGenerator.WritingSystemDisplayName);

				// We can't just prepend the abbreviation run because the content might already contain a before or between run.
				// The abbreviation run should go after the before or between run, but before the string run.
				bool abbrevAdded = false;
				var runs = ((DocFragment)content).DocBody.Elements<Run>().ToList<Run>();
				if (runs.Count > 1)
				{
					// To determine if the first run is before or between content, check if it's run properties
					// have the style associated with all before and between content.
					Run firstRun = runs.First();
					RunProperties runProps = firstRun.OfType<RunProperties>().FirstOrDefault();
					if (runProps != null)
					{
						RunStyle runStyle = runProps.OfType<RunStyle>().FirstOrDefault();
						if (runStyle != null && runStyle.Val == WordStylesGenerator.BeforeAfterBetweenDisplayName)
						{
							((DocFragment)content).DocBody.InsertAfter(abbrevRun, firstRun);
							abbrevAdded = true;
						}
					}
				}

				// There is no before or between run, so just prepend the abbreviation run.
				if (!abbrevAdded)
				{
					((DocFragment)content).DocBody.PrependChild(abbrevRun);
				}

				// Add the abbreviation style to the collection (if not already added).
				GetOrCreateCharacterStyle(WordStylesGenerator.WritingSystemStyleName, WordStylesGenerator.WritingSystemDisplayName, _propertyTable);
			}

			return content;
		}

		public IFragment GenerateAudioLinkContent(ConfigurableDictionaryNode config, string classname, string srcAttribute, string caption, string safeAudioId)
		{
			// TODO
			return new DocFragment("TODO: generate audio link content");
		}
		public IFragment WriteProcessedObject(ConfigurableDictionaryNode config, bool isBlock, IFragment elementContent, string className)
		{
			return WriteProcessedElementContent(elementContent, config);
		}
		public IFragment WriteProcessedCollection(ConfigurableDictionaryNode config, bool isBlock, IFragment elementContent, string className)
		{
			return WriteProcessedElementContent(elementContent, config);
		}

		private IFragment WriteProcessedElementContent(IFragment elementContent, ConfigurableDictionaryNode config)
		{
			// Check if the character style for the last run should be modified.
			if (string.IsNullOrEmpty(config.Style) && !string.IsNullOrEmpty(config.Parent.Style) &&
				(config.Parent.StyleType != ConfigurableDictionaryNode.StyleTypes.Paragraph))
			{
				AddRunStyle(elementContent, config.Parent.Style, config.Parent.DisplayLabel, false);
			}

			bool eachOnANewLine = config != null &&
								  config.DictionaryNodeOptions is IParaOption &&
								  ((IParaOption)(config.DictionaryNodeOptions)).DisplayEachInAParagraph;

			// Add Before text, if it is not going to be displayed on its own line.
			if (!eachOnANewLine && !string.IsNullOrEmpty(config.Before))
			{
				var beforeRun = CreateBeforeAfterBetweenRun(config.Before);
				((DocFragment)elementContent).DocBody.PrependChild(beforeRun);
			}

			// Add After text, if it is not going to be displayed on its own line.
			if (!eachOnANewLine && !string.IsNullOrEmpty(config.After))
			{
				var afterRun = CreateBeforeAfterBetweenRun(config.After);
				((DocFragment)elementContent).DocBody.Append(afterRun);
			}

			return elementContent;
		}
		public IFragment GenerateGramInfoBeforeSensesContent(IFragment content, ConfigurableDictionaryNode config)
		{
			return content;
		}
		public IFragment GenerateGroupingNode(ConfigurableDictionaryNode config, object field, string className,  DictionaryPublicationDecorator publicationDecorator, ConfiguredLcmGenerator.GeneratorSettings settings,
			Func<object, ConfigurableDictionaryNode, DictionaryPublicationDecorator, ConfiguredLcmGenerator.GeneratorSettings, IFragment> childContentGenerator)
		{
			var groupData = new DocFragment();
			WP.Paragraph groupPara = null;
			bool eachOnANewLine = config != null &&
								  config.DictionaryNodeOptions is DictionaryNodeGroupingOptions &&
								  ((DictionaryNodeGroupingOptions)(config.DictionaryNodeOptions)).DisplayEachInAParagraph;

			// If the group is displayed on a new line then the group needs its own paragraph, so
			// the group style can be applied to the entire paragraph (applied to all of the runs
			// contained in it).
			if (eachOnANewLine)
			{
				groupPara = new WP.Paragraph();
			}

			// Add Before text, if it is not going to be displayed on its own line.
			if (!eachOnANewLine && !string.IsNullOrEmpty(config.Before))
			{
				var beforeRun = CreateBeforeAfterBetweenRun(config.Before);
				groupData.DocBody.PrependChild(beforeRun);
			}

			// Add the group data.
			foreach (var child in config.ReferencedOrDirectChildren)
			{
				IFragment childContent = childContentGenerator(field, child, publicationDecorator, settings);
				if (eachOnANewLine)
				{
					var elements = ((DocFragment)childContent).DocBody.Elements().ToList();
					foreach (OpenXmlElement elem in elements)
					{
						// Deep clone the run b/c of its tree of properties and to maintain styles.
						groupPara.AppendChild(groupData.CloneRun(childContent, elem));
					}
				}
				else
				{
					groupData.Append(childContent);
				}
			}

			// Add After text, if it is not going to be displayed on its own line.
			if (!eachOnANewLine && !string.IsNullOrEmpty(config.After))
			{
				var afterRun = CreateBeforeAfterBetweenRun(config.After);
				groupData.DocBody.Append(afterRun);
			}

			// Don't add an empty paragraph to the groupData fragment.
			if (groupPara != null && groupPara.HasChildren)
			{
				// Add the group style.
				if (!string.IsNullOrEmpty(config.Style))
				{
					WP.ParagraphProperties paragraphProps =
						new WP.ParagraphProperties(new ParagraphStyleId() { Val = config.Style });
					groupPara.PrependChild(paragraphProps);
				}
				groupData.DocBody.AppendChild(groupPara);
			}

			return groupData;
		}

		public IFragment AddSenseData(ConfigurableDictionaryNode config, IFragment senseNumberSpan, Guid ownerGuid, IFragment senseContent, bool first)
		{
			var senseData = new DocFragment();
			var senseNode = (DictionaryNodeSenseOptions)config?.DictionaryNodeOptions;
			bool eachOnANewLine = false;
			bool firstSenseInline = false;
			if (senseNode != null)
			{
				eachOnANewLine = senseNode.DisplayEachSenseInAParagraph;
				firstSenseInline = senseNode.DisplayFirstSenseInline;
			}

			// We want a break before the first sense item, between items, and after the last item.
			// So, only add a break before the content if it is the first sense and it's not displayed in-line.
			if (eachOnANewLine && first && !firstSenseInline)
			{
				senseData.AppendBreak();
			}

			// Add Between text, if it is not going to be displayed on it's own line
			// and it is not the first item.
			if (!first &&
				config != null &&
				!eachOnANewLine &&
				!string.IsNullOrEmpty(config.Between))
			{
				var betweenRun = CreateBeforeAfterBetweenRun(config.Between);
				((DocFragment)senseData).DocBody.Append(betweenRun);
			}

			// Add sense numbers if needed
			if (!senseNumberSpan.IsNullOrEmpty())
			{
				senseData.Append(senseNumberSpan);
			}

			senseData.Append(senseContent);

			if (eachOnANewLine)
			{
				senseData.AppendBreak();
			}

			return senseData;
		}

		public IFragment AddCollectionItem(ConfigurableDictionaryNode config, bool isBlock, string collectionItemClass, IFragment content, bool first)
		{
			// Add the style to all the runs in the content fragment.
			if (!string.IsNullOrEmpty(config.Style) &&
				(config.StyleType != ConfigurableDictionaryNode.StyleTypes.Paragraph))
			{
				AddRunStyle(content, config.Style, config.DisplayLabel, true);
			}

			var collData = CreateFragment();
			bool eachOnANewLine = false;
			if (config != null &&
				config.DictionaryNodeOptions is IParaOption &&
				((IParaOption)(config.DictionaryNodeOptions)).DisplayEachInAParagraph)
			{
				eachOnANewLine = true;

				// We want a break before the first collection item, between items, and after the last item.
				// So, only add a break before the content if it is the first.
				if (first)
				{
					collData.AppendBreak();
				}
			}

			// Add Between text, if it is not going to be displayed on its own line
			// and it is not the first item in the collection.
			if (!first &&
				config != null &&
				config.DictionaryNodeOptions is IParaOption &&
				!eachOnANewLine &&
				!string.IsNullOrEmpty(config.Between))
			{
				var betweenRun = CreateBeforeAfterBetweenRun(config.Between);
				((DocFragment)collData).DocBody.Append(betweenRun);
			}

			collData.Append(content);

			if (eachOnANewLine)
			{
				collData.AppendBreak();
			}

			return collData;
		}
		public IFragment AddProperty(ConfigurableDictionaryNode config, string className, bool isBlockProperty, string content)
		{
			var propFrag = new DocFragment();

			// Add Before text.
			if (!string.IsNullOrEmpty(config.Before))
			{
				var beforeRun = CreateBeforeAfterBetweenRun(config.Before);
				propFrag.DocBody.Append(beforeRun);
			}

			// Add the content with the style.
			if (!string.IsNullOrEmpty(content))
			{
				string styleDisplayName = null;
				if (!string.IsNullOrEmpty(config.Style))
				{
					string displayNameBase = !string.IsNullOrEmpty(config.DisplayLabel) ? config.DisplayLabel : config.Style;

					Style style = GetOrCreateCharacterStyle(config.Style, displayNameBase, _propertyTable);
					if (style != null)
					{
						styleDisplayName = style.StyleId;
					}
				}
				var contentRun = CreateRun(content, styleDisplayName);
				propFrag.DocBody.Append(contentRun);
			}

			// Add After text.
			if (!string.IsNullOrEmpty(config.After))
			{
				var afterRun = CreateBeforeAfterBetweenRun(config.After);
				propFrag.DocBody.Append(afterRun);
			}

			return propFrag;
		}

		public IFragment CreateFragment()
		{
			return new DocFragment();
		}

		public IFragment CreateFragment(string str)
		{
			return new DocFragment(str);
		}

		public IFragmentWriter CreateWriter(IFragment frag)
		{
			return new WordFragmentWriter((DocFragment)frag);
		}

		public void StartMultiRunString(IFragmentWriter writer, ConfigurableDictionaryNode config, string writingSystem)
		{
			return;
		}
		public void EndMultiRunString(IFragmentWriter writer)
		{
			return;
		}
		public void StartBiDiWrapper(IFragmentWriter writer, ConfigurableDictionaryNode config, bool rightToLeft)
		{
			return;
		}
		public void EndBiDiWrapper(IFragmentWriter writer)
		{
			return;
		}
		/// <summary>
		/// Add a new run to the writers WordFragment DocBody.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="writingSystem"></param>
		public void StartRun(IFragmentWriter writer, ConfigurableDictionaryNode config, ReadOnlyPropertyTable propTable, string writingSystem, bool first)
		{
			((WordFragmentWriter)writer).AddRun(Cache, config, propTable, writingSystem, first);
		}
		public void EndRun(IFragmentWriter writer)
		{
			// Ending the run should be a null op for word writer
			// Beginning a new run is sufficient to end the old run
			// and to ensure new styles/content are applied to the new run.
		}

		/// <summary>
		/// Overrides the style for a specific run.
		/// This is needed to set the specific style for any field that allows the
		/// default style to be overridden (Table Cell, Custom Field, Note...).
		/// </summary>
		public void SetRunStyle(IFragmentWriter writer, ConfigurableDictionaryNode config, ReadOnlyPropertyTable propertyTable, string writingSystem, string runStyle, bool error)
		{
			if (!string.IsNullOrEmpty(runStyle))
			{
				AddRunStyle(((WordFragmentWriter)writer).WordFragment, runStyle, runStyle, false);
			}
		}
		public void StartLink(IFragmentWriter writer, ConfigurableDictionaryNode config, Guid destination)
		{
			return;
		}
		public void StartLink(IFragmentWriter writer, ConfigurableDictionaryNode config, string externalDestination)
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
			((WordFragmentWriter)writer).WordFragment.GetLastRun()
				.AppendChild(txt);
		}
		public void AddLineBreakInRunContent(IFragmentWriter writer, ConfigurableDictionaryNode config)
		{
			((WordFragmentWriter)writer).WordFragment.GetLastRun()
				.AppendChild(new WP.Break());
		}
		public void StartTable(IFragmentWriter writer, ConfigurableDictionaryNode config)
		{
			WordFragmentWriter wordWriter = (WordFragmentWriter)writer;
			Debug.Assert(wordWriter.CurrentTable == null,
				"Not expecting nested tables.  Treating it as a new table.");

			wordWriter.CurrentTable = new WP.Table();
			wordWriter.TableTitleContent = null;
			wordWriter.TableColumns = 0;
			wordWriter.WordFragment.DocBody.Append(wordWriter.CurrentTable);
		}
		public void AddTableTitle(IFragmentWriter writer, IFragment content)
		{
			WordFragmentWriter wordWriter = (WordFragmentWriter)writer;

			// We can't add the Table Title until we know the total number of columns in the
			// table. Store off the content and add the Title when we are ending the Table.
			wordWriter.TableTitleContent = content;
			if (wordWriter.TableColumns == 0)
			{
				wordWriter.TableColumns = 1;
			}
		}
		public void StartTableBody(IFragmentWriter writer)
		{
			// Nothing to do for Word export.
		}
		public void StartTableRow(IFragmentWriter writer)
		{
			WordFragmentWriter wordWriter = (WordFragmentWriter)writer;
			Debug.Assert(wordWriter.CurrentTableRow == null,
				"Not expecting nested tables rows.  Treating it as a new table row.");

			wordWriter.CurrentTableRow = new WP.TableRow();
			wordWriter.RowColumns = 0;
			wordWriter.CurrentTable.Append(wordWriter.CurrentTableRow);
		}
		public void AddTableCell(IFragmentWriter writer, bool isHead, int colSpan, HorizontalAlign alignment, IFragment content)
		{
			WordFragmentWriter wordWriter = (WordFragmentWriter)writer;
			wordWriter.RowColumns += colSpan;
			WP.Paragraph paragraph = new WP.Paragraph();

			// Set the cell alignment if not Left (the default).
			if (alignment != HorizontalAlign.Left)
			{
				WP.JustificationValues justification = WP.JustificationValues.Left;
				if (alignment == HorizontalAlign.Center)
				{
					justification = WP.JustificationValues.Center;
				}
				else if (alignment == HorizontalAlign.Right)
				{
					justification = WP.JustificationValues.Right;
				}

				WP.ParagraphProperties paragraphProperties = new WP.ParagraphProperties();
				paragraphProperties.AppendChild<WP.Justification>(new WP.Justification() { Val = justification });
				paragraph.AppendChild<WP.ParagraphProperties>(paragraphProperties);
			}

			// The runs contain the text and any cell-specific styling (in the run properties).
			// Note: multiple runs will exist if the cell contains multiple styles.
			foreach (WP.Run run in ((DocFragment)content).DocBody.Elements<WP.Run>())
			{
				WP.Run tableRun = (WP.Run)run.CloneNode(true);

				// Add Bold for headers.
				if (isHead)
				{
					if (tableRun.RunProperties != null)
					{
						tableRun.RunProperties.Append(new WP.Bold());
					}
					else
					{
						WP.RunProperties runProps = new WP.RunProperties(new WP.Bold());
						// Prepend runProps so it appears before any text elements contained in the run
						tableRun.PrependChild<WP.RunProperties>(runProps);
					}
				}
				paragraph.Append(tableRun);
			}

			if (paragraph.HasChildren)
			{
				WP.TableCell tableCell = new WP.TableCell();

				// If there are additional columns to span, then add the property to the
				// first cell to support column spanning.
				if (colSpan > 1)
				{
					WP.TableCellProperties firstCellProps = new WP.TableCellProperties();
					firstCellProps.Append(new WP.HorizontalMerge() { Val = WP.MergedCellValues.Restart });
					tableCell.Append(firstCellProps);
				}
				tableCell.Append(paragraph);
				wordWriter.CurrentTableRow.Append(tableCell);

				// If there are additional columns to span, then add the additional cells.
				if (colSpan > 1)
				{
					for (int ii = 1; ii < colSpan; ii++)
					{
						WP.TableCellProperties spanCellProps = new WP.TableCellProperties();
						spanCellProps.Append(new WP.HorizontalMerge() { Val = WP.MergedCellValues.Continue });
						var spanCell = new WP.TableCell(spanCellProps, new WP.Paragraph());
						wordWriter.CurrentTableRow.Append(spanCell);
					}
				}
			}
		}
		public void EndTableRow(IFragmentWriter writer)
		{
			WordFragmentWriter wordWriter = (WordFragmentWriter)writer;

			if (wordWriter.RowColumns > wordWriter.TableColumns)
			{
				wordWriter.TableColumns = wordWriter.RowColumns;
			}
			wordWriter.RowColumns = 0;
			wordWriter.CurrentTableRow = null;
		}
		public void EndTableBody(IFragmentWriter writer)
		{
			// Nothing to do for Word export.
		}
		public void EndTable(IFragmentWriter writer, ConfigurableDictionaryNode config)
		{
			WordFragmentWriter wordWriter = (WordFragmentWriter)writer;

			// If there is a Table Title, then prepend it now, when we know the number of columns.
			if (wordWriter.TableTitleContent != null)
			{
				wordWriter.CurrentTableRow = new WP.TableRow();
				AddTableCell(writer, false, wordWriter.TableColumns, HorizontalAlign.Center, wordWriter.TableTitleContent);
				wordWriter.CurrentTable.PrependChild(wordWriter.CurrentTableRow); // Prepend so that it is the first row.
				wordWriter.CurrentTableRow = null;
			}

			// Create a TableProperties object and specify the indent information.
			WP.TableProperties tblProp = new WP.TableProperties();

			WP.TableRowAlignmentValues tableAlignment = WP.TableRowAlignmentValues.Left;
			int indentVal = WordStylesGenerator.GetTableIndentInfo(_propertyTable, config, ref tableAlignment);

			var tableJustify = new WP.TableJustification();
			tableJustify.Val = tableAlignment;
			tblProp.Append(tableJustify);

			var tableIndent = new WP.TableIndentation();
			tableIndent.Type = WP.TableWidthUnitValues.Dxa;
			tableIndent.Width = indentVal;
			tblProp.Append(tableIndent);

			// TableProperties MUST be first, so prepend them.
			wordWriter.CurrentTable.PrependChild(tblProp);

			wordWriter.TableColumns = 0;
			wordWriter.TableTitleContent = null;
			wordWriter.CurrentTable = null;
		}
		public void StartEntry(IFragmentWriter writer, ConfigurableDictionaryNode config, string className, Guid entryGuid, int index, RecordClerk clerk)
		{
			// Each entry starts a new paragraph, and any entry data added will usually be added within the same paragraph.
			// The paragraph will end whenever a data type that cannot be in a paragraph is encounter (Tables or Pictures).
			// A new 'continuation' paragraph will be started after the Table or Picture if there is other data that still
			// needs to be added to the entry.
			// Create a new paragraph for the entry.
			DocFragment wordDoc = ((WordFragmentWriter)writer).WordFragment;
			WP.Paragraph entryPar = wordDoc.GetNewParagraph();
			WP.ParagraphProperties paragraphProps = new WP.ParagraphProperties(new ParagraphStyleId() {Val = config.DisplayLabel});
			entryPar.Append(paragraphProps);

			// Create the 'continuation' style for the entry. This style will be the same as the style for the entry with the only
			// difference being that it does not contain the first line indenting (since it is a continuation of the same entry).
			var contStyle = WordStylesGenerator.GenerateContinuationWordStyles(config, _propertyTable);
			s_styleCollection.AddStyle(contStyle, contStyle.StyleId, contStyle.StyleId);
		}
		public void AddEntryData(IFragmentWriter writer, List<ConfiguredLcmGenerator.ConfigFragment> pieces)
		{
			foreach (ConfiguredLcmGenerator.ConfigFragment piece in pieces)
			{
				WordFragmentWriter wordWriter = ((WordFragmentWriter)writer);
				// The final word doc that data is being added to
				DocFragment wordDocument = wordWriter.WordFragment;

				// The word fragment doc containing piece data
				DocFragment frag = ((DocFragment)piece.Frag);

				ConfigurableDictionaryNode config = piece.Config;

				var elements = frag.DocBody.Elements().ToList();

				// This variable will track whether or not we have already added an image from this piece to the Word doc.
				// In the case that more than one image appears in the same piece
				// (e.g. one entry with multiple senses and a picture for each sense),
				// we need to add an empty paragraph between the images to prevent
				// all the images and their captions from being merged into a single textframe by Word.
				Boolean pieceHasImage = false;

				foreach (OpenXmlElement elem in elements)
				{
					switch (elem)
					{
						case WP.Run run:
							Boolean containsDrawing = run.Descendants<Drawing>().Any();
							// Image captions have a Pictures node as their parent.
							// For a main entry, an image will have the "Pictures" ConfigurableDictionaryNode associated with it.
							// For subentries, however, the image is a descendant of a "Subentries" ConfigurableDictionaryNode.
							// Thus, to know if we're dealing with an image and/or caption,
							// we check if the node or its parent is a picture Node, or if the run contains a descendant that is a picture.
							if (config.Label == "Pictures" || config.Parent?.Label == "Pictures" || containsDrawing)
							{
								// Runs containing pictures or captions need to be in separate paragraphs
								// from whatever precedes and follows them because they will be added into textframes,
								// while non-picture content should not be added to the textframes.
								wordWriter.ForceNewParagraph = true;

								// Word automatically merges adjacent textframes with the same size specifications.
								// If the run we are adding is an image (i.e. a Drawing object),
								// and it is being added after another image run was previously added from the same piece,
								// we need to append an empty paragraph between to maintain separate textframes.
								//
								// Checking for adjacent images and adding an empty paragraph between won't work,
								// because each image run is followed by runs containing its caption,
								// copyright & license, etc.
								//
								// But, a lexical entry corresponds to a single piece and all the images it contains
								// are added sequentially at the end of the piece, after all of the senses.
								// This means the order of runs w/in a piece is: headword run, sense1 run, sense2 run, ... ,
								// [image1 run, caption1 run, copyright&license1 run], [image2 run, caption2 run, copyright&license2 run], ...
								// We need empty paragraphs between the [] textframe chunks, which corresponds to adding an empty paragraph
								// immediately before any image run other than the first image run in a piece.
								if (containsDrawing)
								{
									if (pieceHasImage)
									{
										wordWriter.WordFragment.GetNewParagraph();
										wordWriter.WordFragment.AppendToParagraph(frag, new Run(), false);
									}

									// We have now added at least one image from this piece.
									pieceHasImage = true;
								}

								WP.Paragraph newPar = wordWriter.WordFragment.GetNewParagraph();
								WP.ParagraphProperties paragraphProps =
									new WP.ParagraphProperties(new ParagraphStyleId() { Val = WordStylesGenerator.PictureAndCaptionTextframeStyle });
								newPar.Append(paragraphProps);

								wordWriter.WordFragment.AppendToParagraph(frag, run, false);
							}
							else
							{
								wordWriter.WordFragment.AppendToParagraph(frag, run, wordWriter.ForceNewParagraph);
								wordWriter.ForceNewParagraph = false;
							}

							break;

						case WP.Table table:
							wordWriter.WordFragment.AppendTable(table);

							// Start a new paragraph with the next run to maintain the correct position of the table.
							wordWriter.ForceNewParagraph = true;
							break;

						case WP.Paragraph para:
							wordWriter.WordFragment.AppendParagraph(para);

							// Start a new paragraph with the next run so that it uses the correct style.
							wordWriter.ForceNewParagraph = true;

							break;
						default:
							throw new Exception("Unexpected element type on DocBody: " + elem.GetType().ToString());

					}
				}
			}
		}
		public void EndEntry(IFragmentWriter writer)
		{
			return;
		}
		public void AddCollection(IFragmentWriter writer, ConfigurableDictionaryNode config, bool isBlockProperty, string className, IFragment content)
		{
			if (!content.IsNullOrEmpty())
			{
				((WordFragmentWriter)writer).WordFragment.Append(content);
			}
		}
		public void BeginObjectProperty(IFragmentWriter writer, ConfigurableDictionaryNode config, bool isBlockProperty, string getCollectionItemClassAttribute)
		{
			return;
		}
		public void EndObject(IFragmentWriter writer)
		{
			return;
		}
		public void WriteProcessedContents(IFragmentWriter writer, ConfigurableDictionaryNode config, IFragment contents)
		{
			if (!contents.IsNullOrEmpty())
			{
				((WordFragmentWriter)writer).Insert(contents);
			}
		}
		public IFragment AddImage(ConfigurableDictionaryNode config, string classAttribute, string srcAttribute, string pictureGuid)
		{
			DocFragment imageFrag = new DocFragment();
			WordprocessingDocument wordDoc = imageFrag.DocFrag;
			string partId = AddImagePartToPackage(wordDoc, srcAttribute);
			Drawing image = CreateImage(wordDoc, srcAttribute, partId);

			if (wordDoc.MainDocumentPart is null || wordDoc.MainDocumentPart.Document.Body is null)
			{
				throw new ArgumentNullException("MainDocumentPart and/or Body is null.");
			}

			Run imgRun = new Run();
			imgRun.AppendChild(image);

			// Append the image to body, the image should be in a Run.
			wordDoc.MainDocumentPart.Document.Body.AppendChild(imgRun);
			return imageFrag;
		}
		public IFragment AddImageCaption(ConfigurableDictionaryNode config, IFragment captionContent)
		{
			// ConfiguredLcmGenerator constructs the caption in such a way that every run in captionContent will be in a distinct paragraph.
			// We do need to maintain distinct runs b/c they may each have different character styles.
			// However, all runs in the caption ought to be in a single paragraph.

			var docFrag = new DocFragment();
			if (!captionContent.IsNullOrEmpty())
			{
				// Create a paragraph using the textframe style for captions.
				WP.ParagraphProperties paragraphProps = new WP.ParagraphProperties(
					new ParagraphStyleId() { Val = WordStylesGenerator.PictureAndCaptionTextframeStyle });
				WP.Paragraph captionPara = docFrag.DocBody.AppendChild(new WP.Paragraph(paragraphProps));

				// Clone each caption run and append it to the caption paragraph.
				foreach (Run run in ((DocFragment)captionContent).DocBody.Descendants<Run>())
				{
					captionPara.AppendChild(run.CloneNode(true));
				}
			}
			return docFrag;
		}
		public IFragment GenerateSenseNumber(ConfigurableDictionaryNode senseConfigNode, string formattedSenseNumber, string senseNumberWs)
		{
			var senseOptions = (DictionaryNodeSenseOptions)senseConfigNode?.DictionaryNodeOptions;
			string afterNumber = null;
			string beforeNumber = null;
			string numberStyleName = WordStylesGenerator.SenseNumberStyleName;
			if (senseOptions != null)
			{
				afterNumber = senseOptions.AfterNumber;
				beforeNumber = senseOptions.BeforeNumber;
				if (!string.IsNullOrEmpty(senseOptions.NumberStyle))
				{
					numberStyleName = senseOptions.NumberStyle;
				}
			}
			string displayNameBase = DocFragment.GenerateWsStyleName(Cache, WordStylesGenerator.SenseNumberDisplayName, senseNumberWs);

			// Add the style to the collection and get the unique name.
			string uniqueDisplayName = null;
			// The calls to TryGetStyle() and AddStyle() need to be in the same lock.
			lock (s_styleCollection)
			{
				if (s_styleCollection.TryGetStyle(numberStyleName, displayNameBase, out Style existingStyle))
				{
					uniqueDisplayName = existingStyle.StyleId;
				}
				// If the style is not in the collection, then add it.
				else
				{
					var wsString = WordStylesGenerator.GetWsString(senseNumberWs);

					// Get the style from the LcmStyleSheet.
					var cache = _propertyTable.GetValue<LcmCache>("cache");
					var wsId = cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr(senseNumberWs);
					Style style = WordStylesGenerator.GenerateWordStyleFromLcmStyleSheet(numberStyleName, wsId, _propertyTable);
					Debug.Assert(style.Type == StyleValues.Character);

					style.Append(new BasedOn() { Val = wsString });
					style.StyleId = displayNameBase;
					style.StyleName.Val = style.StyleId;
					uniqueDisplayName = s_styleCollection.AddStyle(style, numberStyleName, style.StyleId);
				}
			}

			// Create the run.
			WP.Run run = new WP.Run();

			// Reference the style name.
			WP.RunProperties runProps = new WP.RunProperties(new RunStyle() { Val = uniqueDisplayName });
			run.Append(runProps);

			// Add characters before the number.
			if (!string.IsNullOrEmpty(beforeNumber))
			{
				WP.Text txt = new WP.Text(beforeNumber);
				txt.Space = SpaceProcessingModeValues.Preserve;
				run.Append(txt);
			}

			// Add the number.
			if (!string.IsNullOrEmpty(formattedSenseNumber))
			{
				WP.Text txt = new WP.Text(formattedSenseNumber);
				txt.Space = SpaceProcessingModeValues.Preserve;
				run.Append(txt);
			}

			// Add characters after the number.
			if (!string.IsNullOrEmpty(afterNumber))
			{
				WP.Text txt = new WP.Text(afterNumber);
				txt.Space = SpaceProcessingModeValues.Preserve;
				run.Append(txt);
			}

			DocFragment senseNum = new DocFragment();
			senseNum.DocBody.AppendChild(run);
			return senseNum;
		}
		public IFragment AddLexReferences(ConfigurableDictionaryNode config, bool generateLexType, IFragment lexTypeContent, string className, IFragment referencesContent, bool typeBefore)
		{
			var fragment = new DocFragment();
			// Generate the factored ref types element (if before).
			if (generateLexType && typeBefore)
			{
				fragment.Append(WriteProcessedObject(config, false, lexTypeContent, className));
			}
			// Then add all the contents for the LexReferences (e.g. headwords)
			fragment.Append(referencesContent);
			// Generate the factored ref types element (if after).
			if (generateLexType && !typeBefore)
			{
				fragment.Append(WriteProcessedObject(config, false, lexTypeContent, className));
			}

			return fragment;
		}
		public void BeginCrossReference(IFragmentWriter writer, ConfigurableDictionaryNode senseConfigNode, bool isBlockProperty, string className)
		{
			return;
		}
		public void EndCrossReference(IFragmentWriter writer)
		{
			return;
		}
		public IFragment WriteProcessedSenses(ConfigurableDictionaryNode config, bool isBlock, IFragment senseContent, string className, IFragment sharedGramInfo)
		{
			// Add Before text for the senses.
			if (!string.IsNullOrEmpty(config.Before))
			{
				var beforeRun = CreateBeforeAfterBetweenRun(config.Before);
				((DocFragment)sharedGramInfo).DocBody.PrependChild(beforeRun);
			}

			sharedGramInfo.Append(senseContent);

			// Add After text for the senses.
			if (!string.IsNullOrEmpty(config.After))
			{
				var afterRun = CreateBeforeAfterBetweenRun(config.After);
				((DocFragment)sharedGramInfo).DocBody.Append(afterRun);
			}

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
		public IFragment GenerateVideoLinkContent(ConfigurableDictionaryNode config, string className, string mediaId, string srcAttribute,
			string caption)
		{
			return new DocFragment("TODO: generate video link content");
		}
		#endregion ILcmContentGenerator functions to implement

		/*
		 * Styles Generator Region
		 */
		#region ILcmStylesGenerator functions to implement
		public void AddGlobalStyles(DictionaryConfigurationModel model, ReadOnlyPropertyTable propertyTable)
		{
			var cache = propertyTable.GetValue<LcmCache>("cache");
			var propStyleSheet = FontHeightAdjuster.StyleSheetFromPropertyTable(propertyTable);

			// TODO: Implement custom bullets & numbering
			// LoadBulletUnicodes();
			// LoadNumberingStyles();

			var letterHeaderStyle = WordStylesGenerator.GenerateLetterHeaderStyle(propertyTable);
			if (letterHeaderStyle != null)
			{
				s_styleCollection.AddStyle(letterHeaderStyle, WordStylesGenerator.LetterHeadingStyleName, letterHeaderStyle.StyleId);
			}

			var beforeAfterBetweenStyle = WordStylesGenerator.GenerateBeforeAfterBetweenStyle(propertyTable);
			if (beforeAfterBetweenStyle != null)
			{
				s_styleCollection.AddStyle(beforeAfterBetweenStyle, WordStylesGenerator.BeforeAfterBetweenStyleName, beforeAfterBetweenStyle.StyleId);
			}

			Styles defaultStyles = WordStylesGenerator.GetDefaultWordStyles(propertyTable, propStyleSheet, model);
			if (defaultStyles != null)
			{
				foreach (WP.Style style in defaultStyles.Descendants<Style>())
				{
					s_styleCollection.AddStyle(style, style.StyleId, style.StyleId);
				}
			}

			// TODO: in openxml, will links be plaintext by default?
			//WordStylesGenerator.MakeLinksLookLikePlainText(_styleSheet);
			// TODO:  Generate style for audiows after we add audio to export
			//WordStylesGenerator.GenerateWordStyleForAudioWs(_styleSheet, cache);
		}

		/// <summary>
		/// Gets the style from the dictionary (if it is in the dictionary). If not in the
		/// dictionary then create the Word style from the LCM Style Sheet and add it to the dictionary.
		/// </summary>
		/// <returns>Returns null if it fails to find or create the character style.</returns>
		private static Style GetOrCreateCharacterStyle(string nodeStyleName, string displayNameBase, ReadOnlyPropertyTable propertyTable)
		{
			Style retStyle = null;
			// The calls to TryGetStyle() and AddStyle() need to be in the same lock.
			lock (s_styleCollection)
			{
				if (s_styleCollection.TryGetStyle(nodeStyleName, displayNameBase, out retStyle))
				{
					if (retStyle.Type != StyleValues.Character)
					{
						return null;
					}
				}
				else
				{
					retStyle = WordStylesGenerator.GenerateWordStyleFromLcmStyleSheet(nodeStyleName, 0, propertyTable);
					if (retStyle == null || retStyle.Type != StyleValues.Character)
					{
						return null;
					}

					s_styleCollection.AddStyle(retStyle, nodeStyleName, displayNameBase);
				}
			}
			return retStyle;
		}

		/// <summary>
		/// Generates paragraph styles that are needed by this node and adds them to the collection.
		/// Character styles will be generated from the code that references the style. This simplifies
		/// the situations where a unique style name is generated, because the reference needs to use the
		/// unique name.
		/// </summary>
		public string AddStyles(ConfigurableDictionaryNode node)
		{
			// The css className isn't important for the Word export.
			var className = $".{CssGenerator.GetClassAttributeForConfig(node)}";

			Styles styleContent = null;
			styleContent = WordStylesGenerator.CheckRangeOfStylesForEmpties(WordStylesGenerator.GenerateParagraphStylesFromConfigurationNode(node, _propertyTable));

			if (styleContent == null)
				return className;
			if (!styleContent.Any())
				return className;

			foreach (Style style in styleContent.Descendants<Style>())
			{
				if (style.Type == StyleValues.Paragraph)
				{
					string oldName = style.StyleId;
					string newName = s_styleCollection.AddStyle(style, style.StyleId, style.StyleId);
					Debug.Assert(oldName == newName, "Not expecting the name for a paragraph style to ever change!");
				}
				else
				{
					Debug.Assert(false, "Should not be adding character styles in here. Instead add them from where the style is referenced.");
				}
			}
			return className;
		}
		public void Init(ReadOnlyPropertyTable propertyTable)
		{
			_propertyTable = propertyTable;
		}
		#endregion ILcmStylesGenerator functions to implement

		// Add a StylesDefinitionsPart to the document. Returns a reference to it.
		public static StyleDefinitionsPart AddStylesPartToPackage(WordprocessingDocument doc)
		{
			StyleDefinitionsPart part;
			part = doc.MainDocumentPart.AddNewPart<StyleDefinitionsPart>();
			Styles root = new Styles();
			root.Save(part);
			return part;
		}

		// Add an ImagePart to the document. Returns the part ID.
		public static string AddImagePartToPackage(WordprocessingDocument doc, string imagePath, ImagePartType imageType = ImagePartType.Jpeg)
		{
			MainDocumentPart mainPart = doc.MainDocumentPart;
			ImagePart imagePart = mainPart.AddImagePart(imageType);
			using (FileStream stream = new FileStream(imagePath, FileMode.Open))
			{
				imagePart.FeedData(stream);
			}

			return mainPart.GetIdOfPart(imagePart);
		}

		public static Drawing CreateImage(WordprocessingDocument doc, string filepath, string partId)
		{
			// Create a bitmap to store the image so we can track/preserve aspect ratio.
			var img = new BitmapImage();

			// Minimize the time that the image file is locked by opening with a filestream to initialize the bitmap image
			using (var fs = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				img.BeginInit();
				img.StreamSource = fs;
				img.EndInit();
			}

			var actWidthPx = img.PixelWidth;
			var actHeightPx = img.PixelHeight;
			var horzRezDpi = img.DpiX;
			var vertRezDpi = img.DpiY;
			var actWidthInches = (float)(actWidthPx / horzRezDpi);
			var actHeightInches = (float)(actHeightPx / vertRezDpi);

			var ratioActualInches = actHeightInches / actWidthInches;
			var ratioMaxInches = (float)(maxImageHeightInches) / (float)(maxImageWidthInches);

			// height/widthInches will store the actual height and width
			// to use for the image in the Word doc.
			float heightInches = maxImageHeightInches;
			float widthInches = maxImageWidthInches;

			// If the ratio of the actual image is greater than the max ratio,
			// we leave height equal to the max height and scale width accordingly.
			if (ratioActualInches >= ratioMaxInches)
			{
				widthInches = actWidthInches * (maxImageHeightInches / actHeightInches);
			}
			// Otherwise, if the ratio of the actual image is less than the max ratio,
			// we leave width equal to the max width and scale height accordingly.
			else if (ratioActualInches < ratioMaxInches)
			{
				heightInches = actHeightInches * (maxImageWidthInches / actWidthInches);
			}

			// Calculate the actual height and width in emus to use for the image.
			const int emusPerInch = 914400;
			var widthEmus = (long)(widthInches * emusPerInch);
			var heightEmus = (long)(heightInches * emusPerInch);

			// We want a 4pt right/left margin--4pt is equal to 0.0553 inches in MS word.
			float rlMarginInches = 0.0553F;

			// Create and add a floating image with image wrap set to top/bottom
			// Name for the image -- the name of the file after all containing folders and the file extension are removed.
			string name = (filepath.Split('\\').Last()).Split('.').First();

			var element = new Drawing(
				new DrawingWP.Inline(
					new DrawingWP.Extent()
					{
						Cx = widthEmus,
						Cy = heightEmus
					},
					new DrawingWP.EffectExtent()
					{
						LeftEdge = 0L,
						TopEdge = 0L,
						RightEdge = 0L,
						BottomEdge = 0L
					},
					new DrawingWP.DocProperties()
					{
						Id = (UInt32Value)1U,
						Name = name
					},
					new DrawingWP.NonVisualGraphicFrameDrawingProperties(
						new XmlDrawing.GraphicFrameLocks() { NoChangeAspect = true }),
					new XmlDrawing.Graphic(
						new XmlDrawing.GraphicData(
							new Pictures.Picture(
								new Pictures.NonVisualPictureProperties(
									new Pictures.NonVisualDrawingProperties()
									{
										Id = (UInt32Value)0U,
										Name = name
									},
									new Pictures.NonVisualPictureDrawingProperties(
										new XmlDrawing.PictureLocks()
											{NoChangeAspect = true, NoChangeArrowheads = true}
									)
								),
								new Pictures.BlipFill(
									new XmlDrawing.Blip(
										new XmlDrawing.BlipExtensionList(
											new XmlDrawing.BlipExtension(
												new DocumentFormat.OpenXml.Office2010.Drawing.UseLocalDpi() {Val = false}
											) { Uri = "{28A0092B-C50C-407E-A947-70E740481C1C}" }
										)
									)
									{
										Embed = partId,
										CompressionState = XmlDrawing.BlipCompressionValues.Print
									},
									new XmlDrawing.SourceRectangle(),
									new XmlDrawing.Stretch(new XmlDrawing.FillRectangle())
								),
								new Pictures.ShapeProperties(
									new XmlDrawing.Transform2D(
										new XmlDrawing.Offset() { X = 0L, Y = 0L },
										new XmlDrawing.Extents()
										{
											Cx = widthEmus,
											Cy = heightEmus
										}
									),
									new XmlDrawing.PresetGeometry(
										new XmlDrawing.AdjustValueList()
									) { Preset = XmlDrawing.ShapeTypeValues.Rectangle },
									new XmlDrawing.NoFill()
								) {BlackWhiteMode = XmlDrawing.BlackWhiteModeValues.Auto}
							)
						) { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }
					)
				)
				{
					DistanceFromTop = (UInt32Value)0U,
					DistanceFromBottom = (UInt32Value)0U,
					DistanceFromLeft = (UInt32Value)0U,
					DistanceFromRight = (UInt32Value)0U
				}
			);

			return element;
		}

		/// <summary>
		/// Creates a run using the text provided and using the style provided.
		/// </summary>
		internal static WP.Run CreateRun(string runText, string styleDisplayName)
		{
			WP.Run run = new WP.Run();
			if (!string.IsNullOrEmpty(styleDisplayName))
			{
				WP.RunProperties runProps =
					new WP.RunProperties(new RunStyle() { Val = styleDisplayName });
				run.Append(runProps);
			}

			if (!string.IsNullOrEmpty(runText))
			{
				WP.Text txt = new WP.Text(runText);
				txt.Space = SpaceProcessingModeValues.Preserve;
				run.Append(txt);
			}
			return run;
		}

		/// <summary>
		/// Creates a BeforeAfterBetween run using the text provided and using the BeforeAfterBetween style.
		/// </summary>
		/// <param name="text">Text for the run.</param>
		/// <returns>The BeforeAfterBetween run.</returns>
		internal static WP.Run CreateBeforeAfterBetweenRun(string text)
		{
			if(text.Contains("\\A"))
			{
				var run = new WP.Run()
				{
					RunProperties = new WP.RunProperties(new RunStyle() { Val = WordStylesGenerator.BeforeAfterBetweenDisplayName })
				};
				// If the before after between text has line break characters return a composite run including the line breaks
				// Use Regex.Matches to capture both the content and the delimiters
				var matches = Regex.Matches(text, @"(\\A|\\0A)|[^\\]*(?:(?=\\A|\\0A)|$)");
				foreach (Match match in matches)
				{
					if (match.Groups[1].Success)
						run.Append(new WP.Break() { Type = BreakValues.TextWrapping });
					else
						run.Append(new WP.Text(match.Value));
				}
				return run;
			}

			return CreateRun(text, WordStylesGenerator.BeforeAfterBetweenDisplayName);
		}

		/// <summary>
		/// Worker method for AddRunStyle(), not intended to be called from other places. If it is
		/// then the the pre-checks on 'style' should be added to this method.
		/// </summary>
		private void AddRunStyle_Worker(WP.Run run, string nodeStyleName, string displayNameBase)
		{
			Style rootStyle = WordStylesGenerator.GenerateWordStyleFromLcmStyleSheet(nodeStyleName, 0, _propertyTable);
			if (rootStyle == null || rootStyle.Type != StyleValues.Character)
			{
				return;
			}
			rootStyle.StyleId = displayNameBase;
			rootStyle.StyleName.Val = rootStyle.StyleId;

			if (run.RunProperties != null)
			{
				if (run.RunProperties.Descendants<RunStyle>().Any())
				{
					string currentRunStyle = run.RunProperties.Descendants<RunStyle>().Last().Val;
					// If the run has a current style, then make the new style based on the current style.
					if (!string.IsNullOrEmpty(currentRunStyle))
					{
						// If the current style is a language tag, then no need to add the separator.
						string displayNameBaseCombined = currentRunStyle.StartsWith("[") ?
							(displayNameBase + currentRunStyle) : (displayNameBase + WordStylesGenerator.StyleSeparator + currentRunStyle);

						// The calls to TryGetStyle() and AddStyle() need to be in the same lock.
						lock (s_styleCollection)
						{
							if (s_styleCollection.TryGetStyle(nodeStyleName, displayNameBaseCombined, out Style existingStyle))
							{
								run.RunProperties.Descendants<RunStyle>().Last().Val = existingStyle.StyleId;
							}
							else
							{
								// Don't create a new style if the current style already has the same root.
								int separatorIndex = currentRunStyle.IndexOf(WordStylesGenerator.StyleSeparator);
								separatorIndex = separatorIndex != -1 ? separatorIndex : currentRunStyle.IndexOf("[");
								bool hasSameRoot = separatorIndex == -1 ? currentRunStyle.Equals(displayNameBase) :
									currentRunStyle.Substring(0, separatorIndex).Equals(displayNameBase);
								if (hasSameRoot)
								{
									return;
								}

								Style basedOnStyle = WordStylesGenerator.GenerateBasedOnCharacterStyle(rootStyle, currentRunStyle, displayNameBaseCombined);
								if (basedOnStyle != null)
								{
									string uniqueDisplayName = s_styleCollection.AddStyle(basedOnStyle, nodeStyleName, basedOnStyle.StyleId);
									run.RunProperties.Descendants<RunStyle>().Last().Val = uniqueDisplayName;
								}
							}
						}
					}
					else
					{
						string uniqueDisplayName = s_styleCollection.AddStyle(rootStyle, nodeStyleName, displayNameBase);
						run.RunProperties.Descendants<RunStyle>().Last().Val = uniqueDisplayName;
					}
				}
				else
				{
					string uniqueDisplayName = s_styleCollection.AddStyle(rootStyle, nodeStyleName, displayNameBase);
					run.RunProperties.Append(new RunStyle() { Val = uniqueDisplayName });
				}
			}
			else
			{
				string uniqueDisplayName = s_styleCollection.AddStyle(rootStyle, nodeStyleName, displayNameBase);
				WP.RunProperties runProps =
					new WP.RunProperties(new RunStyle() { Val = uniqueDisplayName });
				// Prepend RunProperties so it appears before any text elements contained in the run
				run.PrependChild<WP.RunProperties>(runProps);
			}
		}

		/// <summary>
		/// Adds the specified style to either all of the runs contained in the fragment or the last
		/// run in the fragment. If a run does not contain RunProperties or a RunStyle then just add
		/// the specified style. Otherwise create a new style for the run that uses the specified
		/// style but makes it BasedOn the current style that is being used by the run.
		/// </summary>
		/// <param name="frag">The fragment containing the runs that should have the new style applied.</param>
		/// <param name="nodeStyleName">The FLEX style to apply to the runs in the fragment.</param>
		/// <param name="displayNameBase">The style name to display in Word.</param>
		/// <param name="allRuns">If true then apply the style to all runs in the fragment.
		///                       If false then only apply the style to the last run in the fragment.</param>
		public void AddRunStyle(IFragment frag, string nodeStyleName, string displayNameBase, bool allRuns)
		{
			string sDefaultTextStyle = "Default Paragraph Characters";
			if (string.IsNullOrEmpty(nodeStyleName) || nodeStyleName.StartsWith(sDefaultTextStyle) || string.IsNullOrEmpty(displayNameBase))
			{
				return;
			}

			if (allRuns)
			{
				foreach (WP.Run run in ((DocFragment)frag).DocBody.Elements<WP.Run>())
				{
					AddRunStyle_Worker(run, nodeStyleName, displayNameBase);
				}
			}
			else
			{
				List<WP.Run> runList = ((DocFragment)frag).DocBody.Elements<WP.Run>().ToList();
				if (runList.Any())
				{
					AddRunStyle_Worker(runList.Last(), nodeStyleName, displayNameBase);
				}
			}
		}
	}
}
