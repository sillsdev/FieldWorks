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

namespace SIL.FieldWorks.XWorks
{
	// This alias is to be used when creating Wordprocessing Text objects,
	// since there are multiple different Text types across the packages we are using.
	using WP = DocumentFormat.OpenXml.Wordprocessing;

	public class LcmWordGenerator : ILcmContentGenerator, ILcmStylesGenerator
	{
		private LcmCache Cache { get; }
		private static Styles _styleSheet { get; set; } = new Styles();
		private static Dictionary<string, Style> _styleDictionary = new Dictionary<string, Style>();
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

					// Add generated styles into the stylesheet from the dictionary
					foreach (var style in _styleDictionary.Values)
					{
						_styleSheet.AppendChild(style.CloneNode(true));
					}

					// Clone styles from the stylesheet into the word doc's styles xml
					stylePart.Styles = ((Styles)_styleSheet.CloneNode(true));

					// clear the dictionary
					_styleDictionary = new Dictionary<string, Style>();

					// clear the styleSheet
					_styleSheet = new WP.Styles();
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
			return DocFragment.GenerateLetterHeaderDocFragment(headerTextBuilder.ToString(), WordStylesGenerator.LetterHeadingStyleName);
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
				// Only create paragraph, run, and text objects if the string is nonempty
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
			/// <param name="styleName">Letter header style.</param>
			internal static DocFragment GenerateLetterHeaderDocFragment(string str, string styleName)
			{
				var docFrag = new DocFragment();
				// Only create paragraph, run, and text objects if string is nonempty
				if (!string.IsNullOrEmpty(str))
				{
					WP.ParagraphProperties paragraphProps = new WP.ParagraphProperties(new ParagraphStyleId() { Val = styleName });
					WP.Paragraph para = docFrag.DocBody.AppendChild(new WP.Paragraph(paragraphProps));
					WP.Run run = para.AppendChild(new WP.Run());
					// For spaces to show correctly, set preserve spaces on the text element
					WP.Text txt = new WP.Text(str);
					txt.Space = SpaceProcessingModeValues.Preserve;
					run.AppendChild(txt);
				}
				return docFrag;
			}

			public static void LinkStyleOrInheritParentStyle(IFragment content, ConfigurableDictionaryNode config)
			{
				DocFragment frag = ((DocFragment)content);

				// Don't add style for tables.
				if (frag.DocBody.Elements<WP.Table>().FirstOrDefault() != null)
				{
					return;
				}

				if (!string.IsNullOrEmpty(config.Style))
				{
					frag.AddStyleLink(config.Style, config, config.StyleType);
				}
				else if (!string.IsNullOrEmpty(config.Parent?.Style))
				{
					frag.AddStyleLink(config.Parent.Style, config.Parent, config.Parent.StyleType);
				}
			}

			public void AddStyleLink(string styleName, ConfigurableDictionaryNode config, ConfigurableDictionaryNode.StyleTypes styleType)
			{
				styleName = GetWsStyleName(styleName, config);
				if (string.IsNullOrEmpty(styleName))
					return;

				if (styleType == ConfigurableDictionaryNode.StyleTypes.Paragraph)
				{
					if (string.IsNullOrEmpty(ParagraphStyle))
					{
						ParagraphStyle = styleName;
					}
				}
				else
					LinkCharStyle(styleName);
			}

			/// <summary>
			/// Appends the given styleName as a style ID for the last paragraph in the doc, or creates a new paragraph with the given styleID if no paragraph exists.
			/// </summary>
			/// <param name="styleName"></param>
			internal void LinkParaStyle(string styleName)
			{
				if (string.IsNullOrEmpty(styleName))
					return;

				WP.Paragraph par = GetLastParagraph();
				if (par.ParagraphProperties != null)
				{
					// if a style is already linked to the paragraph, return without adding another link
					if (par.ParagraphProperties.Descendants<ParagraphStyleId>().Any())
						return;

					par.ParagraphProperties.PrependChild(new ParagraphStyleId() { Val = styleName });
				}
				else
				{
					WP.ParagraphProperties paragraphProps = new WP.ParagraphProperties(new ParagraphStyleId() { Val = styleName });
					par.PrependChild(paragraphProps);
				}
			}

			private void LinkCharStyle(string styleName)
			{
				WP.Run run = GetLastRun();
				if (run.RunProperties != null)
				{
					// if a style is already linked to the run, replace the stylename and return without adding another link
					if (run.RunProperties.Descendants<RunStyle>().Any())
					{
						run.RunProperties.Descendants<RunStyle>().Last().Val = styleName;
						return;
					}

					run.RunProperties.Append(new RunStyle() { Val = styleName });
				}
				else
				{
					WP.RunProperties runProps =
						new WP.RunProperties(new RunStyle() { Val = styleName });
					// Prepend runproperties so it appears before any text elements contained in the run
					run.PrependChild<WP.RunProperties>(runProps);
				}
			}

			public static string GetWsStyleName(string styleName, ConfigurableDictionaryNode config)
			{
				if (config.DictionaryNodeOptions is DictionaryNodeWritingSystemOptions)
				{
					foreach (var opt in ((DictionaryNodeWritingSystemOptions)config.DictionaryNodeOptions).Options)
					{
						if (opt.IsEnabled)
						{
							// If it's magic then don't return a language tag specific style.
							var possiblyMagic = WritingSystemServices.GetMagicWsIdFromName(opt.Id);
							if (possiblyMagic != 0)
							{
								return styleName;
							}
							// else, the DictionaryNodeOption Id specifies a particular writing system
							// if there is no base style, return just the ws style
							if (styleName == null)
								return WordStylesGenerator.GetWsString(opt.Id);
							// if there is a base style, return the ws-specific version of that style
							return styleName + WordStylesGenerator.GetWsString(opt.Id);
						}
					}
				}
				return styleName;
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
			public void Append(WP.Table table)
			{
				// Deep clone the run b/c of its tree of properties and to maintain styles.
				this.DocBody.AppendChild(table.CloneNode(true));
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
			public void CreateRun(string writingSystem)
			{
				if (writingSystem == null)
					WordFragment.DocBody.AppendChild(new WP.Run());
				else
				{
					var wsString = WordStylesGenerator.GetWsString(writingSystem);
					var run = new WP.Run();
					run.Append(new RunProperties());
					run.RunProperties.Append(new RunStyle() { Val = "span"+wsString });
					WordFragment.DocBody.AppendChild(run);
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
			return content;
		}

		public IFragment GenerateAudioLinkContent(string classname, string srcAttribute, string caption, string safeAudioId)
		{
			// TODO
			return new DocFragment("TODO: generate audio link content");
		}
		public IFragment WriteProcessedObject(bool isBlock, IFragment elementContent, ConfigurableDictionaryNode config, string className)
		{
			return WriteProcessedElementContent(elementContent, config, className);
		}
		public IFragment WriteProcessedCollection(bool isBlock, IFragment elementContent, ConfigurableDictionaryNode config, string className)
		{
			return WriteProcessedElementContent(elementContent, config, className);
		}

		private IFragment WriteProcessedElementContent(IFragment elementContent, ConfigurableDictionaryNode config, string className)
		{
			// Use the style name and type of the config node or its parent to link a style to the elementContent fragment where the processed contents are written.
			DocFragment.LinkStyleOrInheritParentStyle(elementContent, config);

			bool displayEachInAParagraph = config != null &&
										   config.DictionaryNodeOptions is IParaOption &&
										   ((IParaOption)(config.DictionaryNodeOptions)).DisplayEachInAParagraph;

			// Add Before text, if it is not going to be displayed in it's own paragraph.
			if (!displayEachInAParagraph && !string.IsNullOrEmpty(config.Before))
			{
				var beforeRun = CreateBeforeAfterBetweenRun(config.Before);
				((DocFragment)elementContent).DocBody.PrependChild(beforeRun);
			}

			// Add After text, if it is not going to be displayed in it's own paragraph.
			if (!displayEachInAParagraph && !string.IsNullOrEmpty(config.After))
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
		public IFragment GenerateGroupingNode(object field, string className, ConfigurableDictionaryNode config, DictionaryPublicationDecorator publicationDecorator, ConfiguredLcmGenerator.GeneratorSettings settings,
			Func<object, ConfigurableDictionaryNode, DictionaryPublicationDecorator, ConfiguredLcmGenerator.GeneratorSettings, IFragment> childContentGenerator)
		{
			//TODO: handle grouping nodes
			//IFragment docfrag = new DocFragment(...);
			//LinkStyleOrInheritParentStyle(docfrag, config);
			//return docfrag;
			return null;
		}
		public IFragment AddSenseData(IFragment senseNumberSpan, Guid ownerGuid, ConfigurableDictionaryNode config, IFragment senseContent, bool first)
		{
			var senseData = new DocFragment();
			var senseNode = (DictionaryNodeSenseOptions)config?.DictionaryNodeOptions;
			bool eachInAParagraph = false;
			bool firstSenseInline = false;
			string afterNumber = null;
			string beforeNumber = null;
			if (senseNode != null)
			{
				eachInAParagraph = senseNode.DisplayEachSenseInAParagraph;
				firstSenseInline = senseNode.DisplayFirstSenseInline;
				afterNumber = senseNode.AfterNumber;
				beforeNumber = senseNode.BeforeNumber;
			}

			// We want a break before the first sense item, between items, and after the last item.
			// So, only add a break before the content if it is the first sense and it's not displayed in-line.
			if (eachInAParagraph && first && !firstSenseInline)
			{
				senseData.AppendBreak();
			}

			// Add sense numbers if needed
			if (!senseNumberSpan.IsNullOrEmpty())
			{
				if (!string.IsNullOrEmpty(beforeNumber))
				{
					senseData.Append(beforeNumber);
				}
				senseData.Append(senseNumberSpan);
				if (!string.IsNullOrEmpty(afterNumber))
				{
					senseData.Append(afterNumber);
				}
			}

			senseData.Append(senseContent);

			if (eachInAParagraph)
			{
				senseData.AppendBreak();
			}

			return senseData;
		}
		public IFragment AddCollectionItem(bool isBlock, string collectionItemClass, ConfigurableDictionaryNode config, IFragment content, bool first)
		{
			if (!string.IsNullOrEmpty(config.Style))
			{
				if (isBlock && (config.StyleType == ConfigurableDictionaryNode.StyleTypes.Paragraph))
					((DocFragment)content).AddStyleLink(config.Style, config, ConfigurableDictionaryNode.StyleTypes.Paragraph);

				else if (!isBlock)
					((DocFragment)content).AddStyleLink(config.Style, config,ConfigurableDictionaryNode.StyleTypes.Character);
			}

			var collData = CreateFragment();
			bool eachInAParagraph = false;
			if (config != null &&
				config.DictionaryNodeOptions is IParaOption &&
				((IParaOption)(config.DictionaryNodeOptions)).DisplayEachInAParagraph)
			{
				eachInAParagraph = true;

				// We want a break before the first collection item, between items, and after the last item.
				// So, only add a break before the content if it is the first.
				if (first)
				{
					collData.AppendBreak();
				}
			}

			// Add Between text, if it is not going to be displayed in it's own paragraph
			// and it is not the first item in the collection.
			if (!first &&
				config != null &&
				config.DictionaryNodeOptions is IParaOption &&
				!eachInAParagraph &&
				!string.IsNullOrEmpty(config.Between))
			{
				var betweenRun = CreateBeforeAfterBetweenRun(config.Between);
				((DocFragment)collData).DocBody.Append(betweenRun);
			}

			collData.Append(content);

			if (eachInAParagraph)
			{
				collData.AppendBreak();
			}

			return collData;
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

		public IFragmentWriter CreateWriter(IFragment frag)
		{
			return new WordFragmentWriter((DocFragment)frag);
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
		/// Add a new run to the writers WordFragment DocBody.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="writingSystem"></param>
		public void StartRun(IFragmentWriter writer, string writingSystem)
		{
			((WordFragmentWriter)writer).CreateRun(writingSystem);
		}
		public void EndRun(IFragmentWriter writer)
		{
			// Ending the run should be a null op for word writer
			// Beginning a new run is sufficient to end the old run
			// and to ensure new styles/content are applied to the new run.
		}

		/// <summary>
		/// Set the style for a specific run.
		/// This is needed to set the specific style for any field that allows the
		/// default style to be overridden (Table Cell, Custom Field, Note...).
		/// </summary>
		public void SetRunStyle(IFragmentWriter writer, ConfigurableDictionaryNode config, ReadOnlyPropertyTable propertyTable, string writingSystem, string runStyle, bool error)
		{
			if (!string.IsNullOrEmpty(runStyle))
			{
				// Add the style link.
				((WordFragmentWriter)writer).WordFragment.AddStyleLink(runStyle, config, ConfigurableDictionaryNode.StyleTypes.Character);

				// Only add the style to the styleSheet if not already there.
				if (!_styleSheet.ChildElements.Any(p => ((Style)p).StyleId == runStyle))
				{
					int ws = Cache.WritingSystemFactory.GetWsFromStr(writingSystem);
					var wpStyle = WordStylesGenerator.GenerateWordStyleFromLcmStyleSheet(runStyle, ws, _propertyTable);
					_styleSheet.Append(wpStyle);
				}
			}
		}
		public void StartLink(IFragmentWriter writer, ConfigurableDictionaryNode config, Guid destination)
		{
			if (config != null && !string.IsNullOrEmpty(config.Style))
			{
				((WordFragmentWriter)writer).WordFragment.AddStyleLink(config.Style, config, ConfigurableDictionaryNode.StyleTypes.Character);
			}
			return;
		}
		public void StartLink(IFragmentWriter writer, ConfigurableDictionaryNode config, string externalDestination)
		{
			if (config != null && !string.IsNullOrEmpty(config.Style))
			{
				((WordFragmentWriter)writer).WordFragment.AddStyleLink(config.Style, config, ConfigurableDictionaryNode.StyleTypes.Character);
			}
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
		public void AddLineBreakInRunContent(IFragmentWriter writer)
		{
			((WordFragmentWriter)writer).WordFragment.GetLastRun()
				.AppendChild(new WP.Break());
		}
		public void StartTable(IFragmentWriter writer)
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
			WP.ParagraphProperties paragraphProps = new WP.ParagraphProperties(new ParagraphStyleId() {Val = config.Style});
			entryPar.Append(paragraphProps);

			// Create the 'continuation' style for the entry. This style will be the same as the style for the entry with the only
			// difference being that it does not contain the first line indenting (since it is a continuation of the same entry).
			AddStyles(config, true);
		}
		public void AddEntryData(IFragmentWriter writer, List<ConfiguredLcmGenerator.ConfigFragment> pieces)
		{
			// TODO: the docfragment is now accessible via piece.Frag, and the configurabledictionarynode via piece.Config -- use this info to handle before/after content & display in separate paragraphs.

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
							if (config.Label == "Pictures" || config.Parent?.Label == "Pictures")
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
								if (run.Descendants<Drawing>().Any())
								{
									if (pieceHasImage)
									{
										wordWriter.WordFragment.GetNewParagraph();
										wordWriter.WordFragment.AppendToParagraph(frag, new Run(), false);
									}

									// We have now added at least one image from this piece.
									pieceHasImage = true;
								}

								wordWriter.WordFragment.GetNewParagraph();
								wordWriter.WordFragment.AppendToParagraph(frag, run, false);
								wordWriter.WordFragment.LinkParaStyle(WordStylesGenerator.PictureAndCaptionTextframeStyle);
							}

							else
							{
								wordWriter.WordFragment.AppendToParagraph(frag, run, wordWriter.ForceNewParagraph);
								wordWriter.ForceNewParagraph = false;
								wordWriter.WordFragment.LinkParaStyle(frag.ParagraphStyle);
							}

							break;

						case WP.Table table:
							wordWriter.WordFragment.Append(table);

							// Start a new paragraph with the next run to maintain the correct position of the table.
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
		public void AddCollection(IFragmentWriter writer, bool isBlockProperty, string className, ConfigurableDictionaryNode config, string content)
		{
			var frag = ((WordFragmentWriter)writer).WordFragment;
			if (isBlockProperty && (config.StyleType == ConfigurableDictionaryNode.StyleTypes.Paragraph))
				frag.AddStyleLink(config.Style, config, ConfigurableDictionaryNode.StyleTypes.Paragraph);
			else if (!isBlockProperty)
				frag.AddStyleLink(config.Style, config, ConfigurableDictionaryNode.StyleTypes.Character);

			if (!string.IsNullOrEmpty(content))
			{
				frag.Append(content);
			}
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
			if (!contents.IsNullOrEmpty())
			{
				((WordFragmentWriter)writer).Insert(contents);
			}
		}
		public IFragment AddImage(string classAttribute, string srcAttribute, string pictureGuid)
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
			RunProperties imgProperties = new RunProperties();

			// Append the image to body, the image should be in a Run.
			wordDoc.MainDocumentPart.Document.Body.AppendChild(imgRun);
			return imageFrag;
		}
		public IFragment AddImageCaption(string captionContent)
		{
			return new DocFragment(captionContent);
		}
		public IFragment GenerateSenseNumber(string formattedSenseNumber, string senseNumberWs, ConfigurableDictionaryNode senseConfigNode)
		{
			DocFragment senseNum = new DocFragment(formattedSenseNumber);
			senseNum.AddStyleLink(WordStylesGenerator.SenseNumberStyleName, senseConfigNode, ConfigurableDictionaryNode.StyleTypes.Character);
			return senseNum;
		}
		public IFragment AddLexReferences(bool generateLexType, IFragment lexTypeContent, ConfigurableDictionaryNode config, string className, string referencesContent, bool typeBefore)
		{
			var fragment = new DocFragment();
			// Generate the factored ref types element (if before).
			if (generateLexType && typeBefore)
			{
				fragment.Append(WriteProcessedObject(false, lexTypeContent, config, className));
			}
			// Then add all the contents for the LexReferences (e.g. headwords)
			fragment.Append(new DocFragment(referencesContent));
			// Generate the factored ref types element (if after).
			if (generateLexType && !typeBefore)
			{
				fragment.Append(WriteProcessedObject(false, lexTypeContent, config, className));
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
		public IFragment WriteProcessedSenses(bool isBlock, IFragment senseContent, ConfigurableDictionaryNode config, string className, IFragment sharedGramInfo)
		{
			// Add Before text for the sharedGramInfo.
			if (!string.IsNullOrEmpty(config.Before))
			{
				var beforeRun = CreateBeforeAfterBetweenRun(config.Before);
				((DocFragment)sharedGramInfo).DocBody.PrependChild(beforeRun);
			}

			// Add After text for the sharedGramInfo.
			if (!string.IsNullOrEmpty(config.After))
			{
				var afterRun = CreateBeforeAfterBetweenRun(config.After);
				((DocFragment)sharedGramInfo).DocBody.Append(afterRun);
			}

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

			var letterHeaderStyle = WordStylesGenerator.GenerateLetterHeaderStyle(propertyTable, propStyleSheet);
			if (letterHeaderStyle != null)
				_styleSheet.Append(letterHeaderStyle);

			var beforeAfterBetweenStyle = WordStylesGenerator.GenerateBeforeAfterBetweenStyle(propertyTable);
			if (beforeAfterBetweenStyle != null)
				_styleSheet.Append(beforeAfterBetweenStyle);

			Styles defaultStyles = WordStylesGenerator.GetDefaultWordStyles(propertyTable, propStyleSheet, model);
			if (defaultStyles != null)
			{
				foreach (WP.Style style in defaultStyles.Descendants<Style>())
				{
					_styleSheet.Append(style.CloneNode(true));
				}
			}

			// TODO: in openxml, will links be plaintext by default?
			//WordStylesGenerator.MakeLinksLookLikePlainText(_styleSheet);
			// TODO:  Generate style for audiows after we add audio to export
			//WordStylesGenerator.GenerateWordStyleForAudioWs(_styleSheet, cache);
		}
		public string AddStyles(ConfigurableDictionaryNode node)
		{
			return AddStyles(node, false);
		}

		/// <summary>
		/// Generates styles that are needed by this node and adds them to the dictionary.
		/// </summary>
		/// <param name="addEntryContinuationStyle">If true then generate the 'continuation' style for the node.
		///                                         If false then generate the regular (non-continuation) styles for the node.</param>
		/// <returns></returns>
		public string AddStyles(ConfigurableDictionaryNode node, bool addEntryContinuationStyle)
		{
			// The css className isn't important for the Word export.
			// Styles should be stored in the dictionary based on their stylenames.
			// Generate all styles that are needed by this class and add them to the dictionary with their stylename as the key.
			var className = $".{CssGenerator.GetClassAttributeForConfig(node)}";

			lock (_styleDictionary)
			{
				Styles styleContent = null;
				if (addEntryContinuationStyle)
				{
					styleContent = WordStylesGenerator.CheckRangeOfStylesForEmpties(WordStylesGenerator.GenerateContinuationWordStyles(node, _propertyTable));
				}
				else
				{
					styleContent = WordStylesGenerator.CheckRangeOfStylesForEmpties(WordStylesGenerator.GenerateWordStylesFromConfigurationNode(node, className, _propertyTable));
				}
				if (styleContent == null)
					return className;
				if (!styleContent.Any())
					return className;

				foreach (Style style in styleContent.Descendants<Style>())
				{
					string styleName = style.StyleId;
					if (!_styleDictionary.ContainsKey(styleName))
					{
						_styleDictionary[styleName] = style;
					}
					// If the content is the same, we don't need to do anything--the style is already in the dictionary.
					// But if the content is NOT the same, re-name this style and add it to the dictionary.
					else if (!WordStylesGenerator.AreStylesEquivalent(_styleDictionary[styleName], style))
					{
						// Otherwise get a unique but useful style name and re-name the style
						styleName = GetBestUniqueNameForNode(_styleDictionary, node);
						style.StyleId = styleName;
						style.StyleName = new StyleName() { Val = styleName };
						_styleDictionary[styleName] = style;
					}
				}
				return className;
			}
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
		/// Finds an unused class name for the configuration node. This should be called when there are two nodes in the <code>DictionaryConfigurationModel</code>
		/// have the same class name, but different style content. We want this name to be usefully recognizable.
		/// </summary>
		/// <returns></returns>
		public static string GetBestUniqueNameForNode(Dictionary<string, Style> styles, ConfigurableDictionaryNode node)
		{
			Guard.AgainstNull(node.Parent, "There should not be duplicate class names at the top of tree.");
			// First try appending the parent node classname.
			var className =  node.Style + "-" + node.Parent.Style;

			string classNameBase = className;
			int counter = 0;
			while (styles.ContainsKey(className))
			{
				className = $"{classNameBase}-{++counter}";
			}
			return className;
		}

		/// <summary>
		/// Creates a BeforeAfterBetween run using the text provided and using the BeforeAfterBetween style.
		/// </summary>
		/// <param name="text">Text for the run.</param>
		/// <returns>The BeforeAfterBetween run.</returns>
		private WP.Run CreateBeforeAfterBetweenRun(string text)
		{
			WP.Run run = new WP.Run();
			WP.RunProperties runProps =
				new WP.RunProperties(new RunStyle() { Val = WordStylesGenerator.BeforeAfterBetweenStyleName });
			run.Append(runProps);

			WP.Text txt = new WP.Text(text);
			txt.Space = SpaceProcessingModeValues.Preserve;
			run.Append(txt);

			return run;
		}
	}
}
