// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using ExCSS;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using XCore;
using Property = ExCSS.Property;

namespace SIL.FieldWorks.XWorks
{
	public static class CssGenerator
	{
		/// <summary>
		/// id that triggers using the default selection on a character style instead of a writing system specific one
		/// </summary>
		internal const int DefaultStyle = -1;

		/// <summary>
		/// Generate all the css rules necessary to represent every enabled portion of the given configuration
		/// </summary>
		/// <param name="model"></param>
		/// <param name="mediator">Necessary to access the styles as configured in FLEx</param>
		/// <returns></returns>
		public static string GenerateCssFromConfiguration(DictionaryConfigurationModel model, Mediator mediator)
		{
			if(model == null)
				throw new ArgumentNullException("model");
			var styleSheet = new StyleSheet();
			foreach(var configNode in model.Parts)
			{
				GenerateCssFromConfigurationNode(configNode, styleSheet, null, mediator);
			}
			// Pretty-print the stylesheet
			return styleSheet.ToString(true, 1);
		}

		/// <summary>
		/// Generates css rules for a configuration node and adds them to the given stylesheet (recursive).
		/// </summary>
		/// <param name="configNode"></param>
		/// <param name="styleSheet"></param>
		/// <param name="baseSelection"></param>
		/// <param name="mediator"></param>
		private static void GenerateCssFromConfigurationNode(ConfigurableDictionaryNode configNode,
																			  StyleSheet styleSheet,
																			  string baseSelection,
																			  Mediator mediator)
		{
			var rule = new StyleRule();
			var senseOptions = configNode.DictionaryNodeOptions as DictionaryNodeSenseOptions;
			if(senseOptions != null)
			{
				// Try to generate the css for the sense number before the baseSelection is updated because
				// the sense number is a sibling of the sense element and we are normally applying styles to the
				// children of collections.
				GenerateCssFromSenseOptions(configNode, styleSheet, baseSelection, senseOptions);
			}
			var beforeAfterSelectors = GenerateSelectorsFromNode(baseSelection, configNode, out baseSelection);
			rule.Value = baseSelection;
			// if the configuration node defines a style then add all the rules generated from that style
			if(!String.IsNullOrEmpty(configNode.Style))
			{
				//Generate the rules for the default font info
				rule.Declarations.Properties.AddRange(GenerateCssStyleFromFwStyleSheet(configNode.Style, DefaultStyle, mediator).Properties);
				var wsOptions = configNode.DictionaryNodeOptions as DictionaryNodeWritingSystemOptions;
				if(wsOptions != null)
				{
					GenerateCssFromWsOptions(configNode, wsOptions, styleSheet, baseSelection, mediator);
				}
			}
			foreach(var selector in beforeAfterSelectors)
				styleSheet.Rules.Add(selector);
			styleSheet.Rules.Add(rule);

			if(configNode.Children == null)
				return;
			//Recurse into each child
			foreach(var child in configNode.Children)
			{
				GenerateCssFromConfigurationNode(child, styleSheet, baseSelection, mediator);
			}
		}

		private static void GenerateCssFromSenseOptions(ConfigurableDictionaryNode configNode,
																		StyleSheet styleSheet, string baseSelection, DictionaryNodeSenseOptions senseOptions)
		{
			var senseNumberRule = new StyleRule();
			// Not using SelectClassName here sense and sensenumber are siblings and the configNode is for the Senses collection.
			// Select the base plus the node's unmodified class attribute and append the sensenumber matcher.
			var senseNumberSelector = String.Format("{0} .{1} .sensenumber", baseSelection, GetClassAttributeForConfig(configNode));

			senseNumberRule.Value = senseNumberSelector;
			var senseNumberProps = senseNumberRule.Declarations.Properties;
			if(!String.IsNullOrEmpty(senseOptions.NumberFont))
			{
				var fontFamily = new Property("font-family");
				fontFamily.Term = new TermList(new PrimitiveTerm(UnitType.String, senseOptions.NumberFont),
								 new PrimitiveTerm(UnitType.Ident, "serif"));
				senseNumberProps.Add(fontFamily);
			}
			if(!String.IsNullOrEmpty(senseOptions.NumberStyle))
			{
				// The NumberStyle can be a combination of bold or italic or unbold(-bold) or unitalic(-italic)
				if(senseOptions.NumberStyle.Contains("-bold"))
				{
					var fontWeight = new Property("font-weight");
					fontWeight.Term = new PrimitiveTerm(UnitType.Ident, "normal");
					senseNumberProps.Add(fontWeight);
				}
				else if(senseOptions.NumberStyle.Contains("bold"))
				{
					var fontWeight = new Property("font-weight");
					fontWeight.Term = new PrimitiveTerm(UnitType.Ident, "bold");
					senseNumberProps.Add(fontWeight);
				}
				if(senseOptions.NumberStyle.Contains("-italic"))
				{
					var fontStyle = new Property("font-style");
					fontStyle.Term = new PrimitiveTerm(UnitType.Ident, "normal");
					senseNumberProps.Add(fontStyle);
				}
				else if(senseOptions.NumberStyle.Contains("italic"))
				{
					var fontStyle = new Property("font-style");
					fontStyle.Term = new PrimitiveTerm(UnitType.Ident, "italic");
					senseNumberProps.Add(fontStyle);
				}
			}
			styleSheet.Rules.Add(senseNumberRule);
			if(!String.IsNullOrEmpty(senseOptions.BeforeNumber))
			{
				var beforeDeclaration = new StyleDeclaration();
				beforeDeclaration.Add(new Property("content") { Term = new PrimitiveTerm(UnitType.String, senseOptions.BeforeNumber) });
				var beforeRule = new StyleRule(beforeDeclaration) { Value = senseNumberSelector + ":before" };
				styleSheet.Rules.Add(beforeRule);
			}
			if(!String.IsNullOrEmpty(senseOptions.AfterNumber))
			{
				var afterDeclaration = new StyleDeclaration();
				afterDeclaration.Add(new Property("content") { Term = new PrimitiveTerm(UnitType.String, senseOptions.AfterNumber) });
				var afterRule = new StyleRule(afterDeclaration) { Value = senseNumberSelector + ":after" };
				styleSheet.Rules.Add(afterRule);
			}
		}

		private static void GenerateCssFromWsOptions(ConfigurableDictionaryNode configNode, DictionaryNodeWritingSystemOptions wsOptions,
																	StyleSheet styleSheet, string baseSelection, Mediator mediator)
		{
			var cache = (FdoCache)mediator.PropertyTable.GetValue("cache");
			foreach(var ws in wsOptions.Options)
			{
				var possiblyMagic = WritingSystemServices.GetMagicWsIdFromName(ws.Id);
				// if the writing system isn't a magic name just use it otherwise find the right one from the magic list
				var wsIdString = possiblyMagic == 0 ? ws.Id : WritingSystemServices.GetWritingSystemList(cache, possiblyMagic, true).First().Id;
				var wsId = cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr(wsIdString);
				var wsRule = new StyleRule();
				wsRule.Value = baseSelection + String.Format("[lang|=\"{0}\"]", wsIdString);
				wsRule.Declarations.Properties.AddRange(GenerateCssStyleFromFwStyleSheet(configNode.Style, wsId, mediator).Properties);
				styleSheet.Rules.Add(wsRule);
			}
		}

		/// <summary>
		/// This method will generate before and after rules if the configuration node requires them. It also generates the selector for the node
		/// </summary>
		/// <param name="parentInfo"></param>
		/// <param name="configNode"></param>
		/// <param name="baseSelection"></param>
		/// <returns></returns>
		private static IEnumerable<StyleRule> GenerateSelectorsFromNode(string parentInfo,
																							 ConfigurableDictionaryNode configNode,
																							 out string baseSelection)
		{
			var rules = new List<StyleRule>();
			if(parentInfo == null)
				baseSelection = SelectClassName(configNode);
			else
				baseSelection = parentInfo + " " + SelectClassName(configNode);
			if(!String.IsNullOrEmpty(configNode.Before))
			{
				var dec = new StyleDeclaration();
				dec.Add(new Property("content") { Term = new PrimitiveTerm(UnitType.String, configNode.Before) });
				var beforeRule = new StyleRule(dec) { Value = baseSelection + ":before" };
				rules.Add(beforeRule);
			}
			if(!String.IsNullOrEmpty(configNode.After))
			{
				var dec = new StyleDeclaration();
				dec.Add(new Property("content") { Term = new PrimitiveTerm(UnitType.String, configNode.After) });
				var afterRule = new StyleRule(dec) { Value = baseSelection + ":after" };
				rules.Add(afterRule);
			}
			return rules;
		}

		/// <summary>
		/// Generates a selector for a class name that matches xhtml that is generated for the configNode.
		/// e.g. '.entry' or '.sense'
		/// </summary>
		/// <param name="configNode"></param>
		/// <returns></returns>
		private static string SelectClassName(ConfigurableDictionaryNode configNode)
		{
			var type = ConfiguredXHTMLGenerator.GetPropertyTypeForConfigurationNode(configNode);
			string collectionItem = String.Empty;
			if(type == ConfiguredXHTMLGenerator.PropertyType.CollectionType)
			{
				collectionItem = GetClassAttributeForConfig(configNode);
				collectionItem = " ." + collectionItem.Remove(collectionItem.Length - 1);
			}
			return "." + GetClassAttributeForConfig(configNode) + collectionItem;
		}

		/// <summary>
		/// Generates a class name for the given configuration for use by Css and XHTML.
		/// Uses SubField and CSSClassNameOverride attributes where found
		/// </summary>
		/// <param name="configNode"></param>
		/// <returns></returns>
		internal static string GetClassAttributeForConfig(ConfigurableDictionaryNode configNode)
		{
			// write out the FieldDescription as the class name, and append a '_' followed by the SubField if it is defined.
			var classAttribute = configNode.FieldDescription +
										(String.IsNullOrEmpty(configNode.SubField) ? "" : ("_" + configNode.SubField));
			if(!String.IsNullOrEmpty(configNode.CSSClassNameOverride))
			{
					classAttribute = configNode.CSSClassNameOverride;
			}
			return classAttribute.ToLower();
		}

		/// <summary>
		/// Generates a css StyleDeclaration for the requested FieldWorks style.
		/// <remarks>internal to facilitate separate unit testing.</remarks>
		/// </summary>
		/// <param name="styleName"></param>
		/// <param name="wsId">writing system id</param>
		/// <param name="mediator"></param>
		/// <returns></returns>
		internal static StyleDeclaration GenerateCssStyleFromFwStyleSheet(string styleName, int wsId, Mediator mediator)
		{
			var declaration = new StyleDeclaration();
			var styleSheet = FontHeightAdjuster.StyleSheetFromMediator(mediator);
			if(!styleSheet.Styles.Contains(styleName))
				throw new ArgumentException(@"given stylename not found in the FwStyleSheet", styleName);
			BaseStyleInfo projectStyle = styleSheet.Styles[styleName];
			var props = styleSheet.GetStyleRgch(0, styleName);
			var exportStyleInfo = new ExportStyleInfo(projectStyle);
			if(exportStyleInfo.HasAlignment)
			{
				declaration.Add(new Property("text-align") { Term = new PrimitiveTerm(UnitType.Ident, exportStyleInfo.Alignment.AsCssString()) });
			}
			if(exportStyleInfo.HasBorder)
			{
				if(exportStyleInfo.HasBorderColor)
				{
					var borderColor = new Property("border-color");
					borderColor.Term = new HtmlColor(exportStyleInfo.BorderColor.A,
																exportStyleInfo.BorderColor.R,
																exportStyleInfo.BorderColor.G,
																exportStyleInfo.BorderColor.B);
					declaration.Add(borderColor);
				}
				var borderLeft = new Property("border-left-width");
				borderLeft.Term = new PrimitiveTerm(UnitType.Point, MilliPtToPt(exportStyleInfo.BorderLeading));
				var borderRight = new Property("border-right-width");
				borderRight.Term = new PrimitiveTerm(UnitType.Point, MilliPtToPt(exportStyleInfo.BorderTrailing));
				var borderTop = new Property("border-top-width");
				borderTop.Term = new PrimitiveTerm(UnitType.Point, MilliPtToPt(exportStyleInfo.BorderTop));
				var borderBottom = new Property("border-bottom-width");
				borderBottom.Term = new PrimitiveTerm(UnitType.Point, MilliPtToPt(exportStyleInfo.BorderBottom));
				declaration.Add(borderLeft);
				declaration.Add(borderRight);
				declaration.Add(borderTop);
				declaration.Add(borderBottom);
			}
			if(exportStyleInfo.HasFirstLineIndent)
			{
				//Handles both first-line and hanging indent, hanging-indent will result in a negative text-indent value
				declaration.Add(new Property("text-indent") { Term = new PrimitiveTerm(UnitType.Point, MilliPtToPt(exportStyleInfo.FirstLineIndent)) });
			}
			if(exportStyleInfo.HasKeepTogether)
			{
				throw new NotImplementedException("Keep Together style export not yet implemented.");
			}
			if(exportStyleInfo.HasKeepWithNext)
			{
				throw new NotImplementedException("Keep With Next style export not yet implemented.");
			}
			if(exportStyleInfo.HasLeadingIndent)
			{
				declaration.Add(new Property("padding-left") { Term = new PrimitiveTerm(UnitType.Point, MilliPtToPt(exportStyleInfo.LeadingIndent)) });
			}
			if(exportStyleInfo.HasLineSpacing)
			{
				var lineHeight = new Property("line-height");
				//m_relative means single, 1.5 or double line spacing was chosen. The CSS should be a number
				if(exportStyleInfo.LineSpacing.m_relative)
				{
					lineHeight.Term = new PrimitiveTerm(UnitType.Number, exportStyleInfo.LineSpacing.m_lineHeight);
				}
				else
				{
					lineHeight.Term = new PrimitiveTerm(UnitType.Point, MilliPtToPt(exportStyleInfo.LineSpacing.m_lineHeight));
				}
				declaration.Add(lineHeight);
			}
			if(exportStyleInfo.HasSpaceAfter)
			{
				declaration.Add(new Property("padding-bottom") { Term = new PrimitiveTerm(UnitType.Point, MilliPtToPt(exportStyleInfo.SpaceAfter)) });
			}
			if(exportStyleInfo.HasSpaceBefore)
			{
				declaration.Add(new Property("padding-top") { Term = new PrimitiveTerm(UnitType.Point, MilliPtToPt(exportStyleInfo.SpaceBefore)) });
			}
			if(exportStyleInfo.HasTrailingIndent)
			{
				declaration.Add(new Property("padding-right") { Term = new PrimitiveTerm(UnitType.Point, MilliPtToPt(exportStyleInfo.TrailingIndent)) });
			}

			AddFontInfoCss(projectStyle, declaration, wsId);

			return declaration;
		}

		/// <summary>
		/// In the FwStyles values were stored in millipoints to avoid expensive floating point calculations in c++ code.
		/// We need to convert these to points for use in css styles.
		/// </summary>
		private static float MilliPtToPt(int millipoints)
		{
			return (float)millipoints / 1000;
		}

		/// <summary>
		/// Builds the css rules for font info properties using the writing system overrides
		/// </summary>
		/// <param name="projectStyle"></param>
		/// <param name="declaration"></param>
		/// <param name="wsId">writing system id</param>
		private static void AddFontInfoCss(BaseStyleInfo projectStyle, StyleDeclaration declaration, int wsId)
		{
			var wsFontInfo = projectStyle.FontInfoForWs(wsId);
			var defaultFontInfo = projectStyle.DefaultCharacterStyleInfo;
			// set fontName to the wsFontInfo value if set, otherwise the defaultFontInfo if set, or null
			var fontName = wsFontInfo.FontName.ValueIsSet ? wsFontInfo.FontName.Value
																		 : defaultFontInfo.FontName.ValueIsSet ? defaultFontInfo.FontName.Value : null;
			if(fontName != null)
			{
				var fontFamily = new Property("font-family");
				fontFamily.Term =
					new TermList(
						new PrimitiveTerm(UnitType.String, fontName),
						new PrimitiveTerm(UnitType.Ident, "serif"));
				declaration.Add(fontFamily);
			}

			AddInfoFromWsOrDefaultValue(wsFontInfo.FontSize, defaultFontInfo.FontSize, "font-size", UnitType.Point, declaration);
			AddInfoFromWsOrDefaultValue(wsFontInfo.Bold, defaultFontInfo.Bold, "font-weight", "bold", "normal", declaration);
			AddInfoFromWsOrDefaultValue(wsFontInfo.Italic, defaultFontInfo.Italic, "font-style", "italic", "normal", declaration);
			AddInfoFromWsOrDefaultValue(wsFontInfo.FontColor, defaultFontInfo.FontColor, "color", declaration);
			AddInfoFromWsOrDefaultValue(wsFontInfo.BackColor, defaultFontInfo.BackColor, "background-color", declaration);
			AddInfoFromWsOrDefaultValue(wsFontInfo.SuperSub, defaultFontInfo.SuperSub, "vertical-align", declaration);
			AddInfoForUnderline(wsFontInfo, defaultFontInfo, declaration);
		}

		/// <summary>
		/// Generates css from boolean style values using writing system overrides where appropriate
		/// </summary>
		/// <param name="wsFontInfo"></param>
		/// <param name="defaultFontInfo"></param>
		/// <param name="propName"></param>
		/// <param name="trueValue"></param>
		/// <param name="falseValue"></param>
		/// <param name="declaration"></param>
		private static void AddInfoFromWsOrDefaultValue(IStyleProp<bool> wsFontInfo,
																		IStyleProp<bool> defaultFontInfo, string propName, string trueValue,
																		string falseValue, StyleDeclaration declaration)
		{
			bool fontValue;
			if(!GetFontValue(wsFontInfo, defaultFontInfo, out fontValue))
				return;
			var fontProp = new Property(propName);
			fontProp.Term = new PrimitiveTerm(UnitType.Ident, fontValue ? trueValue : falseValue);
			declaration.Add(fontProp);
		}

		/// <summary>
		/// Generates css from Color style values using writing system overrides where appropriate
		/// </summary>
		/// <param name="wsFontInfo"></param>
		/// <param name="defaultFontInfo"></param>
		/// <param name="propName"></param>
		/// <param name="declaration"></param>
		private static void AddInfoFromWsOrDefaultValue(IStyleProp<Color> wsFontInfo,
																		IStyleProp<Color> defaultFontInfo, string propName, StyleDeclaration declaration)
		{
			Color fontValue;
			if(!GetFontValue(wsFontInfo, defaultFontInfo, out fontValue))
				return;
			var fontProp = new Property(propName);
			fontProp.Term = new PrimitiveTerm(UnitType.RGB,
														 HtmlColor.FromRgba(fontValue.R, fontValue.G, fontValue.B,
																				  fontValue.A).ToString());
			declaration.Add(fontProp);
		}

		/// <summary>
		/// Generates css from integer style values using writing system overrides where appropriate
		/// </summary>
		/// <param name="wsFontInfo"></param>
		/// <param name="defaultFontInfo"></param>
		/// <param name="propName"></param>
		/// <param name="termType"></param>
		/// <param name="declaration"></param>
		private static void AddInfoFromWsOrDefaultValue(IStyleProp<int> wsFontInfo,
																		IStyleProp<int> defaultFontInfo, string propName, UnitType termType,
																		StyleDeclaration declaration)
		{
			int fontValue;
			if(!GetFontValue(wsFontInfo, defaultFontInfo, out fontValue))
				return;
			var fontProp = new Property(propName);
			fontProp.Term = new PrimitiveTerm(termType, MilliPtToPt(fontValue));
			declaration.Add(fontProp);
		}

		/// <summary>
		/// Generates css from SuperSub style values using writing system overrides where appropriate
		/// </summary>
		/// <param name="wsFontInfo"></param>
		/// <param name="defaultFontInfo"></param>
		/// <param name="propName"></param>
		/// <param name="declaration"></param>
		private static void AddInfoFromWsOrDefaultValue(IStyleProp<FwSuperscriptVal> wsFontInfo,
																		IStyleProp<FwSuperscriptVal> defaultFontInfo, string propName, StyleDeclaration declaration)
		{
			FwSuperscriptVal fontValue;
			if(!GetFontValue(wsFontInfo, defaultFontInfo, out fontValue))
				return;
			var fontProp = new Property(propName);
			string subSuperVal = "inherit";
			switch(fontValue)
			{
				case (FwSuperscriptVal.kssvSub):
				{
					subSuperVal = "sub";
					break;
				}
				case (FwSuperscriptVal.kssvSuper):
				{
					subSuperVal = "super";
					break;
				}
				case (FwSuperscriptVal.kssvOff):
				{
					subSuperVal = "initial";
					break;
				}
			}
			fontProp.Term = new PrimitiveTerm(UnitType.Ident, subSuperVal);
			declaration.Add(fontProp);
		}

		private static void AddInfoForUnderline(FontInfo wsFont, ICharacterStyleInfo defaultFont, StyleDeclaration declaration)
		{
			FwUnderlineType underlineType;
			if(!GetFontValue(wsFont.Underline, defaultFont.Underline, out underlineType))
				return;
			switch(underlineType)
			{
				case(FwUnderlineType.kuntDouble):
				{
					// use border to generate second underline then generate the standard underline
					var fontProp = new Property("border-bottom");
					var termList = new TermList();
					termList.AddTerm(new PrimitiveTerm(UnitType.Pixel, 1));
					termList.AddSeparator(TermList.TermSeparator.Space);
					termList.AddTerm(new PrimitiveTerm(UnitType.Ident, "solid"));
					fontProp.Term = termList;
					declaration.Add(fontProp);
					AddInfoFromWsOrDefaultValue(wsFont.UnderlineColor, defaultFont.UnderlineColor, "border-bottom-color", declaration);
					goto case FwUnderlineType.kuntSingle; //fall through to single
				}
				case(FwUnderlineType.kuntSingle):
				{
					var fontProp = new Property("text-decoration");
					fontProp.Term = new PrimitiveTerm(UnitType.Ident, "underline");
					declaration.Add(fontProp);
					AddInfoFromWsOrDefaultValue(wsFont.UnderlineColor, defaultFont.UnderlineColor, "text-decoration-color", declaration);
					break;
				}
				case(FwUnderlineType.kuntStrikethrough):
				{
					var fontProp = new Property("text-decoration");
					fontProp.Term = new PrimitiveTerm(UnitType.Ident, "line-through");
					declaration.Add(fontProp);
					AddInfoFromWsOrDefaultValue(wsFont.UnderlineColor, defaultFont.UnderlineColor, "text-decoration-color", declaration);
					break;
				}
				case (FwUnderlineType.kuntDashed):
				case (FwUnderlineType.kuntDotted):
				{
					// use border to generate a dotted or dashed underline
					var fontProp = new Property("border-bottom");
					var termList = new TermList();
					termList.AddTerm(new PrimitiveTerm(UnitType.Pixel, 1));
					termList.AddSeparator(TermList.TermSeparator.Space);
					termList.AddTerm(new PrimitiveTerm(UnitType.Ident,
																  underlineType == FwUnderlineType.kuntDashed ? "dashed" : "dotted"));
					fontProp.Term = termList;
					declaration.Add(fontProp);
					AddInfoFromWsOrDefaultValue(wsFont.UnderlineColor, defaultFont.UnderlineColor, "border-bottom-color", declaration);
					break;
				}
			}
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
		private static bool GetFontValue<T>(IStyleProp<T> wsFontInfo, IStyleProp<T> defaultFontInfo,
													out T fontValue)
		{
			fontValue = default(T);
			if(wsFontInfo.ValueIsSet)
				fontValue = wsFontInfo.Value;
			else if(defaultFontInfo.ValueIsSet)
				fontValue = defaultFontInfo.Value;
			else
				return false;
			return true;
		}

		/// <summary>
		/// Extension method to provide a css string conversion from an FwTextAlign enum value
		/// </summary>
		/// <param name="align"></param>
		/// <returns></returns>
		public static String AsCssString(this FwTextAlign align)
		{
			switch(align)
			{
				case (FwTextAlign.ktalJustify):
					return "justify";
				case (FwTextAlign.ktalCenter):
					return "center";
				case (FwTextAlign.ktalLeading):
					return "start";
				case (FwTextAlign.ktalTrailing):
					return "end";
				case (FwTextAlign.ktalLeft):
					return "left";
				case (FwTextAlign.ktalRight):
					return "right";
				default:
					return "inherit";
			}
		}

		public static string GenerateLetterHeaderCss(Mediator mediator)
		{
			var letHeadRule = new StyleRule { Value = ".letHead" };
			letHeadRule.Declarations.Properties.Add(new Property("column-count") { Term = new PrimitiveTerm(UnitType.Number, 1)});
			letHeadRule.Declarations.Properties.Add(new Property("clear") { Term = new PrimitiveTerm(UnitType.Ident, "both") });

			var letterRule = new StyleRule { Value = ".letter" };
			letterRule.Declarations.Properties.Add(new Property("text-align") { Term = new PrimitiveTerm(UnitType.Ident, "center") });
			letterRule.Declarations.Properties.Add(new Property("width") { Term = new PrimitiveTerm(UnitType.Percentage, 100) });
			var cache = (FdoCache)mediator.PropertyTable.GetValue("cache");
			letterRule.Declarations.Properties.AddRange(GenerateCssStyleFromFwStyleSheet("Dictionary-LetterHeading", cache.DefaultVernWs, mediator));
			return letHeadRule.ToString(true) + Environment.NewLine + letterRule.ToString(true) + Environment.NewLine;
		}
	}
}