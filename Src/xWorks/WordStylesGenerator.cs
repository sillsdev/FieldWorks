using DocumentFormat.OpenXml.Wordprocessing;
using ExCSS;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.Widgets;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.DomainImpl;
using SIL.LCModel.DomainServices;
using System;
using System.Collections.Generic;
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

		// Names for global and default styles
		internal const string BeforeAfterBetweenStyleName = "Dictionary-Context";
		internal const string LetterHeadingStyleName = "Dictionary-LetterHeading";
		internal const string SenseNumberStyleName = "Dictionary-SenseNumber";
		internal const string DictionaryNormal = "Dictionary-Normal";
		internal const string DictionaryMinor = "Dictionary-Minor";
		internal const string WritingSystemPrefix = "writingsystemprefix";
		internal const string WritingSystemStyleName = "Writing System Abbreviation";
		internal const string PictureAndCaptionTextframeStyle = "Image-Textframe-Style";
		internal const string EntryStyleContinue = "-Continue";

		public static Style GenerateLetterHeaderStyle(
			ReadOnlyPropertyTable propertyTable, LcmStyleSheet mediatorStyleSheet)
		{
			return GenerateWordStyleFromLcmStyleSheet(LetterHeadingStyleName, 0, propertyTable);
		}

		public static Style GenerateBeforeAfterBetweenStyle(ReadOnlyPropertyTable propertyTable)
		{
			return GenerateWordStyleFromLcmStyleSheet(BeforeAfterBetweenStyleName, 0, propertyTable);
		}

		public static Styles GetDefaultWordStyles(ReadOnlyPropertyTable propertyTable, LcmStyleSheet propStyleSheet, DictionaryConfigurationModel model)
		{
			var styles = new Styles();
			if (propStyleSheet == null)
				return null;
			// Normal is added as a default style; this means all styles will inherit from Normal unless specified otherwise.
			if (propStyleSheet.Styles.Contains("Normal"))
				styles = AddRange(styles, GetWordStyleForWsSpanWithNormalStyle(propertyTable));

			// TODO: handle DictionaryNormal, DictionaryMinor, and LetterHeadingForWritingSystem styles

			return styles;
		}

		/// <summary>
		/// Generates a Word Style for the requested FieldWorks style.
		/// <remarks>internal to facilitate separate unit testing.</remarks>
		/// </summary>
		/// <param name="styleName"></param>
		/// <param name="wsId">writing system id</param>
		/// <param name="propertyTable"></param>
		/// <returns></returns>
		internal static Style GenerateWordStyleFromLcmStyleSheet(
			string styleName, int wsId, ReadOnlyPropertyTable propertyTable)
		{
			return GenerateWordStyleFromLcmStyleSheet(styleName, wsId, null, propertyTable);
		}

		/// <summary>
		/// Generates a css StyleDeclaration for the requested FieldWorks style.
		/// <remarks>internal to facilitate separate unit testing.</remarks>
		/// </summary>
		/// <param name="styleName"></param>
		/// <param name="wsId">writing system id</param>
		/// <param name="node">The configuration node to use for generating paragraph margin in context</param>
		/// <param name="propertyTable">To retrieve styles</param>
		/// <returns></returns>
		internal static Style GenerateWordStyleFromLcmStyleSheet(
			string styleName, int wsId,
			ConfigurableDictionaryNode node, ReadOnlyPropertyTable propertyTable)
		{
			return GenerateWordStyleFromLcmStyleSheet(styleName, wsId, node, propertyTable,
				false, true);
		}

		/// <summary>
		/// Generates Word Styles for the requested FieldWorks style.
		/// <remarks>
		/// Internal to facilitate separate unit testing.
		/// Returns a List of WordProcessing.Style items
		/// </remarks>
		/// </summary>
		/// <param name="styleName"></param>
		/// <param name="wsId">writing system id</param>
		/// <param name="node">The configuration node to use for generating paragraph margin in context</param>
		/// <param name="propertyTable">To retrieve styles</param>
		/// <param name="allowFirstLineIndent">Indicates if the style returned should include FirstLineIndent.</param>
		/// <returns></returns>
		internal static Style GenerateWordStyleFromLcmStyleSheet(
			string styleName, int wsId, ConfigurableDictionaryNode node,
			ReadOnlyPropertyTable propertyTable, bool calculateFirstSenseStyle, bool allowFirstLineIndent)
		{
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
			var parProps = new ParagraphProperties();
			var runProps = new StyleRunProperties();

			if (exportStyleInfo.BasedOnStyle?.Name != null)
				exportStyle.BasedOn = new BasedOn() { Val = exportStyleInfo.BasedOnStyle.Name };

			// Create paragraph and run styles as specified by exportStyleInfo.
			// Only if the style to export is a paragraph style should we create paragraph formatting options like indentation, alignment, border, etc.
			if (exportStyleInfo.IsParagraphStyle)
			{
				exportStyle.Type = StyleValues.Paragraph;
				var hangingIndent = 0.0f;

				// Tuple ancestorIndents used for ancestor components leadingIndent and hangingIndent.
				var ancestorIndents = new AncestorIndents(0.0f, 0.0f);
				if (exportStyleInfo.IsParagraphStyle && node != null)
					ancestorIndents = CalculateParagraphIndentsFromAncestors(node, styleSheet, ancestorIndents);

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

					if (allowFirstLineIndent)
					{
						parProps.Append(new Indentation() { FirstLine = firstLineIndentValue.ToString() });
					}
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

				// calculate leading indent, unless it will be calculated later for first sense style
				if (exportStyleInfo.HasLeadingIndent || hangingIndent < 0.0f ||
					ancestorIndents.TextIndent < 0.0f)
				{
					if (!calculateFirstSenseStyle || ancestorIndents.Ancestor == null)
					{
						var leadingIndent = CalculateMarginLeft(exportStyleInfo, ancestorIndents, hangingIndent);

						if (exportStyleInfo.DirectionIsRightToLeft == TriStateBool.triTrue)
							parProps.Append(new Indentation() { Right = leadingIndent.ToString() });
						else
							parProps.Append(new Indentation() { Left = leadingIndent.ToString() });
					}
					else
					{
						var senseOptions = ancestorIndents.Ancestor.DictionaryNodeOptions as DictionaryNodeSenseOptions;
						if (senseOptions == null || !senseOptions.DisplayEachSenseInAParagraph)
						{
							var leadingIndent = CalculateMarginLeft(exportStyleInfo, ancestorIndents, hangingIndent);

							if (exportStyleInfo.DirectionIsRightToLeft == TriStateBool.triTrue)
								parProps.Append(new Indentation() { Right = leadingIndent.ToString() });
							else
								parProps.Append(new Indentation() { Left = leadingIndent.ToString() });
						}
						// else, leading indent will be added when we calculate the first sense style.
					}

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
					// Check bidirectional flag to determine correct orientation for indent
					if (exportStyleInfo.DirectionIsRightToLeft == TriStateBool.triTrue)
						parProps.Append(new Indentation() { Left = MilliPtToTwentiPt(exportStyleInfo.TrailingIndent).ToString() });
					else
						parProps.Append(new Indentation() { Right = MilliPtToTwentiPt(exportStyleInfo.TrailingIndent).ToString() });
				}

				// if leadingIndent was not calculated above, indent will be calculated now for first sense style
				if (calculateFirstSenseStyle && ancestorIndents.Ancestor != null)
				{
					var senseOptions = ancestorIndents.Ancestor.DictionaryNodeOptions as DictionaryNodeSenseOptions;
					if (senseOptions != null && senseOptions.DisplayEachSenseInAParagraph)
					{
						ancestorIndents = CalculateParagraphIndentsFromAncestors(ancestorIndents.Ancestor, styleSheet, new AncestorIndents(0f, 0f));
						var leadingIndent = CalculateMarginLeft(exportStyleInfo, ancestorIndents, hangingIndent);

						// Check bidirectional flag to determine correct orientation for indent
						if (exportStyleInfo.DirectionIsRightToLeft == TriStateBool.triTrue)
							parProps.Append(new Indentation() { Right = leadingIndent.ToString() });
						else
							parProps.Append(new Indentation() { Left = leadingIndent.ToString() });
					}
				}

				// If text direction is right to left, add BiDi property to the paragraph.
				if (exportStyleInfo.DirectionIsRightToLeft == TriStateBool.triTrue)
				{
					parProps.Append(new BiDi());
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
		/// Generates openxml styles for a configuration node and adds them to the given stylesheet (recursive).
		/// </summary>
		public static Styles GenerateWordStylesFromConfigurationNode(
			ConfigurableDictionaryNode configNode, string styleName,
			ReadOnlyPropertyTable propertyTable)
		{
			var cache = propertyTable.GetValue<LcmCache>("cache");
			switch (configNode.DictionaryNodeOptions)
			{
				case DictionaryNodeSenseOptions senseOptions:
					// Try to generate style for the sense number before the baseSelection is updated b/c
					// the sense number is a sibling of the sense element and we are normally applying styles to the
					// children of collections.
					return GenerateWordStyleForSenses(configNode, senseOptions, ref styleName, propertyTable);

				// TODO: handle listAndPara case and character portion of pictureOptions
				// case IParaOption listAndParaOpts:

				case DictionaryNodePictureOptions pictureOptions:
					return GenerateWordStyleFromPictureOptions(configNode, pictureOptions, styleName, cache, propertyTable);

				default:
					{
						var rule = new Style();

						var rules = new Styles();

						var wsOptions = configNode.DictionaryNodeOptions as DictionaryNodeWritingSystemOptions;
						if (wsOptions != null)
						{
							rules = AddRange(rules, GenerateWordStyleFromWsOptions(configNode, wsOptions, styleName, propertyTable));
							if (wsOptions.DisplayWritingSystemAbbreviations)
							{
								rules = AddRange(rules, GenerateWordStylesForWritingSystemPrefix(configNode, styleName, propertyTable));
							}
						}

						// if the configuration node defines a style then add all the rules generated from that style
						if (!string.IsNullOrEmpty(configNode.Style))
						{
							//Generate the rules for the default font info
							rule = GenerateWordStyleFromLcmStyleSheet(configNode.Style, DefaultStyle, configNode, propertyTable);

							// TODO: how to handle span vs div etc.
							//selectors.AddRange(GenerateWordStylesForWritingSystems(baseSelection + " span", configNode.Style, propertyTable));
						}

						if (configNode.Style != null)
							rule.StyleId = configNode.Style;
						rules.AppendChild(rule.CloneNode(true));
						return rules;
					}
			}
		}

		/// <summary>
		/// Creates run properties/character styles for the default "Normal" style
		/// </summary>
		/// <param name="propertyTable"></param>
		/// <returns></returns>
		private static Styles GetWordStyleForWsSpanWithNormalStyle(ReadOnlyPropertyTable propertyTable)
		{
			var styles = new Styles();
			var defaultStyleProps = new Style();

			// Generate character style rules for the default "Normal" style info
			var normalStyle = GetOnlyCharacterStyle(GenerateWordStyleFromLcmStyleSheet("Normal", DefaultStyle, propertyTable));
			var defaultRunProps = new RunPropertiesDefault();
			if (normalStyle.Descendants<StyleRunProperties>().Any())
			{
				foreach (var item in normalStyle.Descendants<StyleRunProperties>())
				{
					defaultRunProps.AppendChild(item.CloneNode(true));
					defaultStyleProps.AppendChild(defaultRunProps);
				}
			}

			// if a default fontsize wasn't set, set one now
			if (!defaultRunProps.Descendants<FontSize>().Any())
			{
				List<StyleRunProperties> runProps =
					defaultRunProps.OfType<StyleRunProperties>().ToList();

				// Append desired default font size to the last runproperties element in the default run properties (if one exists)
				if (runProps.Any())
					// kDefaultFontSize stores the desired fontsize in points. Openxml expects fontsize given in half points, so we multiply by 2
					runProps.Last().Append(new FontSize() { Val = (FontInfo.kDefaultFontSize * 2).ToString() });

				// Else, create a new runproperties element with the desired default font size and append it to the default run properties
				else
				{
					RunProperties newRunProps = new RunProperties();
					newRunProps.Append(new FontSize() { Val = (FontInfo.kDefaultFontSize * 2).ToString() });
					defaultRunProps.Append(newRunProps);
				}
			}

			defaultStyleProps.StyleId = "Normal";
			styles.Append(defaultStyleProps);
			styles = AddRange(styles, GenerateWordStylesForWritingSystems("span", "Normal", propertyTable));
;
			return styles;
		}

		private static Styles GenerateWordStylesForWritingSystems(string selector, string styleName, ReadOnlyPropertyTable propertyTable)
		{
			var cache = propertyTable.GetValue<LcmCache>("cache");
			var styleRules = new Styles();
			// Generate the rules for all the writing system overrides
			foreach (var aws in cache.ServiceLocator.WritingSystems.AllWritingSystems)
			{
				Style wsCharStyle = GetOnlyCharacterStyle(GenerateWordStyleFromLcmStyleSheet(styleName, aws.Handle, propertyTable));
				wsCharStyle.StyleId = selector + GetWsString(aws.LanguageTag);
				wsCharStyle.StyleName = new StyleName() { Val = wsCharStyle.StyleId };

				styleRules.Append(wsCharStyle);
			}

			return styleRules;
		}

		private static Style GenerateWordStyleFromWsOptions(ConfigurableDictionaryNode configNode, DictionaryNodeWritingSystemOptions wsOptions,
			string baseSelection, ReadOnlyPropertyTable propertyTable)
		{
			var cache = propertyTable.GetValue<LcmCache>("cache");
			foreach (var ws in wsOptions.Options.Where(opt => opt.IsEnabled))
			{
				var possiblyMagic = WritingSystemServices.GetMagicWsIdFromName(ws.Id);
				// if the writing system isn't a magic name just use it otherwise find the right one from the magic list
				var wsIdString = possiblyMagic == 0 ? ws.Id : WritingSystemServices.GetWritingSystemList(cache, possiblyMagic, true).First().Id;
				var wsId = cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr(wsIdString);
				var wsString = GetWsString(wsIdString).Trim('.');

				var wsStyle = new Style();

				if (!string.IsNullOrEmpty(configNode.Style))
					wsStyle = GenerateWordStyleFromLcmStyleSheet(configNode.Style, wsId, propertyTable);

				// Any given style can only be based on one style.
				// This style should be based on the span for the current ws;
				// style info for the current node (independent of WS) should be added during creation of this style.
				wsStyle.Append(new BasedOn() { Val = "span" + wsString });

				wsStyle.StyleId = configNode.Style + wsString;
				wsStyle.StyleName = new StyleName(){ Val = wsStyle.StyleId };

				if (!IsEmptyStyle(wsStyle))
					return wsStyle;
			}

			return new Style();
		}

		private static Styles GenerateWordStyleForSenses(ConfigurableDictionaryNode configNode, DictionaryNodeSenseOptions senseOptions, ref string baseSelection, ReadOnlyPropertyTable propertyTable)
		{
			var styleRules = new Styles();

			// TODO: Handle before/after/between content
			//if (senseOptions.DisplayEachSenseInAParagraph)
			//	selectors = RemoveBeforeAfterSelectorRules(selectors);
			//styleRules = AddRange(styleRules, CheckRangeOfStylesForEmpties(selectors));

			var cache = propertyTable.GetValue<LcmCache>("cache");
			var senseNumberLanguage = cache.ServiceLocator.GetInstance<HomographConfiguration>().WritingSystem;
			senseNumberLanguage = string.IsNullOrEmpty(senseNumberLanguage) ? "en" : senseNumberLanguage;
			var senseNumberWsId = cache.WritingSystemFactory.GetWsFromStr(senseNumberLanguage);
			var senseNumberStyle = new Style();

			if (!String.IsNullOrEmpty(senseOptions.NumberStyle))
			{
				senseNumberStyle = GenerateWordStyleFromLcmStyleSheet(senseOptions.NumberStyle, senseNumberWsId, propertyTable);
			}

			senseNumberStyle.StyleId = SenseNumberStyleName;
			if (!IsEmptyStyle(senseNumberStyle))
				styleRules = AddRange(styleRules, senseNumberStyle);

			Style styleDeclaration = string.IsNullOrEmpty(configNode.Style) ? new Style() : GenerateWordStyleFromLcmStyleSheet(configNode.Style, 0, configNode, propertyTable);
			if (senseOptions.DisplayEachSenseInAParagraph)
			{
				Style senseCharStyle = GetOnlyCharacterStyle(styleDeclaration);
				senseCharStyle.StyleId = configNode.Style;

				if (!IsEmptyStyle(senseCharStyle))
					styleRules.Append(senseCharStyle);

				Style senseParaStyle = GetOnlyParagraphStyle(styleDeclaration);

				// TODO: goal is to apply the paragraph style information to all but the first sensecontent block, if requested -- how to achieve this with word/openxml?
				senseParaStyle.StyleId = configNode.Style;

				styleRules.Append(senseParaStyle);

				// TODO: append bulleted list style after handling custom bullet/numbering systems
			}
			else
			{
				// Generate the character style information specifically for senses
				Style senseContentStyle = GetOnlyCharacterStyle(styleDeclaration);
				senseContentStyle.StyleId = configNode.Style;

				if (!IsEmptyStyle(senseContentStyle))
					styleRules.Append(senseContentStyle);
			}

			return styleRules;
		}

		private static Styles GenerateWordStyleFromPictureOptions(ConfigurableDictionaryNode configNode, DictionaryNodePictureOptions pictureOptions,
			string baseSelection, LcmCache cache, ReadOnlyPropertyTable propertyTable)
		{
			var styles = new Styles();

			var frameStyle = new Style();

			// A textframe for holding an image/caption has to be a paragraph
			frameStyle.Type = StyleValues.Paragraph;

			// We use FLEX's max image width as the width for the textframe.
			// Note: 1 inch is equivalent to 72 points, and width is specified in twentieths of a point.
			// Thus, we calculate textframe width by multiplying max image width in inches by 72*30 = 1440
			var textFrameWidth = LcmWordGenerator.maxImageWidthInches * 1440;

			// A paragraph is turned into a textframe simply by adding a frameproperties object inside the paragraph properties.
			// We leave a 4-pt border around the textframe--80 twentieths of a point.
			var textFrameBorder = "80";
			var textFrameProps = new FrameProperties() { Width = textFrameWidth.ToString(), HeightType = HeightRuleValues.Auto, HorizontalSpace = textFrameBorder, VerticalSpace = textFrameBorder, Wrap = TextWrappingValues.NotBeside, VerticalPosition = VerticalAnchorValues.Text, HorizontalPosition = HorizontalAnchorValues.Margin, XAlign = HorizontalAlignmentValues.Right };
			var parProps = new ParagraphProperties();
			frameStyle.StyleId = PictureAndCaptionTextframeStyle;
			frameStyle.StyleName = new StyleName(){Val = PictureAndCaptionTextframeStyle};
			parProps.Append(textFrameProps);
			frameStyle.Append(parProps);
			styles.Append(frameStyle);

			//TODO: define picture/caption character styles based on user specifications in FLEx
			return styles;
		}

		private static Styles GenerateWordStylesFromListAndParaOptions(ConfigurableDictionaryNode configNode,
			IParaOption listAndParaOpts, ref string baseSelection, LcmCache cache, ReadOnlyPropertyTable propertyTable)
		{
			// TODO: Generate these styles when we implement custom numbering as well as before/after + separate paragraphs in styles
			return null;
		}

		private static Styles GenerateWordStylesForWritingSystemPrefix(ConfigurableDictionaryNode configNode, string baseSelection, ReadOnlyPropertyTable propertyTable)
		{
			var styleRules = new Styles();
			var wsRule1 = GetOnlyCharacterStyle(GenerateWordStyleFromLcmStyleSheet(WritingSystemStyleName, 0, configNode, propertyTable));
			wsRule1.StyleId = (string.Format("{0}.{1}", baseSelection, WritingSystemPrefix)).Trim('.');
			wsRule1.StyleName = new StyleName() { Val = wsRule1.StyleId };
			styleRules = AddRange(styleRules, wsRule1);

			// TODO: Determine how to handle after content in Word export (can't add content via a style)
			/*var wsRule2 = new Style { StyleId = string.Format("{0}.{1}:after", baseSelection, WritingSystemPrefix) };
			wsRule2.Declarations.Properties.Add(new Property("content") { Term = new PrimitiveTerm(UnitType.String, " ") });
			styleRules.Add(wsRule2);*/

			return styleRules;
		}

		/// <summary>
		/// Create the 'continuation' style for the entry, which is needed when an entry contains multiple paragraphs. This
		/// style will be used for all but the first paragraph. It is the same as the style for the first paragraph except
		/// that it does not contain the first line indenting.
		/// </summary>
		/// <returns>Returns the continuation style.</returns>
		internal static Styles GenerateContinuationWordStyles(
			ConfigurableDictionaryNode node, ReadOnlyPropertyTable propertyTable)
		{
			Style contStyle = GenerateWordStyleFromLcmStyleSheet(node.Style, DefaultStyle, node,
				propertyTable, false, false);
			contStyle.StyleName.Val = node.Style + EntryStyleContinue;
			contStyle.StyleId = node.Style + EntryStyleContinue;

			var retStyles = new Styles();
			retStyles.AppendChild(contStyle.CloneNode(true));
			return retStyles;
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
			if (fontName == null && projectStyle.Name == "Normal")
			{
				var lgWritingSystem = cache.ServiceLocator.WritingSystemManager.get_EngineOrNull(wsId);
				if (lgWritingSystem != null)
					fontName = lgWritingSystem.DefaultFontName;
			}

			if (fontName != null)
			{
				// Note: if desired, multiple fonts can be used for different text types in a single run
				// by separately specifying font names to use for ASCII, High ANSI, Complex Script, and East Asian content.
				var font = new RunFonts(){Ascii = fontName};
				charDefaults.Append(font);
			}

			// For the following additions, wsFontInfo is a publicly accessible InheritableStyleProp value if set (ie. m_fontSize, m_bold, etc.).
			// We check for explicit overrides. Otherwise the defaultFontInfo if set (ie. FontSize, Bold, etc), or null.

			// Check fontsize
			int fontSize;
			if (GetFontValue(wsFontInfo.m_fontSize, defaultFontInfo.FontSize, out fontSize))
			{
				// Fontsize is stored internally multiplied by 1000.  (FieldWorks code generally hates floating point.)
				// OpenXML expects fontsize given in halves of a point; thus we divide by 500.
				fontSize = fontSize / 500;
				var size = new FontSize() { Val = fontSize.ToString() };
				charDefaults.Append(size);
			}

			// Check for bold
			bool bold;
			GetFontValue(wsFontInfo.m_bold, defaultFontInfo.Bold, out bold);
			if (bold)
			{
				var boldFont = new Bold() { Val = true };
				charDefaults.Append(boldFont);
			}

			// Check for italic
			bool ital;
			GetFontValue(wsFontInfo.m_italic, defaultFontInfo.Italic, out ital);
			if (ital)
			{
				var italFont = new Italic() { Val = true };
				charDefaults.Append(italFont);
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

		public static string GetWsString(string wsId)
		{
			return String.Format("[lang=\'{0}\']", wsId);
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

		private static AncestorIndents CalculateParagraphIndentsFromAncestors(ConfigurableDictionaryNode currentNode,
			LcmStyleSheet styleSheet, AncestorIndents ancestorIndents)
		{
			var parentNode = currentNode;
			do
			{
				parentNode = parentNode.Parent;
				if (parentNode == null)
					return ancestorIndents;
			} while (!IsParagraphStyle(parentNode, styleSheet));

			var projectStyle = styleSheet.Styles[parentNode.Style];
			var exportStyleInfo = new ExportStyleInfo(projectStyle);

			return new AncestorIndents(parentNode, GetLeadingIndent(exportStyleInfo), GetHangingIndentIfAny(exportStyleInfo));
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
				var leadingIndent = CalculateMarginLeft(exportStyleInfo, new AncestorIndents(0.0f, 0.0f), hangingIndent);
				indentVal = (int)leadingIndent;
			}

			// Get the alignment direction.
			tableAlignment = exportStyleInfo.DirectionIsRightToLeft == TriStateBool.triTrue ?
				TableRowAlignmentValues.Right : TableRowAlignmentValues.Left;

			return indentVal;
		}

		private static float CalculateMarginLeft(ExportStyleInfo exportStyleInfo, AncestorIndents ancestorIndents,
			float hangingIndent)
		{
			var leadingIndent = 0.0f;
			if (exportStyleInfo.HasLeadingIndent)
			{
				leadingIndent = MilliPtToTwentiPt(exportStyleInfo.LeadingIndent);
			}

			var ancestorMargin = ancestorIndents.Margin - ancestorIndents.TextIndent;
			leadingIndent -= ancestorMargin + hangingIndent;
			return leadingIndent;
		}

		private static float GetHangingIndentIfAny(ExportStyleInfo exportStyleInfo)
		{
			// Handles both first-line and hanging indent: hanging indent represented as a negative first-line indent value
			return exportStyleInfo.HasFirstLineIndent && exportStyleInfo.FirstLineIndent < 0 ?
				MilliPtToTwentiPt(exportStyleInfo.FirstLineIndent) : 0.0f;
		}

		private static float GetLeadingIndent(ExportStyleInfo exportStyleInfo)
		{
			return exportStyleInfo.HasLeadingIndent ? MilliPtToTwentiPt(exportStyleInfo.LeadingIndent) : 0.0f;
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

		public static bool AreStylesEquivalent(Style first,
			Style second)
		{
			// OuterXml gets the markup that represents the current element and all of its child elements.
			// All styles and style specification added to the styles element will be its children;
			// if the styles are equivalent, they should have the same child elements.
			if (first.OuterXml.Equals(second.OuterXml))
				return true;

			return false;
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

		private class AncestorIndents
		{
			public AncestorIndents(float margin, float textIndent) : this(null, margin, textIndent)
			{
			}

			public AncestorIndents(ConfigurableDictionaryNode ancestor, float margin, float textIndent)
			{
				Ancestor = ancestor;
				Margin = margin;
				TextIndent = textIndent;
			}

			public float Margin { get; private set; }
			public float TextIndent { get; private set; }
			public ConfigurableDictionaryNode Ancestor { get; private set; }
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
