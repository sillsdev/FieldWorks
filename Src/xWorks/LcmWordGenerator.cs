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
using SIL.LCModel.Core.KernelInterfaces;

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
		public static bool IsBidi { get; private set; }

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
				IsBidi = ConfiguredLcmGenerator.IsEntryStyleRtl(readOnlyPropertyTable, configuration);
				// Call GeneratorSettings with relativesPaths = false but useUri = false because that works better for Word.
				var settings = new ConfiguredLcmGenerator.GeneratorSettings(cache, readOnlyPropertyTable, false, false, true, System.IO.Path.GetDirectoryName(filePath),
							IsBidi, System.IO.Path.GetFileName(cssPath) == "configured.css")
							{ ContentGenerator = generator, StylesGenerator = generator};
				settings.StylesGenerator.AddGlobalStyles(configuration, readOnlyPropertyTable);
				string lastHeader = null;
				bool firstHeader = true;
				string firstGuidewordStyle = null;
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
							ref lastHeader, col, settings, readOnlyPropertyTable, propStyleSheet, firstHeader, clerk );
						firstHeader = false;

						// If needed, append letter header to the word doc
						if (!letterHeader.IsNullOrEmpty())
							fragment.Append(letterHeader);

						// Append the entry to the word doc
						fragment.Append(entry.Item2);

						if (string.IsNullOrEmpty(firstGuidewordStyle))
						{
							firstGuidewordStyle = GetFirstGuidewordStyle((DocFragment)entry.Item2, configuration.Type);
						}
					}
				}
				col?.Dispose();

				// Set the last section of the document to be two columns and add the page headers. (The last section
				// is all the entries after the last letter header.) For the last section this information is stored
				// different than all the other sections. It is stored as the last child element of the body.
				var sectProps = new SectionProperties(
					new HeaderReference() { Id = WordStylesGenerator.PageHeaderIdEven, Type = HeaderFooterValues.Even },
					new HeaderReference() { Id = WordStylesGenerator.PageHeaderIdOdd, Type = HeaderFooterValues.Default },
					new Columns() { EqualWidth = true, ColumnCount = 2 },
					new SectionType() { Val = SectionMarkValues.Continuous }
					);
				// Set the section to BiDi so the columns are displayed right to left.
				if (IsBidi)
				{
					sectProps.Append(new BiDi());
				}
				fragment.DocBody.Append(sectProps);

				if (progress != null)
					progress.Message = xWorksStrings.ksGeneratingStyleInfo;

				// Generate styles
				StyleDefinitionsPart stylePart = fragment.mainDocPart.StyleDefinitionsPart;
				NumberingDefinitionsPart numberingPart = fragment.mainDocPart.NumberingDefinitionsPart;
				if (stylePart == null)
				{
					// Initialize word doc's styles xml
					stylePart = AddStylesPartToPackage(fragment.DocFrag);
					Styles styleSheet = new Styles();

					// Add generated styles into the stylesheet from the collection.
					var styleElements = s_styleCollection.GetStyleElements();
					foreach (var styleElement in styleElements)
					{
						// Generate bullet and numbering data.
						if (styleElement.BulletInfo.HasValue)
						{
							// Initialize word doc's numbering part one time.
							if (numberingPart == null)
							{
								numberingPart = AddNumberingPartToPackage(fragment.DocFrag);
							}

							GenerateBulletAndNumberingData(styleElement, numberingPart);
						}
						styleSheet.AppendChild(styleElement.Style.CloneNode(true));
					}

					// Clear the collection.
					s_styleCollection.Clear();

					// Clone styles from the stylesheet into the word doc's styles xml
					stylePart.Styles = ((Styles)styleSheet.CloneNode(true));
				}

				// Add the page headers.
				var headerParts = fragment.mainDocPart.HeaderParts;
				if (!headerParts.Any())
				{
					AddPageHeaderPartsToPackage(fragment.DocFrag, firstGuidewordStyle);
				}

				// Add document settings
				DocumentSettingsPart settingsPart = fragment.mainDocPart.DocumentSettingsPart;
				if (settingsPart == null)
				{
					// Initialize word doc's settings part
					settingsPart = AddDocSettingsPartToPackage(fragment.DocFrag);

					settingsPart.Settings = new WP.Settings(
						new Compatibility(
							new CompatibilitySetting()
							{
								Name = CompatSettingNameValues.CompatibilityMode,
								// val determines the version of word we are targeting.
								// 14 corresponds to Office 2010; 16 would correspond to Office 2019
								Val = new StringValue("16"),
								Uri = new StringValue("http://schemas.microsoft.com/office/word")
							},
							new CompatibilitySetting()
							{
								// specify that table style should not be overridden
								Name = CompatSettingNameValues.OverrideTableStyleFontSizeAndJustification,
								Val = new StringValue("0"),
								Uri = new StringValue("http://schemas.microsoft.com/office/word")
							},
							new EvenAndOddHeaders()    // Use different page headers for the even and odd pages.

							// If in the future, if we find that certain style items are different in different versions of word,
							// it may help to specify more compatibility settings.
							// A full list of all possible compatibility settings may be found here:
							// https://learn.microsoft.com/en-us/dotnet/api/documentformat.openxml.wordprocessing.compatsettingnamevalues?view=openxml-3.0.1
						)
					);
					settingsPart.Settings.Save();
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

		internal static IFragment GenerateLetterHeaderIfNeeded(ICmObject entry, ref string lastHeader, Collator headwordWsCollator,
			ConfiguredLcmGenerator.GeneratorSettings settings, ReadOnlyPropertyTable propertyTable, LcmStyleSheet mediatorStyleSheet,
			bool firstHeader, RecordClerk clerk = null)
		{
			StringBuilder headerTextBuilder = ConfiguredLcmGenerator.GenerateLetterHeaderIfNeeded(entry, ref lastHeader,
				headwordWsCollator, settings, clerk);

			// Create LetterHeader doc fragment and link it with the letter heading style.
			return DocFragment.GenerateLetterHeaderDocFragment(headerTextBuilder.ToString(), WordStylesGenerator.LetterHeadingDisplayName, firstHeader);
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
			/// <param name="firstHeader">True if this is the first header being written.</param>
			internal static DocFragment GenerateLetterHeaderDocFragment(string str, string styleDisplayName, bool firstHeader)
			{
				var docFrag = new DocFragment();
				// Only create paragraph, run, and text objects if string is nonempty
				if (!string.IsNullOrEmpty(str))
				{
					// Don't add this paragraph before the first letter header. It results in an extra blank line.
					if (!firstHeader)
					{
						// Everything other than the Letter Header should be 2 columns. Create a empty
						// paragraph with two columns for the last paragraph in the section that uses 2
						// columns. (The section is all the entries after the previous letter header.)
						var sectProps2 = new SectionProperties(
							new HeaderReference() { Id = WordStylesGenerator.PageHeaderIdEven, Type = HeaderFooterValues.Even },
							new HeaderReference() { Id = WordStylesGenerator.PageHeaderIdOdd, Type = HeaderFooterValues.Default },
							new Columns() { EqualWidth = true, ColumnCount = 2 },
							new SectionType() { Val = SectionMarkValues.Continuous }
						);
						// Set the section to BiDi so the columns are displayed right to left.
						if (IsBidi)
						{
							sectProps2.Append(new BiDi());
						}
						docFrag.DocBody.AppendChild(new WP.Paragraph(new WP.ParagraphProperties(sectProps2)));
					}

					// Create the letter header in a paragraph.
					WP.ParagraphProperties paragraphProps = new WP.ParagraphProperties(new ParagraphStyleId() { Val = styleDisplayName });
					WP.Paragraph para = docFrag.DocBody.AppendChild(new WP.Paragraph(paragraphProps));
					WP.Run run = para.AppendChild(new WP.Run());
					// For spaces to show correctly, set preserve spaces on the text element
					WP.Text txt = new WP.Text(str);
					txt.Space = SpaceProcessingModeValues.Preserve;
					run.AppendChild(txt);

					// Only the Letter Header should be 1 column. Create a empty paragraph with one
					// column so the previous letter header paragraph uses 1 column.
					var sectProps1 = new SectionProperties(
						new HeaderReference() { Id = WordStylesGenerator.PageHeaderIdEven, Type = HeaderFooterValues.Even },
						new HeaderReference() { Id = WordStylesGenerator.PageHeaderIdOdd, Type = HeaderFooterValues.Default },
						new Columns() { EqualWidth = true, ColumnCount = 1 },
						new SectionType() { Val = SectionMarkValues.Continuous }
					);
					// Set the section to BiDi so the columns are displayed right to left.
					if (IsBidi)
					{
						sectProps1.Append(new BiDi());
					}
					docFrag.DocBody.AppendChild(new WP.Paragraph(new WP.ParagraphProperties(sectProps1)));
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
						this.DocBody.AppendChild(CloneImageElement(frag, elem));
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
			/// <param name="copyFromFrag">If the table contains pictures, then this is the fragment
			///                            where we copy the picture data from.</param>
			/// <param name="table">The table to append.</param>
			public void AppendTable(IFragment copyFromFrag, WP.Table table)
			{
				// Deep clone the run b/c of its tree of properties and to maintain styles.
				this.DocBody.AppendChild(CloneElement(copyFromFrag, table));
			}

			/// <summary>
			/// Append a paragraph to the doc fragment.
			/// </summary>
			/// <param name="copyFromFrag">If the paragraph contains pictures, then this is the fragment
			///                            where we copy the picture data from.</param>
			/// <param name="para">The paragraph to append.</param>
			public void AppendParagraph(IFragment copyFromFrag, WP.Paragraph para)
			{
				// Deep clone the run b/c of its tree of properties and to maintain styles.
				this.DocBody.AppendChild(CloneElement(copyFromFrag, para));
			}


			/// <summary>
			/// Appends a new run inside the last paragraph of the doc fragment--creates a new paragraph if none
			/// exists or if forceNewParagraph is true.
			/// The run will be added to the end of the paragraph.
			/// </summary>
			/// <param name="run">The run to append.</param>
			/// <param name="forceNewParagraph">Even if a paragraph exists, force the creation of a new paragraph.</param>
			public void AppendToParagraph(IFragment fragToCopy, Run run, bool forceNewParagraph)
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
				lastPar.AppendChild(CloneElement(fragToCopy, run));
			}

			/// <summary>
			/// Does a deep clone of the element.  If there is picture data then that is cloned
			/// from the copyFromFrag into 'this' frag.
			/// </summary>
			/// <param name="copyFromFrag">If the element contains pictures, then this is the fragment
			///                            where we copy the picture data from.</param>
			/// <param name="elem">Element to clone.</param>
			/// <returns>The cloned element.</returns>
			public OpenXmlElement CloneElement(IFragment copyFromFrag, OpenXmlElement elem)
			{
				if (elem.Descendants<DocumentFormat.OpenXml.Drawing.Blip>().Any())
				{
					return CloneImageElement(copyFromFrag, elem);
				}
				return elem.CloneNode(true);
			}

			/// <summary>
			/// Clones and returns a element containing an image.
			/// </summary>
			/// <param name="copyFromFrag">The fragment where we copy the picture data from.</param>
			/// <param name="elem">Element to clone.</param>
			/// <returns>The cloned element.</returns>
			public OpenXmlElement CloneImageElement(IFragment copyFromFrag, OpenXmlElement elem)
			{
				var clonedElem = elem.CloneNode(true);
				clonedElem.Descendants<DocumentFormat.OpenXml.Drawing.Blip>().ToList().ForEach(
					blip =>
					{
						var newRelation =
							CopyImage(DocFrag, blip.Embed, ((DocFragment)copyFromFrag).DocFrag);
						// Update the relationship ID in the cloned blip element.
						blip.Embed = newRelation;
					});
				clonedElem.Descendants<DocumentFormat.OpenXml.Vml.ImageData>().ToList().ForEach(
					imageData =>
					{
						var newRelation = CopyImage(DocFrag, imageData.RelationshipId, ((DocFragment)copyFromFrag).DocFrag);
						// Update the relationship ID in the cloned image data element.
						imageData.RelationshipId = newRelation;
					});
				return clonedElem;
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
				var run = new WP.Run();
				string uniqueDisplayName = null;
				string displayNameBase = (config == null || writingSystem == null) ?
					null : DocFragment.GetWsStyleName(cache, config, writingSystem);

				if (!string.IsNullOrEmpty(displayNameBase))
				{
					// The calls to TryGetStyle() and AddStyle() need to be in the same lock.
					lock (s_styleCollection)
					{
						if (s_styleCollection.TryGetStyle(config.Style, displayNameBase, out StyleElement existingStyle))
						{
							uniqueDisplayName = existingStyle.Style.StyleId;
						}
						// If the style is not in the collection, then add it.
						else
						{
							var wsString = WordStylesGenerator.GetWsString(writingSystem);

							// Get the style from the LcmStyleSheet, using the style name defined in the config.
							if (!string.IsNullOrEmpty(config.Style))
							{
								var wsId = cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr(writingSystem);
								Style style = WordStylesGenerator.GenerateCharacterStyleFromLcmStyleSheet(config.Style, wsId, propTable);
								if (style == null)
								{
									// If we hit this assert, then we might end up referencing a style that
									// does not get created.
									Debug.Assert(false);
								}
								else
								{
									style.Append(new BasedOn() { Val = wsString });
									style.StyleId = displayNameBase;
									style.StyleName.Val = style.StyleId;
									bool wsIsRtl = IsWritingSystemRightToLeft(cache, wsId);
									uniqueDisplayName = s_styleCollection.AddCharacterStyle(style, config.Style, style.StyleId, wsId, wsIsRtl);
								}
							}
							// There is no style name defined in the config so generate a style that is identical to the writing system style
							// except that it contains a display name that the user wants to see in the Word Styles.
							// (example: "Reverse Abbreviation[lang='en']")
							else
							{
								StyleElement rootElem = s_styleCollection.GetStyleElement(wsString);
								// rootElem can be null, see LT-21981.
								Style rootStyle = rootElem?.Style;
								if (rootStyle != null)
								{
									Style basedOnStyle = WordStylesGenerator.GenerateBasedOnCharacterStyle(new Style(), wsString, displayNameBase);
									if (basedOnStyle != null)
									{
										uniqueDisplayName = s_styleCollection.AddCharacterStyle(basedOnStyle, config.Style, basedOnStyle.StyleId,
											rootElem.WritingSystemId, rootElem.WritingSystemIsRtl);
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
						run.Append(GenerateRunProperties(uniqueDisplayName));
					}
				}

				// Add Between text, if it is not the first item.
				if (!first &&
					config != null &&
					!string.IsNullOrEmpty(config.Between))
				{
					var betweenRun = CreateBeforeAfterBetweenRun(config.Between, uniqueDisplayName);
					WordFragment.DocBody.Append(betweenRun);
				}

				// Add the run.
				WordFragment.DocBody.AppendChild(run);
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
						if (runStyle != null && runStyle.Val.ToString().StartsWith(WordStylesGenerator.BeforeAfterBetweenDisplayName))
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
			// We are not planning to support audio and video content for Word Export.
			return new DocFragment();
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

			bool eachInAParagraph = config != null &&
								  config.DictionaryNodeOptions is IParaOption &&
								  ((IParaOption)(config.DictionaryNodeOptions)).DisplayEachInAParagraph;
			string styleDisplayName = GetUniqueDisplayName(config, elementContent);


			// Add Before text, if it is not going to be displayed in a paragraph.
			if (!eachInAParagraph && !string.IsNullOrEmpty(config.Before))
			{
				var beforeRun = CreateBeforeAfterBetweenRun(config.Before, styleDisplayName);
				((DocFragment)elementContent).DocBody.PrependChild(beforeRun);
			}

			// Add After text, if it is not going to be displayed in a paragraph.
			if (!eachInAParagraph && !string.IsNullOrEmpty(config.After))
			{
				var afterRun = CreateBeforeAfterBetweenRun(config.After, styleDisplayName);
				((DocFragment)elementContent).DocBody.Append(afterRun);
			}

			// Add Bullet and Numbering Data to lists.
			AddBulletAndNumberingData(elementContent, config, eachInAParagraph);
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
			bool eachInAParagraph = config != null &&
								  config.DictionaryNodeOptions is DictionaryNodeGroupingOptions &&
								  ((DictionaryNodeGroupingOptions)(config.DictionaryNodeOptions)).DisplayEachInAParagraph;
			IFragment childContent = null;

			// Display in its own paragraph, so the group style can be applied to all of the runs
			// contained in it.
			if (eachInAParagraph)
			{
				groupPara = new WP.Paragraph();
			}

			// Add the group data.
			foreach (var child in config.ReferencedOrDirectChildren)
			{
				childContent = childContentGenerator(field, child, publicationDecorator, settings);
				if (eachInAParagraph)
				{
					var elements = ((DocFragment)childContent).DocBody.Elements().ToList();
					foreach (OpenXmlElement elem in elements)
					{
						// Deep clone the run b/c of its tree of properties and to maintain styles.
						groupPara.AppendChild(groupData.CloneElement(childContent, elem));
					}
				}
				else
				{
					groupData.Append(childContent);
				}
			}

			string styleDisplayName = GetUniqueDisplayName(config, childContent);

			// Add Before text, if it is not going to be displayed in a paragraph.
			if (!eachInAParagraph && !string.IsNullOrEmpty(config.Before))
			{
				var beforeRun = CreateBeforeAfterBetweenRun(config.Before, styleDisplayName);
				groupData.DocBody.PrependChild(beforeRun);
			}

			// Add After text, if it is not going to be displayed in a paragraph.
			if (!eachInAParagraph && !string.IsNullOrEmpty(config.After))
			{
				var afterRun = CreateBeforeAfterBetweenRun(config.After, styleDisplayName);
				groupData.DocBody.Append(afterRun);
			}

			// Don't add an empty paragraph to the groupData fragment.
			if (groupPara != null && groupPara.HasChildren)
			{
				// Add the group style.
				if (!string.IsNullOrEmpty(config.Style))
				{
					WP.ParagraphProperties paragraphProps =
						new WP.ParagraphProperties(new ParagraphStyleId() { Val = config.DisplayLabel });
					groupPara.PrependChild(paragraphProps);
				}
				groupData.DocBody.AppendChild(groupPara);
			}

			return groupData;
		}

		public IFragment AddSenseData(ConfigurableDictionaryNode config, IFragment senseNumberSpan, Guid ownerGuid, IFragment senseContent, bool first)
		{
			var senseData = new DocFragment();
			WP.Paragraph newPara = null;
			var senseNode = (DictionaryNodeSenseOptions)config?.DictionaryNodeOptions;
			bool eachInAParagraph = false;
			bool firstSenseInline = false;
			if (senseNode != null)
			{
				eachInAParagraph = senseNode.DisplayEachSenseInAParagraph;
				firstSenseInline = senseNode.DisplayFirstSenseInline;
			}

			bool inAPara = eachInAParagraph && (!first || !firstSenseInline);
			if (inAPara)
			{
				newPara = new WP.Paragraph();
			}

			// Add Between text, if it is not going to be displayed in a paragraph
			// and it is not the first item.
			if (!first &&
				config != null &&
				!eachInAParagraph &&
				!string.IsNullOrEmpty(config.Between))
			{
				string styleDisplayName = GetUniqueDisplayName(config, senseContent);
				var betweenRun = CreateBeforeAfterBetweenRun(config.Between, styleDisplayName);
				senseData.DocBody.Append(betweenRun);
			}

			// Add sense numbers if needed
			if (!senseNumberSpan.IsNullOrEmpty())
			{
				if (inAPara)
				{
					foreach (OpenXmlElement elem in ((DocFragment)senseNumberSpan).DocBody.Elements())
					{
						newPara.AppendChild(senseData.CloneElement(senseNumberSpan, elem));
					}
				}
				else
				{
					senseData.Append(senseNumberSpan);
				}
			}

			if (inAPara)
			{
				SeparateIntoFirstLevelElements(senseData, newPara, senseContent as DocFragment, config);
			}
			else
			{
				senseData.Append(senseContent);
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

			var collData = new DocFragment();
			WP.Paragraph newPara = null;
			bool eachInAParagraph = false;
			if (config != null &&
				config.DictionaryNodeOptions is IParaOption &&
				((IParaOption)(config.DictionaryNodeOptions)).DisplayEachInAParagraph)
			{
				eachInAParagraph = true;
				newPara = new WP.Paragraph();
			}

			// Add Between text, if it is not going to be displayed in a paragraph
			// and it is not the first item in the collection.
			if (!first &&
				config != null &&
				!eachInAParagraph &&
				!string.IsNullOrEmpty(config.Between))
			{
				string styleDisplayName = GetUniqueDisplayName(config, content);
				var betweenRun = CreateBeforeAfterBetweenRun(config.Between, styleDisplayName);
				((DocFragment)collData).DocBody.Append(betweenRun);
			}

			if (newPara != null)
			{
				SeparateIntoFirstLevelElements(collData, newPara, content as DocFragment, config);
			}
			else
			{
				collData.Append(content);
			}

			return collData;
		}
		public IFragment AddProperty(ConfigurableDictionaryNode config, ReadOnlyPropertyTable propTable, string className, bool isBlockProperty, string content, string writingSystem)
		{
			var propFrag = new DocFragment();
			Run contentRun = null;
			string styleDisplayName = null;

			if (string.IsNullOrEmpty(content))
			{
				// In this case, we should not generate the run or any before/after text for it.
				return propFrag;
			}

			// Create a run with the correct style.
			var writer = CreateWriter(propFrag);
			((WordFragmentWriter)writer).AddRun(Cache, config, propTable, writingSystem, true);

			// Add the content to the run.
			AddToRunContent(writer, content);
			var currentRun = ((WordFragmentWriter)writer).WordFragment.GetLastRun();

			// Get the run's styleDisplayName for use in before/after text runs.
			if (currentRun.RunProperties != null)
				styleDisplayName = currentRun.RunProperties.RunStyle?.Val;

			// Add Before text.
			if (!string.IsNullOrEmpty(config.Before))
			{
				var beforeRun = CreateBeforeAfterBetweenRun(config.Before, styleDisplayName);
				propFrag.DocBody.PrependChild(beforeRun);
			}

			// Add After text.
			if (!string.IsNullOrEmpty(config.After))
			{
				var afterRun = CreateBeforeAfterBetweenRun(config.After, styleDisplayName);
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

		public void StartEntry(IFragmentWriter writer, ConfigurableDictionaryNode node, string className, Guid entryGuid, int index, RecordClerk clerk)
		{
			// Each entry starts a new paragraph. The paragraph will end whenever a child needs its own paragraph or
			// when a data type exists that cannot be in a paragraph (Tables or nested paragraphs).
			// A new 'continuation' paragraph will be started for the entry if there is other data that still
			// needs to be added to the entry after the interruption.

			// Create the style for the entry.
			var style = WordStylesGenerator.GenerateParagraphStyleFromLcmStyleSheet(node.Style, WordStylesGenerator.DefaultStyle, _propertyTable, out BulletInfo? bulletInfo);
			style.StyleId = node.DisplayLabel;
			style.StyleName.Val = style.StyleId;
			AddParagraphBasedOnStyle(style, node, _propertyTable);
			string uniqueDisplayName = s_styleCollection.AddParagraphStyle(style, node.Style, style.StyleId, bulletInfo);

			// Create a new paragraph for the entry.
			DocFragment wordDoc = ((WordFragmentWriter)writer).WordFragment;
			WP.Paragraph entryPar = wordDoc.GetNewParagraph();
			WP.ParagraphProperties paragraphProps = new WP.ParagraphProperties(new ParagraphStyleId() {Val = uniqueDisplayName });
			entryPar.Append(paragraphProps);

			// Create the 'continuation' style for the entry. This style will be the same as the style for the entry with the only
			// differences being that it does not contain the first line indenting or bullet info (since it is a continuation of the same entry).
			var contStyle = WordStylesGenerator.GenerateContinuationStyle(style);
			s_styleCollection.AddParagraphStyle(contStyle, node.Style, contStyle.StyleId, null);
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
							wordWriter.WordFragment.AppendTable(frag, table);

							// Start a new paragraph with the next run to maintain the correct position of the table.
							wordWriter.ForceNewParagraph = true;
							break;

						case WP.Paragraph para:
							wordWriter.WordFragment.AppendParagraph(frag, para);

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
			string styleDisplayName = GetUniqueDisplayName(config, content);
			// Add Before text.
			if (!string.IsNullOrEmpty(config.Before))
			{
				var beforeRun = CreateBeforeAfterBetweenRun(config.Before, styleDisplayName);
				((WordFragmentWriter)writer).WordFragment.DocBody.Append(beforeRun);
			}

			if (!content.IsNullOrEmpty())
			{
				((WordFragmentWriter)writer).WordFragment.Append(content);
			}

			// Add After text.
			if (!string.IsNullOrEmpty(config.After))
			{
				var afterRun = CreateBeforeAfterBetweenRun(config.After, styleDisplayName);
				((WordFragmentWriter)writer).WordFragment.DocBody.Append(afterRun);
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
				if (s_styleCollection.TryGetStyle(numberStyleName, displayNameBase, out StyleElement existingStyle))
				{
					uniqueDisplayName = existingStyle.Style.StyleId;
				}
				// If the style is not in the collection, then add it.
				else
				{
					var wsString = WordStylesGenerator.GetWsString(senseNumberWs);

					// Get the style from the LcmStyleSheet.
					var cache = _propertyTable.GetValue<LcmCache>("cache");
					var wsId = cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr(senseNumberWs);
					Style style = WordStylesGenerator.GenerateCharacterStyleFromLcmStyleSheet(numberStyleName, wsId, _propertyTable);

					style.Append(new BasedOn() { Val = wsString });
					style.StyleId = displayNameBase;
					style.StyleName.Val = style.StyleId;
					bool wsIsRtl = IsWritingSystemRightToLeft(cache, wsId);
					uniqueDisplayName = s_styleCollection.AddCharacterStyle(style, numberStyleName, style.StyleId, wsId, wsIsRtl);
				}
			}

			DocFragment senseNum = new DocFragment();

			// Add characters before the number.
			if (!string.IsNullOrEmpty(beforeNumber))
			{
				var beforeRun = CreateBeforeAfterBetweenRun(beforeNumber, uniqueDisplayName);
				senseNum.DocBody.AppendChild(beforeRun);
			}

			// Add the number.
			if (!string.IsNullOrEmpty(formattedSenseNumber))
			{
				var run = CreateRun(formattedSenseNumber, uniqueDisplayName);
				senseNum.DocBody.AppendChild(run);
			}

			// Add characters after the number.
			if (!string.IsNullOrEmpty(afterNumber))
			{
				var afterRun = CreateBeforeAfterBetweenRun(afterNumber, uniqueDisplayName);
				senseNum.DocBody.AppendChild(afterRun);
			}

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

		public void BetweenCrossReferenceType(IFragment content, ConfigurableDictionaryNode node, bool firstItem)
		{
			// Add Between text if it is not the first item in the collection.
			if (!firstItem && !string.IsNullOrEmpty(node.Between))
			{
				string styleDisplayName = GetUniqueDisplayName(node, content);
				var betweenRun = CreateBeforeAfterBetweenRun(node.Between, styleDisplayName);
				((DocFragment)content).DocBody.PrependChild(betweenRun);
			}
		}

		public IFragment WriteProcessedSenses(ConfigurableDictionaryNode config, bool isBlock, IFragment senseContent, string className, IFragment sharedGramInfo)
		{
			var senseOptions = config?.DictionaryNodeOptions as DictionaryNodeSenseOptions;
			bool eachInAParagraph = senseOptions?.DisplayEachSenseInAParagraph ?? false;
			string styleDisplayName = GetUniqueDisplayName(config, sharedGramInfo);

			// Add Before text for the senses if they were not displayed in separate paragraphs.
			if (!eachInAParagraph && !string.IsNullOrEmpty(config.Before))
			{
				var beforeRun = CreateBeforeAfterBetweenRun(config.Before, styleDisplayName);
				((DocFragment)sharedGramInfo).DocBody.PrependChild(beforeRun);
			}

			AddBulletAndNumberingData(senseContent, config, eachInAParagraph);
			sharedGramInfo.Append(senseContent);

			// Add After text for the senses if they were not displayed in separate paragraphs.
			if (!eachInAParagraph && !string.IsNullOrEmpty(config.After))
			{
				var afterRun = CreateBeforeAfterBetweenRun(config.After, styleDisplayName);
				((DocFragment)sharedGramInfo).DocBody.Append(afterRun);
			}

			return sharedGramInfo;
		}
		public IFragment AddAudioWsContent(string wsId, Guid linkTarget, IFragment fileContent)
		{
			// We are not planning to support audio and video content for Word Export.
			return new DocFragment();
		}
		public IFragment GenerateErrorContent(StringBuilder badStrBuilder)
		{
			return new DocFragment($"Error generating content for string: '{badStrBuilder}'");
		}
		public IFragment GenerateVideoLinkContent(ConfigurableDictionaryNode config, string className, string mediaId, string srcAttribute,
			string caption)
		{
			// We are not planning to support audio and video content for Word Export.
			return new DocFragment();
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

			// Generate Character Styles
			//

			var beforeAfterBetweenStyle = WordStylesGenerator.GenerateBeforeAfterBetweenCharacterStyle(propertyTable, out int wsId);
			if (beforeAfterBetweenStyle != null)
			{
				bool wsIsRtl = IsWritingSystemRightToLeft(cache, wsId);
				s_styleCollection.AddCharacterStyle(beforeAfterBetweenStyle,
					WordStylesGenerator.BeforeAfterBetweenStyleName, beforeAfterBetweenStyle.StyleId, wsId, wsIsRtl);
			}

			List<StyleElement> writingSystemStyles = WordStylesGenerator.GenerateWritingSystemsCharacterStyles(propertyTable);
			if (writingSystemStyles != null)
			{
				foreach (StyleElement elem in writingSystemStyles)
				{
					s_styleCollection.AddCharacterStyle(elem.Style, elem.Style.StyleId, elem.Style.StyleId,
						elem.WritingSystemId, elem.WritingSystemIsRtl);
				}
			}

			// Generate Paragraph styles.
			// Note: the order of generation is important since we want based on names to use the display names, not the style names.
			//
			BulletInfo? bulletInfo = null;
			var normStyle = WordStylesGenerator.GenerateNormalParagraphStyle(propertyTable, out bulletInfo);
			if (normStyle != null)
			{
				s_styleCollection.AddParagraphStyle(normStyle, WordStylesGenerator.NormalParagraphStyleName, normStyle.StyleId, bulletInfo);
			}

			var pageHeaderStyle = WordStylesGenerator.GeneratePageHeaderStyle(normStyle);
			// Intentionally re-using the bulletInfo from Normal.
			s_styleCollection.AddParagraphStyle(pageHeaderStyle, WordStylesGenerator.PageHeaderStyleName, pageHeaderStyle.StyleId, bulletInfo);

			var mainStyle = WordStylesGenerator.GenerateMainEntryParagraphStyle(propertyTable, model, out ConfigurableDictionaryNode node, out bulletInfo);
			if (mainStyle != null)
			{
				AddParagraphBasedOnStyle(mainStyle, node, propertyTable);
				s_styleCollection.AddParagraphStyle(mainStyle, node.Style, mainStyle.StyleId, bulletInfo);
			}

			var headStyle = WordStylesGenerator.GenerateLetterHeaderParagraphStyle(propertyTable, out bulletInfo);
			if (headStyle != null)
			{
				AddParagraphBasedOnStyle(headStyle, null, _propertyTable);
				s_styleCollection.AddParagraphStyle(headStyle, WordStylesGenerator.LetterHeadingStyleName, headStyle.StyleId, bulletInfo);
			}

			// TODO: in openxml, will links be plaintext by default?
			//WordStylesGenerator.MakeLinksLookLikePlainText(_styleSheet);
			// TODO:  Generate style for audiows after we add audio to export
			//WordStylesGenerator.GenerateWordStyleForAudioWs(_styleSheet, cache);
		}

		/// <summary>
		/// Intended to add the basedOn styles for paragraph styles, not character styles.
		/// This method is recursive. It walks up the basedOn styles and adds them
		/// until we get to a style that is already in the collection.
		/// If the basedOn style is already in the collection then the style.BasedOn value will
		/// get updated to the unique display name.
		/// </summary>
		/// <param name="style">The style to add it's basedOn style. (It's BasedOn value might get modified.)</param>
		/// <param name="node">Can be null, but if it is then the only option for getting a basedOnStyle is from
		/// the style, not the parent node.</param>
		private void AddParagraphBasedOnStyle(Style style, ConfigurableDictionaryNode node, ReadOnlyPropertyTable propertyTable)
		{
			Debug.Assert(style.Type == StyleValues.Paragraph);

			// No based on styles for pictures.
			if (style.StyleId == WordStylesGenerator.PictureAndCaptionTextframeStyle)
				return;

			string basedOnStyleName = null;
			string basedOnDisplayName = null;
			ConfigurableDictionaryNode parentNode = null;
			if (style.BasedOn != null && !string.IsNullOrEmpty(style.BasedOn.Val))
			{
				basedOnStyleName = style.BasedOn.Val;
			}

			// If there is no basedOn style, or the basedOn style is "Normal" then use the
			// parent node's style for the basedOn style.
			if (string.IsNullOrEmpty(basedOnStyleName) ||
				basedOnStyleName == WordStylesGenerator.NormalParagraphStyleName)
			{
				if (node?.Parent != null && !string.IsNullOrEmpty(node.Parent.Style) &&
					(node.Parent.StyleType == ConfigurableDictionaryNode.StyleTypes.Paragraph))
				{
					parentNode = node.Parent;
					basedOnStyleName = node.Parent.Style;
					basedOnDisplayName = node.Parent.DisplayLabel;
				}
			}

			if (!string.IsNullOrEmpty(basedOnStyleName))
			{
				bool continuationStyle = style.StyleId.Value.EndsWith(WordStylesGenerator.EntryStyleContinue);
				// Currently this method does not work (and should not be used) for continuation styles. The problem is
				// that the basedOn name of the regular style has already been changed to the display name. We would
				// need a way to get the FLEX name from the display name.
				if (continuationStyle)
				{
					Debug.Assert(!continuationStyle, "Currently this method does not support continuation styles.");
					return;
				}

				lock (s_styleCollection)
				{
					// If the basedOn style already exists, then update the reference to the basedOn styles unique name.
					if (s_styleCollection.TryGetParagraphStyle(basedOnStyleName, out Style basedOnStyle))
					{
						style.BasedOn.Val = basedOnStyle.StyleId;
					}
					// Else if the basedOn style does NOT already exist, then create the basedOn style, if needed add
					// it's basedOn style, then add this basedOn style to the collection.
					else
					{
						basedOnStyle = WordStylesGenerator.GenerateParagraphStyleFromLcmStyleSheet(basedOnStyleName,
							WordStylesGenerator.DefaultStyle, propertyTable,out BulletInfo? bulletInfo);
						// Check if the style is based on itself.  This happens with the 'Normal' style and could possibly happen with others.
						bool basedOnIsDifferent = basedOnStyle.BasedOn?.Val != null && basedOnStyle.StyleId != basedOnStyle.BasedOn?.Val;

						if (!string.IsNullOrEmpty(basedOnDisplayName))
						{
							basedOnStyle.StyleId = basedOnDisplayName;
							basedOnStyle.StyleName.Val = basedOnStyle.StyleId;
							style.BasedOn.Val = basedOnStyle.StyleId;
						}

						if (basedOnIsDifferent)
						{
							// If the parentNode is not null then the basedOnStyle came from the parentNode.
							// If the parentNode is null then the basedOnStyle came from the style.BasedOn.Val and
							// we should pass null to AddParagraphBasedOnStyle since no node is associated with the basedOnStyle.
							AddParagraphBasedOnStyle(basedOnStyle, parentNode, propertyTable);
						}
						s_styleCollection.AddParagraphStyle(basedOnStyle, basedOnStyleName, basedOnStyle.StyleId, bulletInfo);
					}
				}
			}
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
				if (s_styleCollection.TryGetStyle(nodeStyleName, displayNameBase, out StyleElement styleElem))
				{
					retStyle = styleElem.Style;
					if (retStyle.Type != StyleValues.Character)
					{
						return null;
					}
				}
				else
				{
					retStyle = WordStylesGenerator.GenerateCharacterStyleFromLcmStyleSheet(nodeStyleName, WordStylesGenerator.DefaultStyle, propertyTable);
					if (retStyle == null || retStyle.Type != StyleValues.Character)
					{
						return null;
					}

					var cache = propertyTable.GetValue<LcmCache>("cache");
					bool wsIsRtl = IsWritingSystemRightToLeft(cache, WordStylesGenerator.DefaultStyle);
					s_styleCollection.AddCharacterStyle(retStyle, nodeStyleName, displayNameBase, WordStylesGenerator.DefaultStyle, wsIsRtl);
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

			Style style = WordStylesGenerator.GenerateParagraphStyleFromConfigurationNode(node, _propertyTable, out BulletInfo? bulletInfo);

			if (style == null)
				return className;

			if (style.Type == StyleValues.Paragraph)
			{
				lock (s_styleCollection)
				{
					if (!s_styleCollection.TryGetStyle(node.Style, style.StyleId, out StyleElement _))
					{
						AddParagraphBasedOnStyle(style, node, _propertyTable);
						string oldName = style.StyleId;
						string newName = s_styleCollection.AddParagraphStyle(style, node.Style, style.StyleId, bulletInfo);
						Debug.Assert(oldName == newName, "Not expecting the name for a paragraph style to ever change!");
					}
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

		// Add a DocumentSettingsPart to the document. Returns a reference to it.
		public static DocumentSettingsPart AddDocSettingsPartToPackage(WordprocessingDocument doc)
		{
			DocumentSettingsPart part;
			part = doc.MainDocumentPart.AddNewPart<DocumentSettingsPart>();
			return part;
		}

		// Add a NumberingDefinitionsPart to the document. Returns a reference to it.
		public static NumberingDefinitionsPart AddNumberingPartToPackage(WordprocessingDocument doc)
		{
			NumberingDefinitionsPart part;
			part = doc.MainDocumentPart.AddNewPart<NumberingDefinitionsPart>();
			Numbering numElement = new Numbering();
			numElement.Save(part);
			return part;
		}

		// Add the page HeaderParts to the document.
		public static void AddPageHeaderPartsToPackage(WordprocessingDocument doc, string guidewordStyle)
		{
			// Generate header for even pages.
			HeaderPart even = doc.MainDocumentPart.AddNewPart<HeaderPart>(WordStylesGenerator.PageHeaderIdEven);
			GenerateHeaderPartContent(even, true, guidewordStyle);

			// Generate header for odd pages.
			HeaderPart odd = doc.MainDocumentPart.AddNewPart<HeaderPart>(WordStylesGenerator.PageHeaderIdOdd);
			GenerateHeaderPartContent(odd, false, guidewordStyle);
		}

		/// <summary>
		/// Adds the page number and the first or last guideword to the HeaderPart.
		/// </summary>
		/// <param name="part">HeaderPart to modify.</param>
		/// <param name="even">True = generate content for even pages.
		///                    False = generate content for odd pages.</param>
		/// <param name="guidewordStyle">The style that will be used to find the first or last guideword on the page.</param>
		private static void GenerateHeaderPartContent(HeaderPart part, bool even, string guidewordStyle)
		{
			ParagraphStyleId paraStyleId = new ParagraphStyleId() { Val = WordStylesGenerator.PageHeaderStyleName };
			Paragraph para = new Paragraph(new ParagraphProperties(paraStyleId));

			if (even)
			{
				if (!string.IsNullOrEmpty(guidewordStyle))
				{
					// Add the first guideword on the page to the header.
					para.Append(new Run(new SimpleField() { Instruction = "STYLEREF \"" + guidewordStyle + "\" \\* MERGEFORMAT" }));
				}
				para.Append(new WP.Run(new WP.TabChar()));
				// Add the page number to the header.
				para.Append(new WP.Run(new SimpleField() { Instruction = "PAGE" }));
			}
			else
			{
				// Add the page number to the header.
				para.Append(new WP.Run(new SimpleField() { Instruction = "PAGE" }));
				para.Append(new WP.Run(new WP.TabChar()));
				if (!string.IsNullOrEmpty(guidewordStyle))
				{
					// Add the last guideword on the page to the header.
					para.Append(new WP.Run(new SimpleField() { Instruction = "STYLEREF \"" + guidewordStyle + "\" \\l \\* MERGEFORMAT" }));
				}
			}

			Header header = new Header(para);
			part.Header = header;
			part.Header.Save();
		}

		// Add an ImagePart to the document. Returns the part ID.
		public static string AddImagePartToPackage(WordprocessingDocument doc, string imagePath, ImagePartType imageType = ImagePartType.Jpeg)
		{
			MainDocumentPart mainPart = doc.MainDocumentPart;
			ImagePart imagePart = mainPart.AddImagePart(imageType);
			using (FileStream stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
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
				WP.RunProperties runProps = GenerateRunProperties(styleDisplayName);
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
		/// Creates a BeforeAfterBetween run using the text and style provided.
		/// </summary>
		/// <param name="text">Text for the run.</param>
		/// <param name="styleDisplayName">The style name to base on, or the complete style name.</param>
		/// <returns>The BeforeAfterBetween run.</returns>
		internal static WP.Run CreateBeforeAfterBetweenRun(string text, string styleDisplayName)
		{
			// Get the unique display name to use in the run.
			string uniqueDisplayName = null;
			// If there is no styleDisplayName then use the default BefAftBet display name.
			if (string.IsNullOrEmpty(styleDisplayName))
			{
				uniqueDisplayName = WordStylesGenerator.BeforeAfterBetweenDisplayName;
			}
			// If the styleDisplayName is already a BefAftBet style, then don't create a new style.
			else if (styleDisplayName.StartsWith(WordStylesGenerator.BeforeAfterBetweenDisplayName))
			{
				uniqueDisplayName = styleDisplayName;
			}
			// Create a new BefAftBet style similar to the default BefAftBet style but based on styleDisplayName.
			else
			{
				// If the styleDisplayName is a language tag, then no need to add the separator.
				string displayNameBaseCombined = WordStylesGenerator.BeforeAfterBetweenDisplayName;
				displayNameBaseCombined += styleDisplayName.StartsWith(WordStylesGenerator.LangTagPre) ?
					(styleDisplayName) : (WordStylesGenerator.StyleSeparator + styleDisplayName);

				// Get the BeforeAfterBetween style.
				StyleElement befAftElem = s_styleCollection.GetStyleElement(WordStylesGenerator.BeforeAfterBetweenDisplayName);

				Style basedOnStyle = WordStylesGenerator.GenerateBasedOnCharacterStyle(befAftElem.Style, styleDisplayName, displayNameBaseCombined);
				if (basedOnStyle != null)
				{
					uniqueDisplayName = s_styleCollection.AddCharacterStyle(basedOnStyle, WordStylesGenerator.BeforeAfterBetweenStyleName,
						basedOnStyle.StyleId, befAftElem.WritingSystemId, befAftElem.WritingSystemIsRtl);
				}
			}

			if (text.Contains("\\A") || text.Contains("\\0A") || text.Contains("\\a") || text.Contains("\\0a"))
			{
				var run = new WP.Run()
				{
					RunProperties = GenerateRunProperties(uniqueDisplayName)
				};
				// If the before after between text has line break characters return a composite run including the line breaks
				// Use Regex.Matches to capture both the content and the delimiters
				var matches = Regex.Matches(text, @"(\\A|\\0A|\\a|\\0a)|[^\\]*(?:(?=\\A|\\0A|\\a|\\0a)|$)");
				foreach (Match match in matches)
				{
					if (match.Groups[1].Success)
						run.Append(new WP.Break() { Type = BreakValues.TextWrapping });
					else
						run.Append(new WP.Text(match.Value));
				}
				return run;
			}

			return CreateRun(text, uniqueDisplayName);
		}

		/// <summary>
		/// Worker method for AddRunStyle(), not intended to be called from other places. If it is
		/// then the the pre-checks on 'style' should be added to this method.
		/// </summary>
		private void AddRunStyle_Worker(WP.Run run, string nodeStyleName, string displayNameBase)
		{
			// Use the writing system that is already used in the run.
			int wsId = WordStylesGenerator.DefaultStyle;
			bool wsIsRtl = false;
			var styleElem = GetStyleElementFromRun(run);
			if (styleElem != null)
			{
				wsId = styleElem.WritingSystemId;
				wsIsRtl = styleElem.WritingSystemIsRtl;
			}
			else
			{
				var cache = _propertyTable.GetValue<LcmCache>("cache");
				wsIsRtl = IsWritingSystemRightToLeft(cache, wsId);
			}

			Style rootStyle = WordStylesGenerator.GenerateWordStyleFromLcmStyleSheet(nodeStyleName, wsId, _propertyTable, out BulletInfo? _);
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
						// If the currentRun has one of the default global character styles then return. We do not
						// want to create a new style based on these.
						if (currentRunStyle.StartsWith(WordStylesGenerator.BeforeAfterBetweenDisplayName) ||
							currentRunStyle == WordStylesGenerator.SenseNumberDisplayName ||
							currentRunStyle == WordStylesGenerator.WritingSystemDisplayName)
						{
							return;
						}

						// If the current style is a language tag, then no need to add the separator.
						string displayNameBaseCombined = currentRunStyle.StartsWith(WordStylesGenerator.LangTagPre) ?
							(displayNameBase + currentRunStyle) : (displayNameBase + WordStylesGenerator.StyleSeparator + currentRunStyle);

						// The calls to TryGetStyle() and AddStyle() need to be in the same lock.
						lock (s_styleCollection)
						{
							if (s_styleCollection.TryGetStyle(nodeStyleName, displayNameBaseCombined, out StyleElement existingStyle))
							{
								ResetRunProperties(run, existingStyle.Style.StyleId);
							}
							else
							{
								// Don't create a new style if the current style already has the same root.
								int separatorIndex = currentRunStyle.IndexOf(WordStylesGenerator.StyleSeparator);
								separatorIndex = separatorIndex != -1 ? separatorIndex : currentRunStyle.IndexOf(WordStylesGenerator.LangTagPre);
								bool hasSameRoot = separatorIndex == -1 ? currentRunStyle.Equals(displayNameBase) :
									currentRunStyle.Substring(0, separatorIndex).Equals(displayNameBase);
								if (hasSameRoot)
								{
									return;
								}

								Style basedOnStyle = WordStylesGenerator.GenerateBasedOnCharacterStyle(rootStyle, currentRunStyle, displayNameBaseCombined);
								if (basedOnStyle != null)
								{
									string uniqueDisplayName = s_styleCollection.AddCharacterStyle(basedOnStyle, nodeStyleName, basedOnStyle.StyleId, wsId, wsIsRtl);
									ResetRunProperties(run, uniqueDisplayName);
								}
							}
						}
					}
					else
					{
						string uniqueDisplayName = s_styleCollection.AddCharacterStyle(rootStyle, nodeStyleName, displayNameBase, wsId, wsIsRtl);
						ResetRunProperties(run, uniqueDisplayName);
					}
				}
				else
				{
					string uniqueDisplayName = s_styleCollection.AddCharacterStyle(rootStyle, nodeStyleName, displayNameBase, wsId, wsIsRtl);
					ResetRunProperties(run, uniqueDisplayName);
				}
			}
			else
			{
				string uniqueDisplayName = s_styleCollection.AddCharacterStyle(rootStyle, nodeStyleName, displayNameBase, wsId, wsIsRtl);
				WP.RunProperties runProps = GenerateRunProperties(uniqueDisplayName);
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

		/// <summary>
		/// Word does not support certain element types being nested inside Paragraphs (Paragraphs & Tables).
		/// If we encounter one of these then end the paragraph and add the un-nestable type at the
		/// same level. If we later encounter nestable types then a continuation paragraph will be created.
		/// </summary>
		/// <param name="copyToFrag">The fragment where the new elements will be added.</param>
		/// <param name="firstParagraph">The first paragraph that will be added to 'copyToFrag'. Content from contentToAdd will be added
		/// to this paragraph until a un-nestable type is encountered.</param>
		/// <param name="contentToAdd">The content to add either to the paragraph or at the same level as the paragraph.</param>
		public void SeparateIntoFirstLevelElements(DocFragment copyToFrag, WP.Paragraph firstParagraph, DocFragment contentToAdd, ConfigurableDictionaryNode node)
		{
			bool continuationParagraph = false;
			var workingParagraph = firstParagraph;
			var elements = ((DocFragment)contentToAdd).DocBody.Elements();
			foreach (OpenXmlElement elem in elements)
			{
				Boolean containsDrawing = elem.Descendants<Drawing>().Any();
				// Un-nestable type (Paragraph or Table), or if a run contains a drawing, then leave it
				// as a first level element. Runs containing drawings will later, in AddEntryData(), get
				// put in their own paragraph.
				if (elem is WP.Paragraph || elem is WP.Table || (elem is WP.Run && containsDrawing))
				{
					// End the current working paragraph and add it to the list.
					if (EndParagraph(workingParagraph, node, continuationParagraph))
					{
						copyToFrag.DocBody.AppendChild(workingParagraph);
					}

					// Add the un-nestable element.
					copyToFrag.DocBody.AppendChild(copyToFrag.CloneElement(contentToAdd, elem));

					// Start a new working paragraph.
					continuationParagraph = true;
					workingParagraph = new WP.Paragraph();
				}
				else
				{
					workingParagraph.AppendChild(copyToFrag.CloneElement(contentToAdd, elem));
				}
			}

			// If the working paragraph contains content then add it's style and add
			// it to the return list.
			if (EndParagraph(workingParagraph, node, continuationParagraph))
			{
				copyToFrag.DocBody.AppendChild(workingParagraph);
			}
		}

		/// <summary>
		/// Adds the style needed for the paragraph and adds the reference to the style.
		/// </summary>
		/// <param name="continuationParagraph">True if this is a continuation paragraph.</param>
		/// <returns>true if the paragraph contains content, false if it does not.</returns>
		private bool EndParagraph(WP.Paragraph paragraph, ConfigurableDictionaryNode node, bool continuationParagraph)
		{
			if (paragraph != null && paragraph.HasChildren)
			{
				// Add the style.
				if (!string.IsNullOrEmpty(node.Style))
				{
					// The calls to TryGetStyle() and AddStyle() need to be in the same lock.
					lock(s_styleCollection)
					{
						BulletInfo? bulletInfo = null;
						string uniqueDisplayName = null;

						// Try to get the continuation style.
						if (continuationParagraph)
						{
							if (s_styleCollection.TryGetStyle(node.Style, node.DisplayLabel + WordStylesGenerator.EntryStyleContinue,
									out StyleElement contStyleElem))
							{
								bulletInfo = contStyleElem.BulletInfo;
								uniqueDisplayName = contStyleElem.Style.StyleId;
							}
						}

						if (string.IsNullOrEmpty(uniqueDisplayName))
						{
							// Try to get the regular style.
							Style style = null;
							if (s_styleCollection.TryGetStyle(node.Style, node.DisplayLabel, out StyleElement styleElem))
							{
								style = styleElem.Style;
								bulletInfo = styleElem.BulletInfo;
								uniqueDisplayName = style.StyleId;
							}
							// Add the regular style.
							else
							{
								style = WordStylesGenerator.GenerateParagraphStyleFromLcmStyleSheet(node.Style, WordStylesGenerator.DefaultStyle, _propertyTable, out bulletInfo);
								style.StyleId = node.DisplayLabel;
								style.StyleName.Val = style.StyleId;
								AddParagraphBasedOnStyle(style, node, _propertyTable);
								uniqueDisplayName = s_styleCollection.AddParagraphStyle(style, node.Style, style.StyleId, bulletInfo);
							}

							// Add the continuation style.
							if (continuationParagraph)
							{
								var contStyle = WordStylesGenerator.GenerateContinuationStyle(style);
								uniqueDisplayName = s_styleCollection.AddParagraphStyle(contStyle, node.Style, contStyle.StyleId, null);
							}
						}
						WP.ParagraphProperties paragraphProps =
							new WP.ParagraphProperties(new ParagraphStyleId() { Val = uniqueDisplayName });
						paragraph.PrependChild(paragraphProps);
					}
				}
				return true;
			}
			return false;
		}

		/// <summary>
		/// Adds the bullet and numbering data to a list of items.
		/// </summary>
		/// <param name="elementContent">The fragment containing the list of items.</param>
		/// <param name="eachInAParagraph">true: The list items are in paragraphs, so add the bullet or numbering.
		///                                false: The list items are not in paragraphs, don't add bullet or numbering.</param>
		private void AddBulletAndNumberingData(IFragment elementContent, ConfigurableDictionaryNode node, bool eachInAParagraph)
		{
			if (node.StyleType == ConfigurableDictionaryNode.StyleTypes.Paragraph &&
				!string.IsNullOrEmpty(node.Style) &&
				eachInAParagraph)
			{
				// Get the StyleElement.
				if (s_styleCollection.TryGetStyle(node.Style, node.DisplayLabel, out StyleElement styleElem))
				{
					// This style uses bullet or numbering.
					if (styleElem.BulletInfo.HasValue)
					{
						var bulletInfo = styleElem.BulletInfo.Value;
						var numScheme = bulletInfo.m_numberScheme;
						int? numberingFirstNumUniqueId = null;

						// We are potentially adding data to the StyleElement so it needs to be in a lock.
						lock (s_styleCollection)
						{
							// If the StyleElement does not already have the unique id then generate one.
							// Note: This number can be the same for all list items on all the lists associated with
							// this StyleElement with one exception; for numbered lists, the first list item on each
							// list needs it's own unique id.
							if (!styleElem.BulletAndNumberingUniqueId.HasValue)
							{
								styleElem.BulletAndNumberingUniqueId = s_styleCollection.GetNewBulletAndNumberingUniqueId;
							}

							// Only generate this number if it is a numbered list.
							// Note: Each list will need a uniqueId to cause the numbering to re-start at the beginning
							//       of each list.
							if (string.IsNullOrEmpty(bulletInfo.m_bulletCustom) &&
								string.IsNullOrEmpty(PreDefinedBullet(numScheme)) &&
								WordNumberingFormat(numScheme).HasValue)
							{
								numberingFirstNumUniqueId = s_styleCollection.GetNewBulletAndNumberingUniqueId;
								styleElem.NumberingFirstNumUniqueIds.Add(numberingFirstNumUniqueId.Value);
							}
						}

						// Iterate through the paragraphs and add the uniqueId to the ParagraphProperties.
						bool firstParagraph = true;
						foreach (OpenXmlElement elem in ((DocFragment)elementContent).DocBody.Elements())
						{
							if (elem is Paragraph)
							{
								var paraProps = elem.Elements<ParagraphProperties>().FirstOrDefault();
								if (paraProps != null)
								{
									// Only add the uniqueId to paragraphs with the correct style.  There could
									// be paragraphs with different styles.
									var paraStyle = paraProps.Elements<ParagraphStyleId>().FirstOrDefault();
									if (paraStyle != null && paraStyle.Val == node.DisplayLabel)
									{
										int uniqueId = styleElem.BulletAndNumberingUniqueId.Value;

										// The first paragraph for a numbered list needs to use a different uniqueId.
										if (firstParagraph && numberingFirstNumUniqueId.HasValue)
										{
											uniqueId = numberingFirstNumUniqueId.Value;
										}

										paraProps.Append(new NumberingProperties(
											new NumberingLevelReference() { Val = 0 },
											new NumberingId() { Val = uniqueId }));
										firstParagraph = false;
									}
								}
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Generate the bullet or numbering data and add it to the Word doc.
		/// </summary>
		/// <param name="styleElement">Contains the bullet and numbering data.</param>
		/// <param name="numberingPart">Part of the Word doc where bullet and numbering data is stored.</param>
		internal static void GenerateBulletAndNumberingData(StyleElement styleElement, NumberingDefinitionsPart numberingPart)
		{
			if (!styleElement.BulletInfo.HasValue)
			{
				return;
			}

			// Not expecting this to be null if BulletInfo is not null. If we hit this assert then
			// most likely there is another place where we need to call AddBulletAndNumberingData().
			Debug.Assert(styleElement.BulletAndNumberingUniqueId.HasValue);

			var bulletInfo = styleElement.BulletInfo.Value;
			var bulletUniqueId = styleElement.BulletAndNumberingUniqueId.Value;
			var numScheme = bulletInfo.m_numberScheme;
			Level abstractLevel = null;

			// Generate custom bullet data.
			if (!string.IsNullOrEmpty(bulletInfo.m_bulletCustom))
			{
				abstractLevel = new Level(new NumberingFormat() { Val = NumberFormatValues.Bullet },
					new LevelText() { Val = bulletInfo.m_bulletCustom })
					{ LevelIndex = 0 };
			}
			// Generate selected bullet data.
			else if (!string.IsNullOrEmpty(PreDefinedBullet(numScheme)))
			{
				abstractLevel = new Level(new NumberingFormat() { Val = NumberFormatValues.Bullet },
					new LevelText() { Val = PreDefinedBullet(numScheme) })
					{ LevelIndex = 0 };
			}
			// Generate numbering data.
			else if (WordNumberingFormat(numScheme).HasValue)
			{
				string numberString = bulletInfo.m_textBefore + "%1" + bulletInfo.m_textAfter;
				abstractLevel = new Level(new NumberingFormat() { Val = WordNumberingFormat(numScheme).Value },
					new LevelText() { Val = numberString },
					new StartNumberingValue() { Val = bulletInfo.m_start })
					{ LevelIndex = 0 };
			}

			if (abstractLevel == null)
			{
				return;
			}

			// Add any font properties that were explicitly set.
			if (bulletInfo.FontInfo != null && bulletInfo.FontInfo.IsAnyExplicit)
			{
				WP.RunProperties runProps = WordStylesGenerator.GetExplicitFontProperties(bulletInfo.FontInfo);
				if (runProps.HasChildren)
				{
					abstractLevel.Append(runProps);
				}
			}

			// Add the new AbstractNum after the last AbstractNum.
			// Word cares about the order of AbstractNum elements and NumberingInstance elements.
			var abstractNum = new AbstractNum(abstractLevel) { AbstractNumberId = bulletUniqueId };
			var lastAbstractNum = numberingPart.Numbering.Elements<AbstractNum>().LastOrDefault();
			if (lastAbstractNum == null)
			{
				numberingPart.Numbering.Append(abstractNum);
			}
			else
			{
				numberingPart.Numbering.InsertAfter(abstractNum, lastAbstractNum);
			}

			// Add the new NumberingInstance after the last NumberingInstance.
			// Word cares about the order of AbstractNum elements and NumberingInstance elements.
			var numberingInstance = new NumberingInstance() { NumberID = bulletUniqueId };
			var abstractNumId = new AbstractNumId() { Val = bulletUniqueId };
			numberingInstance.Append(abstractNumId);
			var lastNumberingInstance = numberingPart.Numbering.Elements<NumberingInstance>().LastOrDefault();
			if (lastNumberingInstance == null)
			{
				numberingPart.Numbering.Append(numberingInstance);
			}
			else
			{
				numberingPart.Numbering.InsertAfter(numberingInstance, lastNumberingInstance);
			}

			// If this is a numbered list then create the NumberingInstances for the first item in each list.
			if (styleElement.NumberingFirstNumUniqueIds.Any())
			{
				NumberingInstance insertAfter = numberingInstance;
				foreach (int firstParagraphUniqueId in styleElement.NumberingFirstNumUniqueIds)
				{
					NumberingInstance firstParagraphNumberingInstance = new NumberingInstance() { NumberID = firstParagraphUniqueId };
					AbstractNumId abstractNumId2 = new AbstractNumId() { Val = bulletUniqueId };
					LevelOverride levelOverride = new LevelOverride()
					{
						LevelIndex = 0,
						StartOverrideNumberingValue = new StartOverrideNumberingValue() { Val = bulletInfo.m_start }
					};
					firstParagraphNumberingInstance.Append(abstractNumId2);
					firstParagraphNumberingInstance.Append(levelOverride);
					numberingPart.Numbering.InsertAfter(firstParagraphNumberingInstance, insertAfter);
					insertAfter = firstParagraphNumberingInstance;
				}
			}
		}

		/// <summary>
		/// Get the pre-defined bullet character associated with the bullet scheme (not for custom bullets).
		/// </summary>
		/// <param name="scheme">The bullet scheme.</param>
		/// <returns>The bullet as a string, or null if the scheme is not for a pre-defined bullet.</returns>
		public static string PreDefinedBullet(VwBulNum scheme)
		{
			string bullet = null;
			switch (scheme)
			{
				case VwBulNum.kvbnBulletBase + 0: bullet = "\x00B7"; break;     // MIDDLE DOT
				case VwBulNum.kvbnBulletBase + 1: bullet = "\x2022"; break;     // BULLET (note: in a list item, consider using 'disc' somehow?)
				case VwBulNum.kvbnBulletBase + 2: bullet = "\x25CF"; break;     // BLACK CIRCLE
				case VwBulNum.kvbnBulletBase + 3: bullet = "\x274D"; break;     // SHADOWED WHITE CIRCLE
				case VwBulNum.kvbnBulletBase + 4: bullet = "\x25AA"; break;     // BLACK SMALL SQUARE (note: in a list item, consider using 'square' somehow?)
				case VwBulNum.kvbnBulletBase + 5: bullet = "\x25A0"; break;     // BLACK SQUARE
				case VwBulNum.kvbnBulletBase + 6: bullet = "\x25AB"; break;     // WHITE SMALL SQUARE
				case VwBulNum.kvbnBulletBase + 7: bullet = "\x25A1"; break;     // WHITE SQUARE
				case VwBulNum.kvbnBulletBase + 8: bullet = "\x2751"; break;     // LOWER RIGHT SHADOWED WHITE SQUARE
				case VwBulNum.kvbnBulletBase + 9: bullet = "\x2752"; break;     // UPPER RIGHT SHADOWED WHITE SQUARE
				case VwBulNum.kvbnBulletBase + 10: bullet = "\x2B27"; break;    // BLACK MEDIUM LOZENGE
				case VwBulNum.kvbnBulletBase + 11: bullet = "\x29EB"; break;    // BLACK LOZENGE
				case VwBulNum.kvbnBulletBase + 12: bullet = "\x25C6"; break;    // BLACK DIAMOND
				case VwBulNum.kvbnBulletBase + 13: bullet = "\x2756"; break;    // BLACK DIAMOND MINUS WHITE X
				case VwBulNum.kvbnBulletBase + 14: bullet = "\x2318"; break;    // PLACE OF INTEREST SIGN
				case VwBulNum.kvbnBulletBase + 15: bullet = "\x261E"; break;    // WHITE RIGHT POINTING INDEX
				case VwBulNum.kvbnBulletBase + 16: bullet = "\x271D"; break;    // LATIN CROSS
				case VwBulNum.kvbnBulletBase + 17: bullet = "\x271E"; break;    // SHADOWED WHITE LATIN CROSS
				case VwBulNum.kvbnBulletBase + 18: bullet = "\x2730"; break;    // SHADOWED WHITE STAR
				case VwBulNum.kvbnBulletBase + 19: bullet = "\x27A2"; break;    // THREE-D TOP-LIGHTED RIGHTWARDS ARROWHEAD
				case VwBulNum.kvbnBulletBase + 20: bullet = "\x27B2"; break;    // CIRCLED HEAVY WHITE RIGHTWARDS ARROW
				case VwBulNum.kvbnBulletBase + 21: bullet = "\x2794"; break;    // HEAVY WIDE-HEADED RIGHTWARDS ARROW
				case VwBulNum.kvbnBulletBase + 22: bullet = "\x2794"; break;    // HEAVY WIDE-HEADED RIGHTWARDS ARROW
				case VwBulNum.kvbnBulletBase + 23: bullet = "\x21E8"; break;    // RIGHTWARDS WHITE ARROW
				case VwBulNum.kvbnBulletBase + 24: bullet = "\x2713"; break;   // CHECK MARK
			}
			return bullet;
		}

		/// <summary>
		/// Return the Word number format.
		/// </summary>
		/// <param name="numberScheme">FLEX number format.</param>
		/// <returns>Word number format, or null if the numberScheme is not a valid numbering format.</returns>
		public static NumberFormatValues? WordNumberingFormat(VwBulNum numberScheme)
		{
			switch (numberScheme)
			{
				case VwBulNum.kvbnArabic:
					return NumberFormatValues.Decimal;
				case VwBulNum.kvbnRomanLower:
					return NumberFormatValues.LowerRoman;
				case VwBulNum.kvbnRomanUpper:
					return NumberFormatValues.UpperRoman;
				case VwBulNum.kvbnLetterLower:
					return NumberFormatValues.LowerLetter;
				case VwBulNum.kvbnLetterUpper:
					return NumberFormatValues.UpperLetter;
				case VwBulNum.kvbnArabic01:
					return NumberFormatValues.DecimalZero;
				default:
					return null;
			}
		}

		/// <summary>
		/// Deletes the existing run properties and creates new run properties; setting the
		/// style name and right to left flag.
		/// </summary>
		/// <param name="uniqueDisplayName">The new style name.</param>
		public void ResetRunProperties(Run run, string uniqueDisplayName)
		{
			if (run.RunProperties != null)
			{
				run.RemoveChild(run.RunProperties);
			}
			run.RunProperties = GenerateRunProperties(uniqueDisplayName);
		}

		/// <summary>
		/// Generate the run properties.  Sets the style name and right to left flag.
		/// </summary>
		/// <param name="uniqueDisplayName">The style name.</param>
		public static RunProperties GenerateRunProperties(string uniqueDisplayName)
		{
			if (string.IsNullOrEmpty(uniqueDisplayName))
			{
				return new RunProperties();
			}

			var runProp = new RunProperties(new RunStyle() { Val = uniqueDisplayName });
			if (IsBidi)
			{
				StyleElement styleElem = s_styleCollection.GetStyleElement(uniqueDisplayName);
				Debug.Assert(styleElem != null);
				if (styleElem.WritingSystemIsRtl)
				{
					runProp.RightToLeftText = new RightToLeftText();
				}
			}
			return runProp;
		}

		/// <summary>
		/// Iterate through the runs in the fragment looking for the style that
		/// most closely matches the node.DisplayLabel.
		/// </summary>
		private string GetUniqueDisplayName(ConfigurableDictionaryNode node, IFragment content)
		{
			Debug.Assert(!string.IsNullOrEmpty(node.DisplayLabel), "Not expecting a node without a DisplayLabel.");
			string endRunStyle = null;
			string beginRunStyle = null;
			var runs = ((DocFragment)content)?.DocBody.OfType<WP.Run>();
			if (runs != null)
			{
				foreach (var run in runs)
				{
					string runStyle = run.RunProperties?.RunStyle?.Val;
					if (runStyle != null)
					{
						// Remove the language tag and any appended numbers.
						string runName = runStyle;
						int langTagIndex = runName.IndexOf(WordStylesGenerator.LangTagPre);
						if (langTagIndex != -1)
						{
							runName = runName.Substring(0, langTagIndex);
						}
						runName = runName.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');

						// This is the common case: DisplayLabel followed by a possible integer and a language tag.
						// Definition (or Gloss)[lang='en'] or
						// Definition (or Gloss)2[lang='en']
						// If we find this, then there is no need to look further.  This is the style we want.
						if (runName == node.DisplayLabel)
						{
								return runStyle;
						}

						// The second preference is a style that ends with the DisplayLabel.
						// Strong : Example Sentence[lang='es'] or
						// Strong : Example Sentence3[lang='es']
						if (endRunStyle == null && runName.EndsWith(node.DisplayLabel))
						{
							// In this case don't use the complete runStyle.  We want the base style, not
							// a possible override applied to a specific run.
							// Return just "Example Sentence[lang='es']" or "Example Sentence3[lang='es']"
							endRunStyle = runStyle.Substring(runStyle.IndexOf(node.DisplayLabel));
						}

						// The third preference is a style that begins with the DisplayLabel.
						// Grammatical Info.2 : Category Info.[lang='en']
						if (beginRunStyle == null && endRunStyle == null && runStyle.StartsWith(node.DisplayLabel))
						{
							// In this case return the complete RunStyle.
							// Return "Grammatical Info.2 : Category Info.[lang='en']"
							beginRunStyle = runStyle;
						}
					}
				}
			}
			// Default to returning the DisplayLabel if we don't have anything else.
			// This is a common case for nodes that are collections.
			return endRunStyle ?? beginRunStyle ?? node.DisplayLabel;
		}

		/// <summary>
		/// Gets the unique display name out of a run.
		/// </summary>
		/// <returns>The name, or null if the run does not contain the information.</returns>
		public string GetUniqueDisplayName(Run run)
		{
			return run?.RunProperties?.RunStyle?.Val;
		}

		/// <summary>
		/// Get the StyleElement associated with a run.
		/// </summary>
		/// <returns>The StyleElement, or null if the run does not contain the information.</returns>
		public StyleElement GetStyleElementFromRun(Run run)
		{
			string uniqueDisplayName = GetUniqueDisplayName(run);
			if (uniqueDisplayName == null)  // Runs containing a 'Drawing' will not have RunProperties.
				return null;

			StyleElement elem = s_styleCollection.GetStyleElement(uniqueDisplayName);
			Debug.Assert(elem != null);  // I don't think we should ever not find a styleElement.

			return elem;
		}

		/// <summary>
		/// Check if a writing system is right to left.
		/// </summary>
		internal static bool IsWritingSystemRightToLeft(LcmCache cache, int wsId)
		{
			var lgWritingSystem = cache.ServiceLocator.WritingSystemManager.get_EngineOrNull(wsId);
			if (lgWritingSystem == null)
			{
				CoreWritingSystemDefinition defAnalWs = cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem;
				lgWritingSystem = cache.ServiceLocator.WritingSystemManager.get_EngineOrNull(defAnalWs.Handle);
			}
			return lgWritingSystem.RightToLeftScript;
		}

		/// <summary>
		/// Get the full style name for the first RunStyle that begins with the guideword style.
		/// </summary>
		/// <param name="type">Indicates if we are are exporting a Reversal or regular dictionary.</param>
		/// <returns>The full style name that begins with the guideword style.
		///          Null if none are found.</returns>
		public static string GetFirstGuidewordStyle(DocFragment frag, DictionaryConfigurationModel.ConfigType type)
		{
			string guidewordStyle = type == DictionaryConfigurationModel.ConfigType.Reversal ?
				WordStylesGenerator.ReversalFormDisplayName : WordStylesGenerator.HeadwordDisplayName;

			// Find the first run style with a value that begins with the guideword style.
			foreach (RunStyle runStyle in frag.DocBody.Descendants<RunStyle>())
			{
				if (runStyle.Val.Value.StartsWith(guidewordStyle))
				{
					return runStyle.Val.Value;
				}
			}
			return null;
		}

		/// <summary>
		/// Added to support tests.
		/// </summary>
		public static void ClearStyleCollection()
		{
			s_styleCollection.Clear();
		}
	}
}
