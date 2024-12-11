using DocumentFormat.OpenXml.Wordprocessing;
using ExCSS;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.Widgets;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainImpl;
using SIL.LCModel.DomainServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	public class WordStylesGenerator
	{

		// Styles functions
		/// <summary>
		/// id that triggers using the default selection on a character style instead of a writing system specific one
		/// </summary>
		internal const int DefaultStyle = -1;

		// Global and default character styles.
		internal const string BeforeAfterBetweenStyleName = "Dictionary-Context";
		internal const string BeforeAfterBetweenDisplayName = "Context";
		internal const string SenseNumberStyleName = "Dictionary-SenseNumber";
		internal const string SenseNumberDisplayName = "Sense Number";
		internal const string WritingSystemStyleName = "Writing System Abbreviation";
		internal const string WritingSystemDisplayName = "Writing System Abbreviation";
		internal const string HeadwordDisplayName = "Headword";
		internal const string ReversalFormDisplayName = "Reversal Form";
		internal const string StyleSeparator = " : ";
		internal const string LangTagPre = "[lang=\'";
		internal const string LangTagPost = "\']";

		// Globals and default paragraph styles.
		internal const string NormalParagraphStyleName = "Normal";
		internal const string PageHeaderStyleName = "Header";
		internal const string MainEntryParagraphDisplayName = "Main Entry";
		internal const string LetterHeadingStyleName = "Dictionary-LetterHeading";
		internal const string LetterHeadingDisplayName = "Letter Heading";
		internal const string PictureAndCaptionTextframeStyle = "Image-Textframe-Style";
		internal const string EntryStyleContinue = "-Continue";

		internal const string PageHeaderIdEven = "EvenPages";
		internal const string PageHeaderIdOdd = "OddPages";

		public static Style GenerateLetterHeaderParagraphStyle(ReadOnlyPropertyTable propertyTable, out BulletInfo? bulletInfo)
		{
			var style = GenerateParagraphStyleFromLcmStyleSheet(LetterHeadingStyleName, DefaultStyle, propertyTable, out bulletInfo);
			style.StyleId = LetterHeadingDisplayName;
			style.StyleName.Val = style.StyleId;
			return style;
		}

		public static Style GenerateBeforeAfterBetweenCharacterStyle(ReadOnlyPropertyTable propertyTable, out int wsId)
		{
			var cache = propertyTable.GetValue<LcmCache>("cache");
			wsId = cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle;
			var style = GenerateCharacterStyleFromLcmStyleSheet(BeforeAfterBetweenStyleName, wsId, propertyTable);
			style.StyleId = BeforeAfterBetweenDisplayName;
			style.StyleName.Val = style.StyleId;
			return style;
		}

		public static Style GenerateNormalParagraphStyle(ReadOnlyPropertyTable propertyTable, out BulletInfo? bulletInfo)
		{
			var style = GenerateParagraphStyleFromLcmStyleSheet(NormalParagraphStyleName, DefaultStyle, propertyTable, out bulletInfo);
			return style;
		}

		public static Style GenerateMainEntryParagraphStyle(ReadOnlyPropertyTable propertyTable, DictionaryConfigurationModel model,
			out ConfigurableDictionaryNode mainEntryNode, out BulletInfo? bulletInfo)
		{
			Style style = null;
			bulletInfo = null;

			// The user can change the style name that is associated with the Main Entry, so look up the node style name using the DisplayLabel.
			mainEntryNode = model?.Parts.Find(node => node.DisplayLabel == MainEntryParagraphDisplayName);
			if (mainEntryNode != null)
			{
				style = GenerateParagraphStyleFromLcmStyleSheet(mainEntryNode.Style, DefaultStyle, propertyTable, out bulletInfo);
				style.StyleId = MainEntryParagraphDisplayName;
				style.StyleName.Val = style.StyleId;
			}
			return style;
		}

		/// <summary>
		/// Generate the style that will be used for the header that goes on the top of
		/// every page.  The header style will be similar to the provided style, with the
		/// addition of the tab stop.
		/// </summary>
		/// <param name="style">The style to based the header style on.</param>
		/// <returns>The header style.</returns>
		internal static Style GeneratePageHeaderStyle(Style style)
		{
			Style pageHeaderStyle = (Style)style.CloneNode(true);
			pageHeaderStyle.StyleId = PageHeaderStyleName;
			pageHeaderStyle.StyleName.Val = pageHeaderStyle.StyleId;

			// Add the tab stop.
			var tabs = new Tabs();
			tabs.Append(new TabStop() { Val = TabStopValues.End, Position = (int)(1440 * 6.5/*inches*/) });
			pageHeaderStyle.StyleParagraphProperties.Append(tabs);
			return pageHeaderStyle;
		}

		/// <summary>
		/// Generates a Word Paragraph Style for the requested FieldWorks style.
		/// </summary>
		/// <param name="styleName">Name of the paragraph style.</param>
		/// <param name="wsId">writing system id</param>
		/// <param name="propertyTable">To retrieve styles</param>
		/// <param name="bulletInfo">Returns the bullet and numbering info associated with the style. Returns null
		///                          if there is none.</param>
		/// <returns>Returns the WordProcessing.Style item. Can return null.</returns>
		internal static Style GenerateParagraphStyleFromLcmStyleSheet(string styleName, int wsId,
			ReadOnlyPropertyTable propertyTable, out BulletInfo? bulletInfo)
		{
			var style = GenerateWordStyleFromLcmStyleSheet(styleName, wsId, propertyTable, out bulletInfo);
			Debug.Assert(style == null || style.Type == StyleValues.Paragraph);
			return style;
		}

		/// <summary>
		/// Generates a Word Character Style for the requested FieldWorks style.
		/// </summary>
		/// <param name="styleName">Name of the character style.</param>
		/// <param name="wsId">writing system id</param>
		/// <param name="propertyTable">To retrieve styles</param>
		/// <returns>Returns the WordProcessing.Style item. Can return null.</returns>
		internal static Style GenerateCharacterStyleFromLcmStyleSheet(string styleName, int wsId,
			ReadOnlyPropertyTable propertyTable)
		{
			var style = GenerateWordStyleFromLcmStyleSheet(styleName, wsId, propertyTable, out BulletInfo? _);
			Debug.Assert(style == null || style.Type == StyleValues.Character);
			return style;
		}

		/// <summary>
		/// Generates a Word Style for the requested FieldWorks style.
		/// </summary>
		/// <param name="styleName">Name of the character or paragraph style.</param>
		/// <param name="wsId">writing system id</param>
		/// <param name="propertyTable">To retrieve styles</param>
		/// <param name="bulletInfo">Returns the bullet and numbering info associated with the style. Returns null
		///                          if there is none. (For character styles always returns null.)</param>
		/// <returns>Returns the WordProcessing.Style item. Can return null.</returns>
		internal static Style GenerateWordStyleFromLcmStyleSheet(string styleName, int wsId,
			ReadOnlyPropertyTable propertyTable, out BulletInfo? bulletInfo)
		{
			bulletInfo = null;
			var styleSheet = FontHeightAdjuster.StyleSheetFromPropertyTable(propertyTable);
			if (styleSheet == null || !styleSheet.Styles.Contains(styleName))
			{
				return null;
			}

			var projectStyle = styleSheet.Styles[styleName];
			var exportStyleInfo = new ExportStyleInfo(projectStyle);
			var exportStyle = new Style();
			// StyleId is used for style linking in the xml.
			exportStyle.StyleId = styleName.Trim('.');
			// StyleName is the name a user will see for the given style in Word's style sheet.
			exportStyle.Append(new StyleName() {Val = exportStyle.StyleId});
			var parProps = new StyleParagraphProperties();
			var runProps = new StyleRunProperties();

			if (exportStyleInfo.BasedOnStyle?.Name != null)
				exportStyle.BasedOn = new BasedOn() { Val = exportStyleInfo.BasedOnStyle.Name };

			// Create paragraph and run styles as specified by exportStyleInfo.
			// Only if the style to export is a paragraph style should we create paragraph formatting options like indentation, alignment, border, etc.
			if (exportStyleInfo.IsParagraphStyle)
			{
				exportStyle.Type = StyleValues.Paragraph;
				var hangingIndent = 0.0f;

				if (exportStyleInfo.HasAlignment)
				{
					var alignmentStyle = exportStyleInfo.Alignment.AsWordStyle();
					if (alignmentStyle != null)
						// alignment is always a paragraph property
						parProps.Append(alignmentStyle);
				}

				// TODO:
				// The code below works to handle borders for the word export.
				// However, borders do not currently display in FLEx, and once a border has been added in FLEx,
				// deselecting the border does not actually remove it from the styles object in FLEx.
				// Until this is fixed, it is better not to display borders in the word export.
				/*if (exportStyleInfo.HasBorder)
				{
					// create borders to add to the paragraph properties
					ParagraphBorders border = new ParagraphBorders();

					// FieldWorks allows only solid line borders; in OpenXML solid line borders are denoted by BorderValues.Single
					// OpenXML uses eighths of a point for border sizing instead of the twentieths of a point it uses for most spacing values
					LeftBorder LeftBorder = new LeftBorder() { Val = BorderValues.Single, Size = (UInt32)MilliPtToEighthPt(exportStyleInfo.BorderLeading), Space = 1 };
					RightBorder RightBorder = new RightBorder() { Val = BorderValues.Single, Size = (UInt32)MilliPtToEighthPt(exportStyleInfo.BorderTrailing), Space = 1 };
					TopBorder TopBorder = new TopBorder() { Val = BorderValues.Single, Size = (UInt32)MilliPtToEighthPt(exportStyleInfo.BorderTop), Space = 1 }; ;
					BottomBorder BottomBorder = new BottomBorder() { Val = BorderValues.Single, Size = (UInt32)MilliPtToEighthPt(exportStyleInfo.BorderBottom), Space = 1 };

					if (exportStyleInfo.HasBorderColor)
					{
						// note: export style info contains an alpha value, but openxml does not allow an alpha value for border color.
						string openXmlColor = GetOpenXmlColor(exportStyleInfo.BorderColor.R, exportStyleInfo.BorderColor.G, exportStyleInfo.BorderColor.B);

						LeftBorder.Color = openXmlColor;
						RightBorder.Color = openXmlColor;
						TopBorder.Color = openXmlColor;
						BottomBorder.Color = openXmlColor;
					}
					border.Append(LeftBorder);
					border.Append(RightBorder);
					border.Append(TopBorder);
					border.Append(BottomBorder);
					parProps.Append(border);

				}*/

				if (exportStyleInfo.HasFirstLineIndent)
				{
					// Handles both first-line and hanging indent, hanging-indent will result in a negative text-indent value
					var firstLineIndentValue = MilliPtToTwentiPt(exportStyleInfo.FirstLineIndent);

					if (firstLineIndentValue < 0.0f)
					{
						hangingIndent = firstLineIndentValue;
					}
					parProps.Append(new Indentation() { FirstLine = firstLineIndentValue.ToString() });
				}

				if (exportStyleInfo.HasKeepWithNext)
				{
					// attempt to prevent page break between this paragraph and the next
					parProps.Append(new KeepNext());
				}

				if (exportStyleInfo.HasKeepTogether)
				{
					// attempt to keep all lines within this paragraph on the same page
					parProps.Append(new KeepLines());
				}

				// calculate leading indent.
				if (exportStyleInfo.HasLeadingIndent || hangingIndent < 0.0f)
				{
					var leadingIndent = CalculateMarginLeft(exportStyleInfo, hangingIndent);
					parProps.Append(new Indentation() { Left = leadingIndent.ToString() });
				}

				if (exportStyleInfo.HasLineSpacing)
				{
					//m_relative means single, 1.5 or double line spacing was chosen.
					if (exportStyleInfo.LineSpacing.m_relative)
					{
						// The relative value is stored internally multiplied by 10000.  (FieldWorks code generally hates floating point.)
						// Calculating relative lineHeight; (should be 1, 1.5, or 2 depending on spacing selected)
						var lineHeight = Math.Round(Math.Abs(exportStyleInfo.LineSpacing.m_lineHeight) / 10000.0F, 1);

						SpacingBetweenLines lineSpacing;

						// Calculate fontsize to use in linespacing calculation.
						double fontSize;
						if (!GetFontSize(projectStyle, wsId, out fontSize))
							// If no fontsize is specified, use 12 as the default.
							fontSize = 12;

						// OpenXML expects to see line spacing values in twentieths of a point. 20 * fontsize corresponds to single spacing given in 20ths of a point
						lineSpacing = new SpacingBetweenLines() { Line = ((int)Math.Round((20 * fontSize) * lineHeight)).ToString() };

						parProps.Append(lineSpacing);
					}
					else
					{
						// Note: In Flex a user can set 'at least' or 'exactly' for line heights. These are differentiated using negative and positive
						// values in LineSpacing.m_lineHeight -- negative value means at least line height, otherwise it's exactly line height
						var lineHeight = exportStyleInfo.LineSpacing.m_lineHeight;
						if (lineHeight < 0)
						{
							lineHeight = MilliPtToTwentiPt(Math.Abs(exportStyleInfo.LineSpacing.m_lineHeight));
							parProps.Append(new SpacingBetweenLines() { Line = lineHeight.ToString(), LineRule = LineSpacingRuleValues.AtLeast });
						}
						else
						{
							lineHeight = MilliPtToTwentiPt(exportStyleInfo.LineSpacing.m_lineHeight);
							parProps.Append(new SpacingBetweenLines() { Line = lineHeight.ToString(), LineRule = LineSpacingRuleValues.Exact });
						}
					}
					if (exportStyleInfo.HasSpaceAfter)
					{
						parProps.Append(new SpacingBetweenLines() { After = MilliPtToTwentiPt(exportStyleInfo.SpaceAfter).ToString() });
					}
					if (exportStyleInfo.HasSpaceBefore)
					{
						parProps.Append(new SpacingBetweenLines() { Before = MilliPtToTwentiPt(exportStyleInfo.SpaceBefore).ToString() });
					}
				}

				if (exportStyleInfo.HasTrailingIndent)
				{
					parProps.Append(new Indentation() { Right = MilliPtToTwentiPt(exportStyleInfo.TrailingIndent).ToString() });
				}

				// If text direction is right to left, add BiDi property to the paragraph.
				if (exportStyleInfo.DirectionIsRightToLeft == TriStateBool.triTrue)
				{
					parProps.Append(new BiDi());
				}

				// Add Bullet and Numbering.
				if (exportStyleInfo.NumberScheme != VwBulNum.kvbnNone)
				{
					bulletInfo = exportStyleInfo.BulletInfo;
				}

				exportStyle.Append(parProps);
			}
			// If the style to export isn't a paragraph style, set it to character style type
			else
			{
				exportStyle.Type = StyleValues.Character;
			}

			// Getting the character formatting info to add to the run properties
			runProps = AddFontInfoWordStyles(projectStyle, wsId, propertyTable.GetValue<LcmCache>("cache"));
			exportStyle.Append(runProps);
			return exportStyle;
		}

		/// <summary>
		/// Generates paragraph styles from a configuration node.
		/// </summary>
		public static Style GenerateParagraphStyleFromConfigurationNode(ConfigurableDictionaryNode configNode,
			ReadOnlyPropertyTable propertyTable, out BulletInfo? bulletInfo)
		{
			bulletInfo = null;
			switch (configNode.DictionaryNodeOptions)
			{
				// TODO: handle listAndPara case and character portion of pictureOptions
				// case IParaOption listAndParaOpts:

				case DictionaryNodePictureOptions pictureOptions:
					var cache = propertyTable.GetValue<LcmCache>("cache");
					return GenerateParagraphStyleFromPictureOptions(configNode, pictureOptions, cache, propertyTable);

				default:
					{
						// If the configuration node defines a paragraph style then add the style.
						if (!string.IsNullOrEmpty(configNode.Style) &&
							(configNode.StyleType == ConfigurableDictionaryNode.StyleTypes.Paragraph))
						{
							var style = GenerateParagraphStyleFromLcmStyleSheet(configNode.Style, DefaultStyle, propertyTable, out bulletInfo);
							style.StyleId = configNode.DisplayLabel;
							style.StyleName.Val = style.StyleId;
							return style;
						}
						return null;
					}
			}
		}

		/// <summary>
		/// Generate the character styles (for the writing systems) that will be the base of all other character styles.
		/// </summary>
		/// <param name="propertyTable"></param>
		/// <returns></returns>
		public static List<StyleElement> GenerateWritingSystemsCharacterStyles(ReadOnlyPropertyTable propertyTable)
		{
			var styleElements = new List<StyleElement>();
			var cache = propertyTable.GetValue<LcmCache>("cache");
			// Generate the styles for all the writing systems
			foreach (var aws in cache.ServiceLocator.WritingSystems.AllWritingSystems)
			{
				// Get the character style information from the "Normal" paragraph style.
				Style wsCharStyle = GetOnlyCharacterStyle(GenerateParagraphStyleFromLcmStyleSheet(NormalParagraphStyleName, aws.Handle, propertyTable, out BulletInfo? _));
				wsCharStyle.StyleId = GetWsString(aws.LanguageTag);
				wsCharStyle.StyleName = new StyleName() { Val = wsCharStyle.StyleId };
				var styleElem = new StyleElement(wsCharStyle.StyleId, wsCharStyle, null, aws.Handle, aws.RightToLeftScript);
				styleElements.Add(styleElem);
			}

			return styleElements;
		}

		private static Style GenerateParagraphStyleFromPictureOptions(ConfigurableDictionaryNode configNode, DictionaryNodePictureOptions pictureOptions,
			LcmCache cache, ReadOnlyPropertyTable propertyTable)
		{
			var frameStyle = new Style();

			// A textframe for holding an image/caption has to be a paragraph
			frameStyle.Type = StyleValues.Paragraph;

			// We use FLEX's max image width as the width for the textframe.
			// Note: 1 inch is equivalent to 72 points, and width is specified in twentieths of a point.
			// Thus, we calculate textframe width by multiplying max image width in inches by 72*30 = 1440
			var textFrameWidth = LcmWordGenerator.maxImageWidthInches * 1440;

			// We will leave a 4-pt border around the textframe--80 twentieths of a point.
			var textFrameBorder = "80";

			// A paragraph is turned into a textframe simply by adding a frameproperties object inside the paragraph properties.
			// Note that the argument "Y = textFrameBorder" is necessary for the following reason:
			// In Word 2019, in order for the image textframe to display below the entry it portrays,
			// a positive y-value offset must be specified that matches or exceeds the border of the textframe.
			// We also lock the image's anchor because this allows greater flexibility in positioning the image from within Word.
			// Without a locked anchor, if a user drags a textframe, Word will arbitrarily change the anchor and snap the textframe into a new location,
			// rather than allowing the user to drag the textframe to their desired location.
			var textFrameProps = new FrameProperties() { Width = textFrameWidth.ToString(), HeightType = HeightRuleValues.Auto, HorizontalSpace = textFrameBorder, VerticalSpace = textFrameBorder,
				Wrap = TextWrappingValues.NotBeside, VerticalPosition = VerticalAnchorValues.Text, HorizontalPosition = HorizontalAnchorValues.Text, XAlign = HorizontalAlignmentValues.Right,
				Y=textFrameBorder, AnchorLock = new DocumentFormat.OpenXml.OnOffValue(true) };
			var parProps = new ParagraphProperties();
			frameStyle.StyleId = PictureAndCaptionTextframeStyle;
			frameStyle.StyleName = new StyleName(){Val = PictureAndCaptionTextframeStyle};
			parProps.Append(textFrameProps);
			frameStyle.Append(parProps);
			return frameStyle;
		}

		private static Styles GenerateWordStylesFromListAndParaOptions(ConfigurableDictionaryNode configNode,
			IParaOption listAndParaOpts, ref string baseSelection, LcmCache cache, ReadOnlyPropertyTable propertyTable)
		{
			// TODO: Generate these styles when we implement custom numbering as well as before/after + separate paragraphs in styles
			return null;
		}

		/// <summary>
		/// Create a paragraph 'continuation' style based on a regular style. This is needed when a paragraph is split
		/// because part of the content cannot be nested in a paragraph (table, another paragraph). The
		/// continuation style is the same as the regular style except that it does not contain the first line indenting.
		/// </summary>
		/// <returns>Returns the continuation style.</returns>
		internal static Style GenerateContinuationStyle(Style style)
		{
			Style contStyle = (Style)style.CloneNode(true);
			WordStylesGenerator.RemoveFirstLineIndentation(contStyle);
			contStyle.StyleId = contStyle.StyleId + EntryStyleContinue;
			contStyle.StyleName.Val = contStyle.StyleId;

			if (contStyle.BasedOn != null && !string.IsNullOrEmpty(contStyle.BasedOn.Val) &&
				contStyle.BasedOn.Val != NormalParagraphStyleName)
			{
				contStyle.BasedOn.Val = contStyle.BasedOn.Val + EntryStyleContinue;
			}
			return contStyle;
		}

		/// <summary>
		/// Remove the first line indentation from the style.
		/// Continuation styles need this removed.
		/// </summary>
		/// <param name="style">The style that will be modified to remove the value.</param>
		private static void RemoveFirstLineIndentation(Style style)
		{
			// Get the paragraph properties.
			StyleParagraphProperties paraProps = style.OfType<StyleParagraphProperties>().FirstOrDefault();
			if (paraProps != null)
			{
				// Remove FirstLine from all the indentations. Typically it will only be in one.
				// Note: ToList() is necessary so we are not enumerating over the collection that we are removing from.
				foreach (var indentation in paraProps.OfType<Indentation>().ToList())
				{
					if (indentation.FirstLine != null)
					{
						// Remove the FirstLine value.
						indentation.FirstLine = null;

						// Remove the indentation if it doesn't contain anything.
						if (!indentation.HasChildren && !indentation.HasAttributes)
						{
							paraProps.RemoveChild(indentation);
						}
					}
				}
			}
		}

		/// <summary>
		/// Generates a new character style similar to the rootStyle, but being based on the provided style name.
		/// </summary>
		/// <param name="rootStyle">The style we want the new style to be similar to.</param>
		/// <param name="styleToBaseOn">The name of the style that the new style will be based on.</param>
		/// <param name="newStyleName">The name for the new style.</param>
		internal static Style GenerateBasedOnCharacterStyle(Style rootStyle, string styleToBaseOn, string newStyleName)
		{
			if (rootStyle == null || string.IsNullOrEmpty(styleToBaseOn) || string.IsNullOrEmpty(newStyleName))
			{
				return null;
			}

			Style retStyle = GetOnlyCharacterStyle(rootStyle);
			retStyle.Append(new BasedOn() { Val = styleToBaseOn });
			retStyle.StyleId = newStyleName;
			retStyle.StyleName = new StyleName() { Val = retStyle.StyleId };
			return retStyle;
		}

		/// <summary>
		/// Builds the word styles for font info properties using the writing system overrides
		/// </summary>
		private static StyleRunProperties AddFontInfoWordStyles(BaseStyleInfo projectStyle, int wsId, LcmCache cache)
		{
			var charDefaults = new StyleRunProperties();
			var wsFontInfo = projectStyle.FontInfoForWs(wsId);
			var defaultFontInfo = projectStyle.DefaultCharacterStyleInfo;

			// set fontName to the wsFontInfo publicly accessible InheritableStyleProp value if set, otherwise the
			// defaultFontInfo if set, or null.
			var fontName = wsFontInfo.m_fontName.ValueIsSet ? wsFontInfo.m_fontName.Value
				: defaultFontInfo.FontName.ValueIsSet ? defaultFontInfo.FontName.Value : null;

			// fontName still null means not set in Normal Style, then get default fonts from WritingSystems configuration.
			// Comparison, projectStyle.Name == "Normal", required to limit the font-family definition to the
			// empty span (ie span[lang="en"]{}. If not included, font-family will be added to many more spans.
			if (fontName == null && projectStyle.Name == NormalParagraphStyleName)
			{
				var lgWritingSystem = cache.ServiceLocator.WritingSystemManager.get_EngineOrNull(wsId);
				if (lgWritingSystem != null)
					fontName = lgWritingSystem.DefaultFontName;
				else
				{
					CoreWritingSystemDefinition defAnalWs = cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem;
					lgWritingSystem = cache.ServiceLocator.WritingSystemManager.get_EngineOrNull(defAnalWs.Handle);
					if (lgWritingSystem != null)
						fontName = lgWritingSystem.DefaultFontName;

				}
			}

			if (fontName != null)
			{
				var font = new RunFonts()
				{
					Ascii = fontName,
					HighAnsi = fontName,
					ComplexScript = fontName,
					EastAsia = fontName
				};
				charDefaults.Append(font);
			}

			// For the following additions, wsFontInfo is a publicly accessible InheritableStyleProp value if set (ie. m_fontSize, m_bold, etc.).
			// We check for explicit overrides. Otherwise the defaultFontInfo if set (ie. FontSize, Bold, etc), or null.

			// Check fontsize
			int fontSize;
			if (GetFontValue(wsFontInfo.m_fontSize, defaultFontInfo.FontSize, out fontSize) ||
			   projectStyle.Name == NormalParagraphStyleName)
			{
				// Always set the font size for the 'Normal' paragraph style.
				if (fontSize == 0)
				{
					fontSize = FontInfo.kDefaultFontSize * 1000;
				}

				// Fontsize is stored internally multiplied by 1000.  (FieldWorks code generally hates floating point.)
				// OpenXML expects fontsize given in halves of a point; thus we divide by 500.
				fontSize = fontSize / 500;
				var size = new FontSize() { Val = fontSize.ToString() };
				var sizeCS = new FontSizeComplexScript() { Val = fontSize.ToString() };
				charDefaults.Append(size);
				charDefaults.Append(sizeCS);
			}

			// Check for bold
			bool bold;
			GetFontValue(wsFontInfo.m_bold, defaultFontInfo.Bold, out bold);
			if (bold)
			{
				var boldFont = new Bold() { Val = true };
				var boldCS = new BoldComplexScript() { Val = true };
				charDefaults.Append(boldFont);
				charDefaults.Append(boldCS);
			}

			// Check for italic
			bool ital;
			GetFontValue(wsFontInfo.m_italic, defaultFontInfo.Italic, out ital);
			if (ital)
			{
				var italFont = new Italic() { Val = true };
				var italicCS = new ItalicComplexScript() { Val = true };
				charDefaults.Append(italFont);
				charDefaults.Append(italicCS);
			}

			// Check for font color
			System.Drawing.Color fontColor;
			if (GetFontValue(wsFontInfo.m_fontColor, defaultFontInfo.FontColor, out fontColor))
			{
				// note: open xml does not allow alpha
				string openXmlColor = GetOpenXmlColor(fontColor.R, fontColor.G, fontColor.B);
				var color = new Color() { Val = openXmlColor };
				charDefaults.Append(color);
			}

			// Check for background color
			System.Drawing.Color backColor;
			if (GetFontValue(wsFontInfo.m_backColor, defaultFontInfo.BackColor, out backColor))
			{
				// note: open xml does not allow alpha,
				// though a percentage shading could be implemented using shading pattern options.
				string openXmlColor = GetOpenXmlColor(backColor.R, backColor.G, backColor.B);
				var backShade = new Shading() { Fill = openXmlColor };
				charDefaults.Append(backShade);
			}

			FwSuperscriptVal fwSuperSub;
			if (GetFontValue(wsFontInfo.m_superSub, defaultFontInfo.SuperSub, out fwSuperSub))
			{
				VerticalTextAlignment oxmlSuperSub = new VerticalTextAlignment();
				switch (fwSuperSub)
				{
					case (FwSuperscriptVal.kssvSub):
						oxmlSuperSub.Val = VerticalPositionValues.Subscript;
						break;
					case (FwSuperscriptVal.kssvSuper):
						oxmlSuperSub.Val = VerticalPositionValues.Superscript;
						break;
					case (FwSuperscriptVal.kssvOff):
						oxmlSuperSub.Val = VerticalPositionValues.Baseline;
						break;
				}
				charDefaults.Append(oxmlSuperSub);
			}

			// Handling underline and strikethrough.
			FwUnderlineType fwUnderline;
			if (GetFontValue(wsFontInfo.m_underline, defaultFontInfo.Underline, out fwUnderline))
			{
				// In FieldWorks, strikethrough is a special type of underline,
				// but strikethrough and underline are represented by different objects in OpenXml
				if (fwUnderline != FwUnderlineType.kuntStrikethrough)
				{
					Underline oxmlUnderline = new Underline();
					switch (fwUnderline)
					{
						case (FwUnderlineType.kuntSingle):
							oxmlUnderline.Val = UnderlineValues.Single;
							break;
						case (FwUnderlineType.kuntDouble):
							oxmlUnderline.Val = UnderlineValues.Double;
							break;
						case (FwUnderlineType.kuntDotted):
							oxmlUnderline.Val = UnderlineValues.Dotted;
							break;
						case (FwUnderlineType.kuntDashed):
							oxmlUnderline.Val = UnderlineValues.Dash;
							break;
						case (FwUnderlineType.kuntNone):
							oxmlUnderline.Val = UnderlineValues.None;
							break;
					}

					// UnderlineColor
					System.Drawing.Color color;
					if (GetFontValue(wsFontInfo.m_underlineColor, defaultFontInfo.UnderlineColor, out color) &&
						oxmlUnderline.Val != UnderlineValues.None)
					{
						string openXmlColor = GetOpenXmlColor(color.R, color.G, color.B);
						oxmlUnderline.Color = openXmlColor;
					}

					charDefaults.Append(oxmlUnderline);
				}
				// Else the underline is actually a strikethrough.
				else
				{
					charDefaults.Append(new Strike());
				}
			}
			//TODO: handle remaining font features including from ws or default,

			return charDefaults;
		}

		/// <summary>
		/// Gets the font properties that were explicitly set.
		/// </summary>
		/// <returns>RunProperties containing all explicitly set font properties.</returns>
		public static RunProperties GetExplicitFontProperties(FontInfo fontInfo)
		{
			var runProps = new RunProperties();

			// FontName
			if (((InheritableStyleProp<string>)fontInfo.FontName).IsExplicit)
			{
				// Note: if desired, multiple fonts can be used for different text types in a single run
				// by separately specifying font names to use for ASCII, High ANSI, Complex Script, and East Asian content.
				var font = new RunFonts() { Ascii = fontInfo.FontName.Value };
				runProps.Append(font);
			}

			// FontSize
			if (((InheritableStyleProp<int>)fontInfo.FontSize).IsExplicit)
			{
				// Fontsize is stored internally multiplied by 1000.  (FieldWorks code generally hates floating point.)
				// OpenXML expects fontsize given in halves of a point; thus we divide by 500.
				int fontSize = fontInfo.FontSize.Value / 500;
				var size = new FontSize() { Val = fontSize.ToString() };
				runProps.Append(size);
			}

			// Bold
			if (((InheritableStyleProp<bool>)fontInfo.Bold).IsExplicit)
			{
				var bold = new Bold() { Val = fontInfo.Bold.Value };
				runProps.Append(bold);
			}

			// Italic
			if (((InheritableStyleProp<bool>)fontInfo.Italic).IsExplicit)
			{
				var ital = new Italic() { Val = fontInfo.Italic.Value };
				runProps.Append(ital);
			}

			// FontColor
			if (((InheritableStyleProp<System.Drawing.Color>)fontInfo.FontColor).IsExplicit)
			{
				System.Drawing.Color color = fontInfo.FontColor.Value;
				// note: open xml does not allow alpha
				string openXmlColor = GetOpenXmlColor(color.R, color.G, color.B);
				var fontColor = new Color() { Val = openXmlColor };
				runProps.Append(fontColor);
			}

			// BackColor
			if (((InheritableStyleProp<System.Drawing.Color>)fontInfo.BackColor).IsExplicit)
			{
				System.Drawing.Color color = fontInfo.BackColor.Value;
				// note: open xml does not allow alpha,
				// though a percentage shading could be implemented using shading pattern options.
				string openXmlColor = GetOpenXmlColor(color.R, color.G, color.B);
				var backShade = new Shading() { Fill = openXmlColor };
				runProps.Append(backShade);
			}

			// Superscript
			if (((InheritableStyleProp<FwSuperscriptVal>)fontInfo.SuperSub).IsExplicit)
			{
				FwSuperscriptVal fwSuperSub = fontInfo.SuperSub.Value;
				VerticalTextAlignment oxmlSuperSub = new VerticalTextAlignment();
				switch (fwSuperSub)
				{
					case (FwSuperscriptVal.kssvSub):
						oxmlSuperSub.Val = VerticalPositionValues.Subscript;
						break;
					case (FwSuperscriptVal.kssvSuper):
						oxmlSuperSub.Val = VerticalPositionValues.Superscript;
						break;
					case (FwSuperscriptVal.kssvOff):
						oxmlSuperSub.Val = VerticalPositionValues.Baseline;
						break;
				}
				runProps.Append(oxmlSuperSub);
			}

			// Underline, UnderlineColor, and Strikethrough.
			if (((InheritableStyleProp<FwUnderlineType>)fontInfo.Underline).IsExplicit)
			{
				FwUnderlineType fwUnderline = fontInfo.Underline.Value;

				// In FieldWorks, strikethrough is a special type of underline,
				// but strikethrough and underline are represented by different objects in OpenXml
				if (fwUnderline != FwUnderlineType.kuntStrikethrough)
				{
					Underline oxmlUnderline = new Underline();
					switch (fwUnderline)
					{
						case (FwUnderlineType.kuntSingle):
							oxmlUnderline.Val = UnderlineValues.Single;
							break;
						case (FwUnderlineType.kuntDouble):
							oxmlUnderline.Val = UnderlineValues.Double;
							break;
						case (FwUnderlineType.kuntDotted):
							oxmlUnderline.Val = UnderlineValues.Dotted;
							break;
						case (FwUnderlineType.kuntDashed):
							oxmlUnderline.Val = UnderlineValues.Dash;
							break;
						case (FwUnderlineType.kuntNone):
							oxmlUnderline.Val = UnderlineValues.None;
							break;
					}

					// UnderlineColor
					if (((InheritableStyleProp<System.Drawing.Color>)fontInfo.UnderlineColor).IsExplicit &&
						oxmlUnderline.Val != UnderlineValues.None)
					{
						System.Drawing.Color color = fontInfo.UnderlineColor.Value;
						string openXmlColor = GetOpenXmlColor(color.R, color.G, color.B);
						oxmlUnderline.Color = openXmlColor;
					}

					runProps.Append(oxmlUnderline);
				}
				// Strikethrough
				else
				{
					runProps.Append(new Strike());
				}
			}
			return runProps;
		}

		public static string GetWsString(string wsId)
		{
			return LangTagPre + wsId + LangTagPost;
		}

		/// <summary>
		/// This method will set fontValue to the font value from the writing system info falling back to the
		/// default info. It will return false if the value is not set in either info.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="wsFontInfo">writing system specific font info</param>
		/// <param name="defaultFontInfo">default font info</param>
		/// <param name="fontValue">the value retrieved from the given font infos</param>
		/// <returns>true if fontValue was defined in one of the info objects</returns>
		private static bool GetFontValue<T>(InheritableStyleProp<T> wsFontInfo, IStyleProp<T> defaultFontInfo,
			out T fontValue)
		{
			fontValue = default(T);
			if (wsFontInfo.ValueIsSet)
				fontValue = wsFontInfo.Value;
			else if (defaultFontInfo.ValueIsSet)
				fontValue = defaultFontInfo.Value;
			else
				return false;
			return true;
		}

		private static ConfigurableDictionaryNode AncestorWithParagraphStyle(ConfigurableDictionaryNode currentNode,
			LcmStyleSheet styleSheet)
		{
			var parentNode = currentNode;
			do
			{
				parentNode = parentNode.Parent;
				if (parentNode == null)
					return null;
			} while (!IsParagraphStyle(parentNode, styleSheet));

			return parentNode;
		}

		/// <summary>
		/// Gets the indentation information for a Table.
		/// </summary>
		/// <param name="tableAlignment">Returns the table alignment.</param>
		/// <returns>Returns the indentation value.</returns>
		internal static int GetTableIndentInfo(ReadOnlyPropertyTable propertyTable, ConfigurableDictionaryNode config, ref TableRowAlignmentValues tableAlignment)
		{
			var style = config.Parent?.Style;
			var styleSheet = FontHeightAdjuster.StyleSheetFromPropertyTable(propertyTable);
			if (style == null || styleSheet == null || !styleSheet.Styles.Contains(style))
			{
				return 0;
			}

			var projectStyle = styleSheet.Styles[style];
			var exportStyleInfo = new ExportStyleInfo(projectStyle);

			// Get the indentation value.
			int indentVal = 0;
			var hangingIndent = 0.0f;
			if (exportStyleInfo.HasFirstLineIndent)
			{
				var firstLineIndentValue = MilliPtToTwentiPt(exportStyleInfo.FirstLineIndent);
				if (firstLineIndentValue < 0.0f)
				{
					hangingIndent = firstLineIndentValue;
				}
			}
			if (exportStyleInfo.HasLeadingIndent || hangingIndent < 0.0f)
			{
				var leadingIndent = CalculateMarginLeft(exportStyleInfo, hangingIndent);
				indentVal = (int)leadingIndent;
			}

			// Get the alignment direction.
			tableAlignment = exportStyleInfo.DirectionIsRightToLeft == TriStateBool.triTrue ?
				TableRowAlignmentValues.Right : TableRowAlignmentValues.Left;

			return indentVal;
		}

		/// <summary>
		/// Calculate the left margin.
		/// Note that in Word Styles the left margin is not combined with its ancestor so
		/// no adjustment is necessary.
		/// </summary>
		private static float CalculateMarginLeft(ExportStyleInfo exportStyleInfo, float hangingIndent)
		{
			var leadingIndent = 0.0f;
			if (exportStyleInfo.HasLeadingIndent)
			{
				leadingIndent = MilliPtToTwentiPt(exportStyleInfo.LeadingIndent);
			}

			leadingIndent -= hangingIndent;
			return leadingIndent;
		}

		/// <summary>
		/// Returns a style containing only the run properties from the full style declaration
		/// </summary>
		internal static Style GetOnlyCharacterStyle(Style fullStyleDeclaration)
		{
			Style charStyle = new Style() { Type = StyleValues.Character };
			if (fullStyleDeclaration.StyleId != null)
				charStyle.StyleId = fullStyleDeclaration.StyleId;
			if (fullStyleDeclaration.StyleRunProperties != null)
				charStyle.Append(fullStyleDeclaration.StyleRunProperties.CloneNode(true));
			return charStyle;
		}

		/// <summary>
		/// Returns a style containing only the paragraph properties from the full style declaration
		/// </summary>
		internal static Style GetOnlyParagraphStyle(Style fullStyleDeclaration)
		{
			Style parStyle = new Style() { Type = StyleValues.Paragraph };
			if (fullStyleDeclaration.StyleId != null)
				parStyle.StyleId = fullStyleDeclaration.StyleId;
			if (fullStyleDeclaration.StyleParagraphProperties != null)
				parStyle.Append(fullStyleDeclaration.StyleParagraphProperties.CloneNode(true));
			return parStyle;
		}

		private static Styles AddRange(Styles styles, Styles moreStyles)
		{
			if (styles != null)
			{
				if (moreStyles != null)
				{
					foreach (Style style in moreStyles)
						styles.Append(style.CloneNode(true));
				}

				return styles;
			}

			// if we reach this point, moreStyles can only be null if style is also null,
			// in which case we do actually wish to return null
			return moreStyles;
		}

		private static Styles AddRange(Styles moreStyles, Style style)
		{
			if (style != null)
			{
				if (moreStyles == null)
				{
					moreStyles = new Styles();
				}

				moreStyles.Append(style.CloneNode(true));
			}

			// if we reach this point, moreStyles can only be null if style is also null,
			// in which case we do actually wish to return null
			return moreStyles;
		}

		private static Styles RemoveBeforeAfterSelectorRules(Styles styles)
		{
			Styles selectedStyles = new Styles();
			// TODO: once all styles are handled, shouldn't need this nullcheck anymore
			if (styles != null)
			{
				foreach (Style style in styles)
					if (!IsBeforeOrAfter(style))
						selectedStyles.Append(style.CloneNode(true));
				return selectedStyles;
			}

			return null;
		}

		public static Styles CheckRangeOfStylesForEmpties(Styles rules)
		{
			// TODO: once all styles are handled, shouldn't need this nullcheck anymore
			//if (rules == null)
			//	return null;
			Styles nonEmptyStyles = new Styles();
			foreach (Style style in rules.Descendants<Style>())
				if (!IsEmptyStyle(style))
					nonEmptyStyles.Append(style.CloneNode(true));
			if (nonEmptyStyles.Descendants<Style>().Any())
				return nonEmptyStyles;

			return null;
		}

		private static bool IsBeforeOrAfter(Style style)
		{
			// TODO
			return false;
		}

		private static bool IsEmptyStyle(Style style)
		{
			// If the style has even just an ID, it will have descendants.
			// To check if style is truly empty, need to check if any of its descendants are of the following types:
			// paragraphproperties, stylerunproperties, runproperties, or tableproperties (or numberingproperties, but these would be specified w/in paragraph properties)
			// TODO: it is still possible for a style that contains e.g. nothing but an empty RunProperty to return as non-empty. Is there a more robust way of checking this?
			if (style.Descendants<ParagraphProperties>().Any() ||
				style.Descendants<StyleRunProperties>().Any() ||
				style.Descendants<RunProperties>().Any() ||
				style.Descendants<TableProperties>().Any() ||
				style.Descendants<TableCellProperties>().Any())
				return false;

			return true;
		}

		private static bool IsParagraphStyle(ConfigurableDictionaryNode node, LcmStyleSheet styleSheet)
		{
			if (node.StyleType == ConfigurableDictionaryNode.StyleTypes.Character)
				return false;
			var style = node.Style;
			return !string.IsNullOrEmpty(style) && styleSheet.Styles.Contains(style) && styleSheet.Styles[style].IsParagraphStyle;
		}

		private static bool GetFontSize(BaseStyleInfo projectStyle, int wsId, out double fontSize)
		{
			fontSize = default(double);
			var wsFontInfo = projectStyle.FontInfoForWs(wsId);
			var defaultFontInfo = projectStyle.DefaultCharacterStyleInfo;
			int fwFontSize;
			bool result = GetFontValue(wsFontInfo.m_fontSize, defaultFontInfo.FontSize, out fwFontSize);
			// Fontsize is stored internally multiplied by 1000.  (FieldWorks code generally hates floating point.)
			// We divide by 1000 and return fontsize in points
			if (result)
				fontSize = fwFontSize / 1000.0;

			return result;
		}

		private static string GetOpenXmlColor(byte r, byte g, byte b)
		{
			// note: openxml does not allow an alpha value for border color.
			// openxml expects border color values given as 6-digit hex values with the '#' omitted.
			return $"{r:X2}{g:X2}{b:X2}";
		}

		/// <summary>
		/// In the FwStyles values were stored in millipoints to avoid expensive floating point calculations in c++ code.
		/// We need to convert these to twentieths of a point for use in openxml word styles.
		/// </summary>
		private static int MilliPtToTwentiPt(int millipoints)
		{
			return (int)Math.Round((float)millipoints / 50, 0);
		}

		/// <summary>
		/// In the FwStyles values were stored in millipoints to avoid expensive floating point calculations in c++ code.
		/// For borders in openxml word styles, we need to convert these to eighths of a point.
		/// </summary>
		private static int MilliPtToEighthPt(int millipoints)
		{
			return (int)Math.Round((float)millipoints / 125, 0);
		}
	}

	public static class WordStyleExtensions
	{
		/// <summary>
		/// Extension method to provide a word style conversion from a FwTextAlign enum value
		/// </summary>
		/// <param name="align"></param>
		/// <returns></returns>
		public static Justification AsWordStyle(this FwTextAlign align)
		{
			switch (align)
			{
				case (FwTextAlign.ktalJustify):
					return new Justification() { Val = JustificationValues.Both };
				case (FwTextAlign.ktalCenter):
					return new Justification() { Val = JustificationValues.Center };
				case (FwTextAlign.ktalLeading):
					return new Justification() { Val = JustificationValues.Start };
				case (FwTextAlign.ktalTrailing):
					return new Justification() { Val = JustificationValues.End };
				case (FwTextAlign.ktalLeft):
					return new Justification() { Val = JustificationValues.Left };
				case (FwTextAlign.ktalRight):
					return new Justification() { Val = JustificationValues.Right };
				default:
					// If justification is not specified, it should automatically be inherited.
					return null;
			}
		}
	}
}
