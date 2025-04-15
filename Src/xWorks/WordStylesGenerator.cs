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
		internal const string NormalCharDisplayName = "Normal Font";
		internal const string NormalCharNodePath = ".normalFont";
		internal const string BeforeAfterBetweenStyleName = "Dictionary-Context";
		internal const string SenseNumberStyleName = "Dictionary-SenseNumber";
		internal const string SenseNumberDisplayName = "Sense Number";
		internal const string WritingSystemStyleName = "Writing System Abbreviation";
		internal const string WritingSystemDisplayName = "Writing System Abbreviation";
		internal const string HeadwordDisplayName = "Headword";
		internal const string ReversalFormDisplayName = "Reversal Form";
		internal const string StyleSeparator = "-";
		internal const string LangTagPre = "[lang=";
		internal const string LangTagPost = "]";
		internal const string BeforeAfterBetween = "-Context";
		internal const string LinkedCharacterStyle = "-char";
		internal const string SubentriesHeadword = "Subheadword";

		// Globals and default paragraph styles.
		// Nodepaths declared here are common names to use for the global styles
		// and don't necessarily match the actual paths of each node.
		internal const string NormalParagraphStyleName = "Normal";
		internal const string NormalParagraphDisplayName = "Normal";
		internal const string NormalParagraphNodePath = ".normal";
		internal const string PageHeaderStyleName = "Header";
		internal const string PageHeaderDisplayName = "Header";
		internal const string PageHeaderNodePath = ".header";
		internal const string MainEntryParagraphDisplayName = "Main Entry";
		internal const string LetterHeadingStyleName = "Dictionary-LetterHeading";
		internal const string LetterHeadingDisplayName = "Letter Heading";
		internal const string LetterHeadingNodePath = ".letterHeading";
		internal const string PictureAndCaptionTextframeDisplayName = "Picture And Caption";
		internal const string PictureAndCaptionNodePath = ".pictures";
		internal const string PictureTextboxOuterDisplayName = "Pictureframe Textbox";
		internal const string EntryStyleContinue = "-Continue";

		internal const string PageHeaderIdEven = "EvenPages";
		internal const string PageHeaderIdOdd = "OddPages";
		internal const string SubentriesClassName = ".subentries";
		internal const string HeadwordClassName = ".headword";

		/// <summary>
		/// Generate the style that will be used for the header that goes on the top of
		/// every page.  The header style will be similar to the provided  paragraph style, with the
		/// addition of the tab stop. It will also include the run properties from the runPropStyle, because
		/// the Word header does not apply run properties applied to the run. They need to be added to the
		/// paragraph.
		/// </summary>
		/// <param name="style">The style to based the header style on.</param>
		/// <param name="runPropStyle">The style to get the run properties from.</param>
		/// <returns>The header style.</returns>
		internal static Style GeneratePageHeaderStyle(Style style, Style runPropStyle)
		{
			Style pageHeaderStyle = (Style)style.CloneNode(true);
			SetStyleName(pageHeaderStyle, PageHeaderStyleName);

			// Add the tab stop.
			var tabs = new Tabs();
			tabs.Append(new TabStop() { Val = TabStopValues.End, Position = (int)(1440 * 6.5/*inches*/) });
			pageHeaderStyle.StyleParagraphProperties.Append(tabs);

			// The Page Header paragraph needs the run properties directly added to it.
			// Adding run properties to the runs in the page header do not seem to get applied.
			var runProps = runPropStyle.GetFirstChild<StyleRunProperties>();
			pageHeaderStyle.Append(runProps.CloneNode(true));

			return pageHeaderStyle;
		}

		internal static Style GeneratePictureFrameOuterStyle(ConfigurableDictionaryNode node, WordStyleCollection s_stylecollection)
		{
			//string nodePath = CssGenerator.GetNodePath(node);
			//ParagraphElement pictureFrameOuter;
			/*//var pictureFrameOuterStyle =
			if (!s_stylecollection.TryGetParagraphStyle(nodePath, out pictureFrameOuter))
				s_stylecollection.AddParagraphStyle()*/

			var pictureFrameOuterStyle = new Style();
			pictureFrameOuterStyle.Type = StyleValues.Paragraph;
			SetStyleName(pictureFrameOuterStyle, PictureTextboxOuterDisplayName);
			SetBasedOn(pictureFrameOuterStyle, NormalParagraphDisplayName);

			//Use the image alignment specified in FLEx for the textbox alignment, with right align as default
			//For images, FLEX provides three options: center, left and right.
			string alignment = "right";
			JustificationValues enumAlignVal = JustificationValues.Right;

			if (node.DictionaryNodeOptions is DictionaryNodePictureOptions)
				alignment = node.Model.Pictures.Alignment.ToString().ToLower();
			if (alignment == "left")
				enumAlignVal = JustificationValues.Left;
			if (alignment == "center")
				enumAlignVal = JustificationValues.Center;

			if (pictureFrameOuterStyle.StyleParagraphProperties == null)
				pictureFrameOuterStyle.StyleParagraphProperties = new StyleParagraphProperties();

			// Justification here will determine the horizontal location of the image textbox within its column.
			// In FLEx, pictures have no added before/after paragraph spacing.
			pictureFrameOuterStyle.StyleParagraphProperties.Append(new Justification() { Val = enumAlignVal },
				new SpacingBetweenLines() { Before = "0", After = "0" });

			//WordStyleCollection.makePictureOuterElement(pictureFrameOuterStyle);

			/*var pictureOuterElem = new ParagraphElement(PictureTextboxOuterDisplayName,
				pictureFrameOuterStyle, 1, PictureTextboxOuterNodePath, null);

			WordStyleCollection.AddParagraphElement(pictureOuterElem);
			pictureCaptionElem.Used = true;*/

			return pictureFrameOuterStyle;
		}

		internal static bool IsParagraphStyle(string styleName, ReadOnlyPropertyTable propertyTable)
		{
			if(string.IsNullOrEmpty(styleName))
			{
				return false;
			}
			var styleSheet = FontHeightAdjuster.StyleSheetFromPropertyTable(propertyTable);
			if (styleSheet == null || !styleSheet.Styles.Contains(styleName))
			{
				return false;
			}
			var projectStyle = styleSheet.Styles[styleName];
			var exportStyleInfo = new ExportStyleInfo(projectStyle);

			return exportStyleInfo.IsParagraphStyle;
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
		internal static Style GenerateParagraphStyleFromLcmStyleSheet(string styleName,
			ReadOnlyPropertyTable propertyTable, out BulletInfo? bulletInfo)
		{
			var style = GenerateWordStyleFromLcmStyleSheet(true, styleName, DefaultStyle, propertyTable, out bulletInfo);
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
			ReadOnlyPropertyTable propertyTable, StyleRunProperties basedOnProps = null)
		{
			var style = GenerateWordStyleFromLcmStyleSheet(false, styleName, wsId, propertyTable, out BulletInfo? _, basedOnProps);
			Debug.Assert(style == null || style.Type == StyleValues.Character);
			return style;
		}

		/// <summary>
		/// Generates a Paragraph or Character Word Style for the requested FieldWorks style.
		/// If the FieldWorks style is a paragraph style then either a paragraph or character style can
		/// be returned.
		/// If the FieldWorks style is a character style then only a character style can be returned.
		/// </summary>
		/// <param name="paragraphData">True to get a paragraph data, False to get a character data.</param>
		/// <param name="styleName">Name of the character or paragraph style.</param>
		/// <param name="wsId">writing system id. Only used for character style.</param>
		/// <param name="propertyTable">To retrieve styles</param>
		/// <param name="bulletInfo">Returns the bullet and numbering info associated with the style. Returns null
		///                          if there is none. (For character styles always returns null.)</param>
		/// <returns>Returns the WordProcessing.Style item. Can return null.</returns>
		internal static Style GenerateWordStyleFromLcmStyleSheet(bool paragraphData, string styleName, int wsId,
			ReadOnlyPropertyTable propertyTable, out BulletInfo? bulletInfo, StyleRunProperties basedOnProps = null)
		{
			bulletInfo = null;
			var styleSheet = FontHeightAdjuster.StyleSheetFromPropertyTable(propertyTable);
			if (styleSheet == null || !styleSheet.Styles.Contains(styleName))
			{
				return null;
			}

			var projectStyle = styleSheet.Styles[styleName];
			var exportStyleInfo = new ExportStyleInfo(projectStyle);

			// We can't return paragraph data from a character style.
			if (!exportStyleInfo.IsParagraphStyle && paragraphData)
			{
				Debug.Assert(false, "Can't return paragraph data from a character style.");
				return null;
			}

			var exportStyle = new Style();
			// StyleId is used for style linking in the xml.
			exportStyle.StyleId = styleName.Trim('.');
			// StyleName is the name a user will see for the given style in Word's style sheet.
			exportStyle.Append(new StyleName() {Val = exportStyle.StyleId});

			// Create paragraph and run styles as specified by exportStyleInfo.
			// Only if the style to export is a paragraph style should we create paragraph formatting options like indentation, alignment, border, etc.
			if (paragraphData)
			{
				var parProps = new StyleParagraphProperties();
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
						// values in LineSpacing.m_lineHeight -- positive value means at least line height, otherwise it's exact line height
						var lineHeight = exportStyleInfo.LineSpacing.m_lineHeight;
						if (lineHeight >= 0)
						{
							lineHeight = MilliPtToTwentiPt(lineHeight);
							parProps.Append(new SpacingBetweenLines() { Line = lineHeight.ToString(), LineRule = LineSpacingRuleValues.AtLeast });
						}
						else
						{
							lineHeight = MilliPtToTwentiPt(Math.Abs(lineHeight));
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

				// Getting the character formatting info to add to the run properties
				var runProps = AddFontInfoWordStyles(projectStyle, wsId, propertyTable.GetValue<LcmCache>("cache"), basedOnProps);
				exportStyle.Append(runProps);
			}

			return exportStyle;
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

			// Remove the link to the character style. A continuation should never need an associated character style.
			contStyle.RemoveAllChildren<LinkedStyle>();

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
		/// Builds the word styles for font info properties using the writing system overrides
		/// </summary>
		private static StyleRunProperties AddFontInfoWordStyles(BaseStyleInfo projectStyle, int wsId,
			LcmCache cache, StyleRunProperties basedOnProps)
		{
			StyleRunProperties charDefaults = null;
			if (basedOnProps == null)
			{
				charDefaults = new StyleRunProperties();
			}
			else
			{
				charDefaults = (StyleRunProperties)basedOnProps.CloneNode(true);
			}

			var wsFontInfo = projectStyle.FontInfoForWs(wsId);
			var defaultFontInfo = projectStyle.DefaultCharacterStyleInfo;

			// set fontName to the wsFontInfo publicly accessible InheritableStyleProp value if set, otherwise the
			// defaultFontInfo if set, or null.
			var fontName = wsFontInfo.m_fontName.ValueIsSet ? wsFontInfo.m_fontName.Value
				: defaultFontInfo.FontName.ValueIsSet ? defaultFontInfo.FontName.Value : null;

			// If font is explicitly set in FLEx to "<default font>", this gets picked up as the fontname.
			// In that case, we want to set fontName to null in the word style so that it can be inherited from the WS.
			if (fontName == "<default font>")
			{
				fontName = null;
			}

			// fontName still null means not set in Normal Style, then get default fonts from WritingSystems configuration.
			// Comparison, projectStyle.Name == "Normal", required to limit the font-family definition to the
			// empty span (ie char). If not included, font-family will be added to many more spans.
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
				charDefaults.RemoveAllChildren<RunFonts>();
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
				charDefaults.RemoveAllChildren<FontSize>();
				charDefaults.RemoveAllChildren<FontSizeComplexScript>();
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
				charDefaults.RemoveAllChildren<Bold>();
				charDefaults.RemoveAllChildren<BoldComplexScript>();
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
				charDefaults.RemoveAllChildren<Italic>();
				charDefaults.RemoveAllChildren<ItalicComplexScript>();
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
				charDefaults.RemoveAllChildren<Color>();
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
				charDefaults.RemoveAllChildren<Shading>();
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
				charDefaults.RemoveAllChildren<VerticalTextAlignment>();
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
					charDefaults.RemoveAllChildren<Underline>();
					charDefaults.Append(oxmlUnderline);
				}
				// Else the underline is actually a strikethrough.
				else
				{
					charDefaults.RemoveAllChildren<Strike>();
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

		public static string GetWsString(string wsString)
		{
			return LangTagPre + wsString + LangTagPost;
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

		internal static void SetStyleName(Style style, string styleName)
		{
			style.StyleId = styleName;
			if (style.StyleName == null)
			{
				style.StyleName = new StyleName() { Val = style.StyleId };
			}
			else
			{
				style.StyleName.Val = style.StyleId;
			}
		}

		internal static void SetBasedOn(Style style, string basedOnValue)
		{
			if (style.BasedOn == null)
			{
				style.BasedOn = new BasedOn() { Val = basedOnValue };
			}
			else
			{
				style.BasedOn.Val = basedOnValue;
			}
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
