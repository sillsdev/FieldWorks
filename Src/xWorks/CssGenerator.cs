// Copyright (c) 2014-2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using ExCSS;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.XWorks.DictionaryDetailsView;
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

		internal const string BeforeAfterBetweenStyleName = "Dictionary-Context";
		internal const string LetterHeadingStyleName = "Dictionary-LetterHeading";
		internal const string DictionaryNormal = "Dictionary-Normal";
		internal const string DictionaryMinor = "Dictionary-Minor";
		internal const string WritingSystemPrefix = "writingsystemprefix";
		internal const string WritingSystemStyleName = "Writing System Abbreviation";
		private static readonly Dictionary<string, string> BulletSymbolsCollection = new Dictionary<string, string>();
		private static readonly Dictionary<string, string> NumberingStylesCollection = new Dictionary<string, string>();

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
			var mediatorstyleSheet = FontHeightAdjuster.StyleSheetFromMediator(mediator);
			var cache = (FdoCache)mediator.PropertyTable.GetValue("cache");
			LoadBulletUnicodes();
			LoadNumberingStyles();
			GenerateCssForDefaultStyles(mediator, mediatorstyleSheet, styleSheet, model, cache);
			MakeLinksLookLikePlainText(styleSheet);
			GenerateCssForAudioWs(styleSheet, cache);
			foreach(var configNode in model.Parts.Where(x => x.IsEnabled).Concat(model.SharedItems.Where(x => x.Parent != null)))
			{
				GenerateCssFromConfigurationNode(configNode, styleSheet, null, mediator);
			}
			// Pretty-print the stylesheet
			return styleSheet.ToString(true, 1);
		}

		private static void GenerateCssForDefaultStyles(Mediator mediator, FwStyleSheet mediatorstyleSheet,
			StyleSheet styleSheet, DictionaryConfigurationModel model, FdoCache cache)
		{
			if (mediatorstyleSheet == null) return;
			if (mediatorstyleSheet.Styles.Contains("Normal"))
			{
				GenerateCssForWsSpanWithNormalStyle(styleSheet, mediator, cache);
			}
			if (mediatorstyleSheet.Styles.Contains(DictionaryNormal))
			{
				GenerateDictionaryNormalParagraphCss(styleSheet, mediator);
			}
			if (mediatorstyleSheet.Styles.Contains(DictionaryMinor))
			{
				GenerateDictionaryMinorParagraphCss(styleSheet, mediator, model);
			}
		}

		private static void MakeLinksLookLikePlainText(StyleSheet styleSheet)
		{
			var rule = new StyleRule { Value = "a" };
			rule.Declarations.Properties.AddRange(new [] {
				new Property("text-decoration") { Term = new PrimitiveTerm(UnitType.Attribute, "inherit") },
				new Property("color") { Term = new PrimitiveTerm(UnitType.Attribute, "inherit") }
			});
			styleSheet.Rules.Add(rule);
		}

		private static void GenerateCssForWsSpanWithNormalStyle(StyleSheet styleSheet, Mediator mediator, FdoCache cache)
		{
			// Generate the rules for the programmatic default style info (
			var defaultStyleProps = GetOnlyCharacterStyle(GenerateCssStyleFromFwStyleSheet("Normal", DefaultStyle, mediator));
			if (!defaultStyleProps.Any(p => p.Name == "font-size"))
			{
				defaultStyleProps.Add(new Property("font-size") { Term = new PrimitiveTerm(UnitType.Point, FontInfo.kDefaultFontSize) });
			}
			var defaultRule = new StyleRule { Value = "body" };
			defaultRule.Declarations.Properties.AddRange(defaultStyleProps);
			styleSheet.Rules.Add(defaultRule);
			// Then generate the rules for all the writing system overrides
			GenerateCssForWritingSystems("span", "Normal", styleSheet, mediator);
		}

		private static void GenerateDictionaryNormalParagraphCss(StyleSheet styleSheet, Mediator mediator)
		{
			var dictNormalRule = new StyleRule { Value = "div.entry" };
			var dictNormalStyle = GenerateCssStyleFromFwStyleSheet(DictionaryNormal, 0, mediator);
			dictNormalRule.Declarations.Properties.AddRange(GetOnlyParagraphStyle(dictNormalStyle));
			styleSheet.Rules.Add(dictNormalRule);
			// Then generate the rules for all the writing system overrides
			GenerateCssForWritingSystems("div.entry span", DictionaryNormal, styleSheet, mediator);
		}

		private static void GenerateDictionaryMinorParagraphCss(StyleSheet styleSheet, Mediator mediator, DictionaryConfigurationModel model)
		{
			// Use the style set in all the parts following main entry, if no style is specified assume Dictionary-Minor
			for (var i = 1; i < model.Parts.Count; ++i)
			{
				var minorEntryNode = model.Parts[i];
				if (minorEntryNode.IsEnabled)
				{
					var styleName = minorEntryNode.Style;
					if (string.IsNullOrEmpty(styleName))
						styleName = DictionaryMinor;
					var dictionaryMinorStyle = GenerateCssStyleFromFwStyleSheet(styleName, 0, mediator);
					var minorRule = new StyleRule { Value = string.Format("div.{0}", GetClassAttributeForConfig(minorEntryNode)) };
					minorRule.Declarations.Properties.AddRange(GetOnlyParagraphStyle(dictionaryMinorStyle));
					styleSheet.Rules.Add(minorRule);
					// Then generate the rules for all the writing system overrides
					GenerateCssForWritingSystems(string.Format("div.{0} span", GetClassAttributeForConfig(minorEntryNode)), DictionaryMinor, styleSheet, mediator);
				}
			}
		}

		private static void GenerateCssForWritingSystems(string selector, string styleName, StyleSheet styleSheet, Mediator mediator)
		{
			var cache = (FdoCache)mediator.PropertyTable.GetValue("cache");
			// Generate the rules for all the writing system overrides
			foreach (var aws in cache.ServiceLocator.WritingSystems.AllWritingSystems)
			{
				// We want only the character type settings from the styleName style since we're applying them
				// to a span.
				var wsRule = new StyleRule { Value = selector + String.Format("[lang|=\"{0}\"]", aws.RFC5646) };
				var styleDecls = GenerateCssStyleFromFwStyleSheet(styleName, aws.Handle, mediator);
				wsRule.Declarations.Properties.AddRange(GetOnlyCharacterStyle(styleDecls));
				styleSheet.Rules.Add(wsRule);
			}
		}

		private static void GenerateCssForAudioWs(StyleSheet styleSheet, FdoCache cache)
		{
			foreach (var aws in cache.ServiceLocator.WritingSystems.AllWritingSystems)
			{
				if (aws.RFC5646.Contains("audio"))
				{
					var wsaudioRule = new StyleRule {Value = String.Format("a.{0}:after", aws.RFC5646)};
					wsaudioRule.Declarations.Properties.Add(new Property("content")
					{
						Term = new PrimitiveTerm(UnitType.String, "\uD83D\uDD0A")
					});
					styleSheet.Rules.Add(wsaudioRule);
					wsaudioRule = new StyleRule {Value = String.Format("a.{0}", aws.RFC5646)};
					wsaudioRule.Declarations.Properties.Add(new Property("text-decoration")
					{
						Term = new PrimitiveTerm(UnitType.Attribute, "none")
					});
					styleSheet.Rules.Add(wsaudioRule);
				}
			}
		}

		/// <summary>
		/// Generates css rules for a configuration node and adds them to the given stylesheet (recursive).
		/// </summary>
		private static void GenerateCssFromConfigurationNode(ConfigurableDictionaryNode configNode,
																			  StyleSheet styleSheet,
																			  string baseSelection,
																			  Mediator mediator)
		{
			var cache = (FdoCache)mediator.PropertyTable.GetValue("cache");
			var rule = new StyleRule();
			var senseOptions = configNode.DictionaryNodeOptions as DictionaryNodeSenseOptions;
			var listAndParaOpts = configNode.DictionaryNodeOptions as DictionaryNodeListAndParaOptions;
			if (senseOptions != null)
			{
				// Try to generate the css for the sense number before the baseSelection is updated because
				// the sense number is a sibling of the sense element and we are normally applying styles to the
				// children of collections. Also set display:block on span
				GenerateCssForSenses(configNode, senseOptions, styleSheet, ref baseSelection, mediator);
			}
			else if (listAndParaOpts != null)
			{
				GenerateCssFromListAndParaOptions(configNode, listAndParaOpts, styleSheet, ref baseSelection, cache, mediator);
			}
			else if (configNode.DictionaryNodeOptions is DictionaryNodeGroupingOptions
					&& ((DictionaryNodeGroupingOptions)configNode.DictionaryNodeOptions).DisplayGroupInParagraph)
			{
				// In a grouping node with DisplayGroupInParagraph on we should add the block display
				GenerateSelectorsFromNode(baseSelection, configNode, out baseSelection, cache, mediator);
				rule.Value = baseSelection;
				rule.Declarations.Add(new Property("display"){ Term = new PrimitiveTerm(UnitType.Ident, "block") });
				// if the configuration node defines a style then add all the rules generated from that style
				if (!String.IsNullOrEmpty(configNode.Style))
				{
					//Generate the rules for the paragraph style
					rule.Declarations.Properties.AddRange(GetOnlyParagraphStyle(GenerateCssStyleFromFwStyleSheet(configNode.Style, DefaultStyle, configNode,
						mediator)));
				}
				styleSheet.Rules.Add(rule);
			}
			else
			{
				if (configNode.DictionaryNodeOptions is DictionaryNodePictureOptions)
				{
					GenerateCssFromPictureOptions(configNode, (DictionaryNodePictureOptions)configNode.DictionaryNodeOptions, styleSheet, baseSelection);
				}
				var selectors = GenerateSelectorsFromNode(baseSelection, configNode, out baseSelection,
					cache, mediator);
				rule.Value = baseSelection;

				// if the configuration node defines a style then add all the rules generated from that style
				if (!String.IsNullOrEmpty(configNode.Style))
				{
					//Generate the rules for the default font info
					rule.Declarations.Properties.AddRange(GenerateCssStyleFromFwStyleSheet(configNode.Style, DefaultStyle, configNode,
						mediator));
				}
				var wsOptions = configNode.DictionaryNodeOptions as DictionaryNodeWritingSystemOptions;
				if (wsOptions != null)
				{
					GenerateCssFromWsOptions(configNode, wsOptions, styleSheet, baseSelection, mediator);
					if (wsOptions.DisplayWritingSystemAbbreviations)
					{
						GenerateCssForWritingSystemPrefix(configNode, styleSheet, baseSelection, mediator);
					}
				}
				styleSheet.Rules.AddRange(CheckRangeOfRulesForEmpties(selectors));
				if (!IsEmptyRule(rule))
					styleSheet.Rules.Add(rule);
			}
			if(configNode.Children == null)
				return;
			//Recurse into each child
			foreach(var child in configNode.Children.Where(x => x.IsEnabled))
			{
				GenerateCssFromConfigurationNode(child, styleSheet, baseSelection, mediator);
			}
		}

		private static bool IsEmptyRule(StyleRule rule)
		{
			return rule.Declarations.All(decl => string.IsNullOrWhiteSpace(decl.ToString()));
		}

		private static IEnumerable<StyleRule> CheckRangeOfRulesForEmpties(IEnumerable<StyleRule> rules)
		{
			return rules.Where(rule => !IsEmptyRule(rule));
		}

		private static bool IsBeforeOrAfter(StyleRule rule)
		{
			var sel = rule.Selector.ToString();
			return sel.EndsWith(":before") || sel.EndsWith(":after");
		}

		private static IEnumerable<StyleRule> RemoveBeforeAfterSelectorRules(IEnumerable<StyleRule> rules)
		{
			return rules.Where(rule => !IsBeforeOrAfter(rule));
		}

		private static void GenerateCssForSenses(ConfigurableDictionaryNode configNode, DictionaryNodeSenseOptions senseOptions,
														StyleSheet styleSheet, ref string baseSelection, Mediator mediator)
		{
			var selectors = GenerateSelectorsFromNode(baseSelection, configNode, out baseSelection, (FdoCache)mediator.PropertyTable.GetValue("cache"), mediator);
			// Insert '> .sensecontent' between '.*senses' and '.*sense' (where * could be 'referring', 'sub', or similar)
			var senseContentSelector = string.Format("{0}> .sensecontent", baseSelection.Substring(0, baseSelection.LastIndexOf('.')));
			var senseItemName = baseSelection.Substring(baseSelection.LastIndexOf('.'));
			if (senseOptions.DisplayEachSenseInAParagraph)
				selectors = RemoveBeforeAfterSelectorRules(selectors);
			styleSheet.Rules.AddRange(CheckRangeOfRulesForEmpties(selectors));
			var senseNumberRule = new StyleRule();
			// Not using SelectClassName here; sense and sensenumber are siblings and the configNode is for the Senses collection.
			// Select the base plus the node's unmodified class attribute and append the sensenumber matcher.
			var senseNumberSelector = string.Format("{0} .sensenumber", senseContentSelector);

			senseNumberRule.Value = senseNumberSelector;
			if(!String.IsNullOrEmpty(senseOptions.NumberStyle))
			{
				senseNumberRule.Declarations.Properties.AddRange(GenerateCssStyleFromFwStyleSheet(senseOptions.NumberStyle, DefaultStyle, mediator));
			}
			if (!IsEmptyRule(senseNumberRule))
				styleSheet.Rules.Add(senseNumberRule);
			if(!String.IsNullOrEmpty(senseOptions.BeforeNumber))
			{
				var beforeDeclaration = new StyleDeclaration
				{
					new Property("content") { Term = new PrimitiveTerm(UnitType.String, senseOptions.BeforeNumber) }
				};
				styleSheet.Rules.Add(new StyleRule(beforeDeclaration) { Value = senseNumberSelector + ":before" });
			}
			if(!String.IsNullOrEmpty(senseOptions.AfterNumber))
			{
				var afterDeclaration = new StyleDeclaration();
				afterDeclaration.Add(new Property("content") { Term = new PrimitiveTerm(UnitType.String, senseOptions.AfterNumber) });
				var afterRule = new StyleRule(afterDeclaration) { Value = senseNumberSelector + ":after" };
				styleSheet.Rules.Add(afterRule);
			}
			// set the base selection to the sense level under the sense content
			baseSelection = string.Format("{0} > {1}", senseContentSelector, senseItemName);
			var styleDeclaration = string.IsNullOrEmpty(configNode.Style) ? new StyleDeclaration() : GenerateCssStyleFromFwStyleSheet(configNode.Style, 0, configNode, mediator);
			if (senseOptions.DisplayEachSenseInAParagraph)
			{
				var sensCharDeclaration = GetOnlyCharacterStyle(styleDeclaration);
				var senseCharRule = new StyleRule(sensCharDeclaration)
				{
					// Apply the style with paragraph info removed to the first sense
					Value = baseSelection
				};
				if (!IsEmptyRule(senseCharRule))
					styleSheet.Rules.Add(senseCharRule);

				var senseParaDeclaration = GetOnlyParagraphStyle(styleDeclaration);
				senseParaDeclaration.Add(new Property("display")
				{
					Term = new PrimitiveTerm(UnitType.Ident, "block")
				});
				var senseParaRule = new StyleRule(senseParaDeclaration)
				{
					// Apply the paragraph style information to all but the first sensecontent block, if requested
					Value = senseOptions.DisplayFirstSenseInline ? string.Format("{0} + {1}", senseContentSelector, ".sensecontent") : senseContentSelector
				};

				styleSheet.Rules.Add(senseParaRule);
				GenerateCssforBulletedList(configNode, styleSheet, senseParaRule.Value, mediator, styleDeclaration);
			}
			else
			{
				// Generate the style information specifically for senses
				var senseContentRule = new StyleRule(GetOnlyCharacterStyle(styleDeclaration))
				{
					Value = baseSelection
				};
				if (!IsEmptyRule(senseContentRule))
					styleSheet.Rules.Add(senseContentRule);
			}

			if (senseOptions.ShowSharedGrammarInfoFirst)
			{
				var collectionSelector = senseContentSelector.Substring(0, senseContentSelector.LastIndexOf(" .", StringComparison.Ordinal));
				foreach (var gramInfoNode in configNode.Children.Where(node => node.FieldDescription == "MorphoSyntaxAnalysisRA" && node.IsEnabled))
				{
					GenerateCssFromConfigurationNode(gramInfoNode, styleSheet, collectionSelector + " .sharedgrammaticalinfo", mediator);
				}
			}
		}

		/// <summary>
		/// Generates Bulleted List style properties
		/// </summary>
		/// <param name="configNode">Dictionary Node</param>
		/// <param name="styleSheet">Stylesheet to add the new rule</param>
		/// <param name="bulletSelector">Style name for the bullet property</param>
		/// <param name="mediator">mediator to get the styles</param>
		/// <param name="styleDeclaration">Style properties collection</param>
		private static void GenerateCssforBulletedList(ConfigurableDictionaryNode configNode, StyleSheet styleSheet, string bulletSelector, Mediator mediator, StyleDeclaration styleDeclaration)
		{
			if (configNode.Style != null)
			{
				if (styleDeclaration.Properties.Count == 0)
					styleDeclaration = GenerateCssStyleFromFwStyleSheet(configNode.Style, DefaultStyle, mediator);
				GenerateCssForCounterReset(styleSheet, bulletSelector, styleDeclaration, false);
				var senseOptions = configNode.DictionaryNodeOptions as DictionaryNodeSenseOptions;
				var senseSufixRule = senseOptions != null && senseOptions.DisplayFirstSenseInline ? ":not(:first-child):before" : ":before";
				var bulletRule = new StyleRule { Value = bulletSelector + senseSufixRule };
				bulletRule.Declarations.Properties.AddRange(GetOnlyBulletContent(styleDeclaration));
				var projectStyles = FontHeightAdjuster.StyleSheetFromMediator(mediator);
				BaseStyleInfo projectStyle = projectStyles.Styles[configNode.Style];
				var exportStyleInfo = new ExportStyleInfo(projectStyle);
				if (exportStyleInfo.NumberScheme != 0)
				{
					var wsFontInfo = exportStyleInfo.BulletInfo.FontInfo;
					bulletRule.Declarations.Add(new Property("font-size") { Term = new PrimitiveTerm(UnitType.Point, MilliPtToPt(wsFontInfo.FontSize.Value)) });
					bulletRule.Declarations.Add(new Property("color") { Term = new PrimitiveTerm(UnitType.RGB, wsFontInfo.FontColor.Value.Name) });
				}
				if (!IsEmptyRule(bulletRule))
				{
					styleSheet.Rules.Add(bulletRule);
				}
			}
		}

		private static void GenerateCssFromListAndParaOptions(ConfigurableDictionaryNode configNode,
			DictionaryNodeListAndParaOptions listAndParaOpts, StyleSheet styleSheet, ref string baseSelection, FdoCache cache, Mediator mediator)
		{
			var blockDeclarations = string.IsNullOrEmpty(configNode.Style)
				? new List<StyleDeclaration> { new StyleDeclaration() }
				: GenerateCssStyleFromFwStyleSheet(configNode.Style, 0, configNode, mediator, true);
			var selectors = GenerateSelectorsFromNode(baseSelection, configNode, out baseSelection, cache, mediator);
			var styleRules = selectors as StyleRule[] ?? selectors.ToArray();
			if (listAndParaOpts.DisplayEachInAParagraph)
			{
				for (var i = 0; i < blockDeclarations.Count; ++i)
				{
					var declaration = blockDeclarations[i];
					declaration.Add(new Property("display") { Term = new PrimitiveTerm(UnitType.Ident, "block") });
					var blockRule = new StyleRule(declaration)
					{
						Value = baseSelection
					};
					styleSheet.Rules.Add(blockRule);
					GenerateCssForCounterReset(styleSheet, baseSelection, declaration, true);
					var bulletRule = AdjustRuleIfParagraphNumberScheme(blockRule, configNode, mediator);
					styleSheet.Rules.AddRange(RemoveBeforeAfterSelectorRules(styleRules));
					styleSheet.Rules.Add(bulletRule);
				}
			}
			else
			{
				foreach (var declaration in blockDeclarations)
				{
					// Generate the style information specifically for ComplexFormsOptions
					var complexContentRule = new StyleRule(GetOnlyCharacterStyle(declaration))
					{
						Value = baseSelection
					};
					if (!IsEmptyRule(complexContentRule))
						styleSheet.Rules.Add(complexContentRule);
					styleSheet.Rules.AddRange(styleRules);
				}
			}
		}

		/// <summary>
		/// Generates Counter reset style properties
		/// </summary>
		/// <param name="styleSheet">Stylesheet to add the new rule</param>
		/// <param name="baseSelection">Style name for the bullet property</param>
		/// <param name="declaration">Style properties collection</param>
		/// <param name="isSplitBySpace">Split baseSelection by space/greater than</param>
		private static void GenerateCssForCounterReset(StyleSheet styleSheet, string baseSelection, StyleDeclaration declaration, bool isSplitBySpace)
		{
			var resetSection = GetOnlyCounterResetContent(declaration);
			if (!string.IsNullOrEmpty(resetSection))
			{
				string bulletParentSelector = baseSelection.Substring(0, baseSelection.LastIndexOf('>') - 1);
				if (isSplitBySpace)
					bulletParentSelector = baseSelection.Substring(0, baseSelection.LastIndexOf(' '));
				var resetRule = new StyleRule {Value = bulletParentSelector};
				resetRule.Declarations.Add(new Property("counter-reset")
				{
					Term = new PrimitiveTerm(UnitType.Attribute, resetSection)
				});
				styleSheet.Rules.Add(resetRule);
			}
		}

		/// <summary>
		/// Return a :before rule if the given rule derives from a paragraph style with a number scheme (such as bulleted).
		/// Remove the content part of the given rule if it is present and also remove the properties that don't apply to a :before rule.
		/// </summary>
		/// <remarks>
		/// See https://jira.sil.org/browse/LT-11625 for justification.
		/// </remarks>
		private static StyleRule AdjustRuleIfParagraphNumberScheme(StyleRule rule, ConfigurableDictionaryNode configNode, Mediator mediator)
		{
			if (!string.IsNullOrEmpty(configNode.Style))
			{
				var projectStyles = FontHeightAdjuster.StyleSheetFromMediator(mediator);
				BaseStyleInfo projectStyle = projectStyles.Styles[configNode.Style];
				var exportStyleInfo = new ExportStyleInfo(projectStyle);
				if (exportStyleInfo.NumberScheme != 0)
				{
					// Create a rule to add the bullet content before based off the given rule
					var bulletRule = new StyleRule { Value = rule.Value + ":before" };
					bulletRule.Declarations.Properties.AddRange(GetOnlyBulletContent(rule.Declarations));
					var wsFontInfo = exportStyleInfo.BulletInfo.FontInfo;
					bulletRule.Declarations.Add(new Property("font-size") { Term = new PrimitiveTerm(UnitType.Point, MilliPtToPt(wsFontInfo.FontSize.Value)) });
					bulletRule.Declarations.Add(new Property("color") { Term = new PrimitiveTerm(UnitType.RGB, wsFontInfo.FontColor.Value.Name) });
					// remove the bullet content if present in the base rule
					var contentInRule = rule.Declarations.FirstOrDefault(p => p.Name == "content");
					if (contentInRule != null)
						rule.Declarations.Remove(contentInRule);
					// remove the bullet counter-increment if present in the base rule
					var counterIncrement = rule.Declarations.FirstOrDefault(p => p.Name == "counter-increment");
					if (counterIncrement != null)
						rule.Declarations.Remove(counterIncrement);
					return bulletRule;
				}
			}
			return rule;
		}

		private static void GenerateCssFromWsOptions(ConfigurableDictionaryNode configNode, DictionaryNodeWritingSystemOptions wsOptions,
																	StyleSheet styleSheet, string baseSelection, Mediator mediator)
		{
			var cache = (FdoCache)mediator.PropertyTable.GetValue("cache");
			foreach(var ws in wsOptions.Options.Where(opt => opt.IsEnabled))
			{
				var possiblyMagic = WritingSystemServices.GetMagicWsIdFromName(ws.Id);
				// if the writing system isn't a magic name just use it otherwise find the right one from the magic list
				var wsIdString = possiblyMagic == 0 ? ws.Id : WritingSystemServices.GetWritingSystemList(cache, possiblyMagic, true).First().Id;
				var wsId = cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr(wsIdString);
				var wsRule = new StyleRule {Value = baseSelection + String.Format("[lang|=\"{0}\"]", wsIdString)};
				if (!String.IsNullOrEmpty(configNode.Style))
					wsRule.Declarations.Properties.AddRange(GenerateCssStyleFromFwStyleSheet(configNode.Style, wsId, mediator));
				if (!IsEmptyRule(wsRule))
					styleSheet.Rules.Add(wsRule);
			}
		}

		private static void GenerateCssForWritingSystemPrefix(ConfigurableDictionaryNode configNode, StyleSheet styleSheet, string baseSelection, Mediator mediator)
		{
			var wsRule1 = new StyleRule { Value = string.Format("{0}.{1}", baseSelection, WritingSystemPrefix)};
			wsRule1.Declarations.Properties.AddRange(GetOnlyCharacterStyle(GenerateCssStyleFromFwStyleSheet(WritingSystemStyleName, 0, configNode, mediator)));
			styleSheet.Rules.Add(wsRule1);
			var wsRule2 = new StyleRule { Value = string.Format("{0}.{1}:after", baseSelection, WritingSystemPrefix) };
			wsRule2.Declarations.Properties.Add(new Property("content"){Term = new PrimitiveTerm(UnitType.String, " ")});
			styleSheet.Rules.Add(wsRule2);
		}

		private static void GenerateCssFromPictureOptions(ConfigurableDictionaryNode configNode, DictionaryNodePictureOptions pictureOptions,
			StyleSheet styleSheet, string baseSelection)
		{
			var pictureAndCaptionRule = new StyleRule();
			pictureAndCaptionRule.Value = baseSelection + " " + SelectClassName(configNode);

			var pictureProps = pictureAndCaptionRule.Declarations.Properties;
			pictureProps.Add(new Property("float") { Term = new PrimitiveTerm(UnitType.Ident, "right") });
			pictureProps.Add(new Property("text-align") { Term = new PrimitiveTerm(UnitType.Ident, "center") });
			var margin = new Property("margin");
			var marginValues = BuildTermList(TermList.TermSeparator.Space, new PrimitiveTerm(UnitType.Point, 0),
				new PrimitiveTerm(UnitType.Point, 0), new PrimitiveTerm(UnitType.Point, 4), new PrimitiveTerm(UnitType.Point, 4));
			margin.Term = marginValues;
			pictureProps.Add(margin);
			pictureProps.Add(new Property("padding") { Term = new PrimitiveTerm(UnitType.Point, 2) });
			pictureProps.Add(new Property("float")
			{
				Term = new PrimitiveTerm(UnitType.Ident, pictureOptions.PictureLocation.ToString().ToLowerInvariant())
			});
			styleSheet.Rules.Add(pictureAndCaptionRule);

			var pictureRule = new StyleRule();
			pictureRule.Value = pictureAndCaptionRule.Value + " img";
			if(pictureOptions.MinimumHeight > 0)
			{
				pictureRule.Declarations.Properties.Add(new Property("min-height")
				{
					Term = new PrimitiveTerm(UnitType.Inch, pictureOptions.MinimumHeight)
				});
			}
			if(pictureOptions.MaximumHeight > 0)
			{
				pictureRule.Declarations.Properties.Add(new Property("max-height")
				{
					Term = new PrimitiveTerm(UnitType.Inch, pictureOptions.MaximumHeight)
				});
			}
			if(pictureOptions.MinimumWidth > 0)
			{
				pictureRule.Declarations.Properties.Add(new Property("min-width")
				{
					Term = new PrimitiveTerm(UnitType.Inch, pictureOptions.MinimumWidth)
				});
			}
			if(pictureOptions.MaximumWidth > 0)
			{
				pictureRule.Declarations.Properties.Add(new Property("max-width")
				{
					Term = new PrimitiveTerm(UnitType.Inch, pictureOptions.MaximumWidth)
				});
			}
			if (!IsEmptyRule(pictureRule))
				styleSheet.Rules.Add(pictureRule);
		}

		/// <summary>
		/// This method will generate before and after rules if the configuration node requires them. It also generates the selector for the node
		/// </summary>
		private static IEnumerable<StyleRule> GenerateSelectorsFromNode(
			string parentSelector, ConfigurableDictionaryNode configNode,
			out string baseSelection, FdoCache cache, Mediator mediator)
		{
			var rules = new List<StyleRule>();
			var fwStyles = FontHeightAdjuster.StyleSheetFromMediator(mediator);
			// simpleSelector is used for nodes that use before and after.  Collection type nodes produce wrong
			// results if we use baseSelection in handling before and after content.  See LT-17048.
			string simpleSelector;
			if(parentSelector == null)
			{
				baseSelection = SelectClassName(configNode);
				simpleSelector = SelectBareClassName(configNode);
				GenerateFlowResetForBaseNode(baseSelection, rules);
			}
			else
			{
				if(!String.IsNullOrEmpty(configNode.Between))
				{
					// content is generated before each item which follows an item of the same name
					// eg. .complexformrefs>.complexformref + .complexformref:before { content: "," }
					var dec = new StyleDeclaration();
					dec.Add(new Property("content") { Term = new PrimitiveTerm(UnitType.String, SpecialCharacterHandling.MakeSafeCss(configNode.Between)) });
					if (fwStyles != null && fwStyles.Styles.Contains(BeforeAfterBetweenStyleName))
						dec.Properties.AddRange(GenerateCssStyleFromFwStyleSheet(BeforeAfterBetweenStyleName, cache.DefaultAnalWs, mediator));
					var collectionSelector = "." + GetClassAttributeForConfig(configNode);
					var itemSelector = " ." + GetClassAttributeForCollectionItem(configNode);
					var betweenSelector = String.Format("{0} {1}>{2}+{2}:before", parentSelector, collectionSelector, itemSelector);
					var betweenRule = new StyleRule(dec) { Value = betweenSelector };
					if (configNode.DictionaryNodeOptions != null)
					{
						var wsOptions = configNode.DictionaryNodeOptions as DictionaryNodeWritingSystemOptions;
						var senseOptions = configNode.DictionaryNodeOptions as DictionaryNodeSenseOptions;
						// If wsOptions are enabled generate a between rule which will not put content between the abbreviation and the ws data
						if (wsOptions != null)
						{
							if (wsOptions.DisplayWritingSystemAbbreviations)
							{
								betweenSelector = String.Format("{0}> {1}> span.{2} ~ span.{2}:before", parentSelector, collectionSelector,
									WritingSystemPrefix);
							}
							else
							{
								var enabledWsOptions = wsOptions.Options.Where(x => x.IsEnabled).ToArray();
								//Fix LT-17238: Between rule added as before rule to ws span which iterates from last ws to second ws span
								//First Ws is skipped as between rules no longer needed before first WS span
								for (var i = enabledWsOptions.Count() - 1; i > 0; i--)
								{
									betweenSelector = (i == enabledWsOptions.Count() - 1 ? string.Empty : (betweenSelector + ",")) +
									String.Format("{0}> {1}> span[lang|='{2}']:before", parentSelector, collectionSelector,
									enabledWsOptions[i].Id);
								}
							}
						}
						else if (senseOptions != null && senseOptions.ShowSharedGrammarInfoFirst)
							betweenSelector = String.Format("{0}> {1}>{2}.sensecontent+{2}:before", parentSelector, collectionSelector, " span");
						else if (configNode.FieldDescription == "PicturesOfSenses")
							betweenSelector = String.Format("{0}> {1}>{2}+{2}:before", parentSelector, collectionSelector, " div");
						else
							betweenSelector = String.Format("{0}> {1}>{2}+{2}:before", parentSelector, collectionSelector, " span");

						betweenRule = new StyleRule(dec) { Value = betweenSelector };
					}
					rules.Add(betweenRule);
				}
				// Headword, Gloss, and Caption are contained in a captionContent area.
				if (configNode.Parent.DictionaryNodeOptions is DictionaryNodePictureOptions)
				{
					baseSelection = parentSelector + "> " + ".captionContent " + SelectClassName(configNode, cache);
					simpleSelector = parentSelector + "> " + ".captionContent " + SelectBareClassName(configNode, cache);
				}
				else
				{
					baseSelection = parentSelector + "> " + SelectClassName(configNode, cache);
					simpleSelector = parentSelector + "> " + SelectBareClassName(configNode, cache);
				}
			}
			if(!String.IsNullOrEmpty(configNode.Before))
			{
				var dec = new StyleDeclaration();
				dec.Add(new Property("content") { Term = new PrimitiveTerm(UnitType.String, SpecialCharacterHandling.MakeSafeCss(configNode.Before)) });
				if (fwStyles != null && fwStyles.Styles.Contains(BeforeAfterBetweenStyleName))
					dec.Properties.AddRange(GenerateCssStyleFromFwStyleSheet(BeforeAfterBetweenStyleName, cache.DefaultAnalWs, mediator));
				var selectorBase = simpleSelector;
				if (configNode.FieldDescription == "PicturesOfSenses")
					selectorBase += "> div:first-child";
				var beforeRule = new StyleRule(dec) { Value = GetBaseSelectionWithSelectors(selectorBase, ":before") };
				rules.Add(beforeRule);
			}
			if(!String.IsNullOrEmpty(configNode.After))
			{
				var dec = new StyleDeclaration();
				dec.Add(new Property("content") { Term = new PrimitiveTerm(UnitType.String, SpecialCharacterHandling.MakeSafeCss(configNode.After)) });
				if (fwStyles != null && fwStyles.Styles.Contains(BeforeAfterBetweenStyleName))
					dec.Properties.AddRange(GenerateCssStyleFromFwStyleSheet(BeforeAfterBetweenStyleName, cache.DefaultAnalWs, mediator));
				var selectorBase = simpleSelector;
				if (configNode.FieldDescription == "PicturesOfSenses")
					selectorBase += "> div:last-child";
				var afterRule = new StyleRule(dec) { Value = GetBaseSelectionWithSelectors(selectorBase, ":after") };
				rules.Add(afterRule);
			}
			return rules;
		}

		/// <summary>
		/// Method to create matching selector based on baseSelection
		/// If baseSelection ends with span, first-child/last-child will add before the before/after selector
		/// </summary>
		/// <param name="baseSelection">baseselector value</param>
		/// <param name="selector">Before/After selector</param>
		/// <returns></returns>
		private static string GetBaseSelectionWithSelectors(string baseSelection, string selector)
		{
			string baseSelectionValue = baseSelection;
			if (baseSelection.LastIndexOf("span", StringComparison.Ordinal) != baseSelection.Length - 4)
				return baseSelectionValue + selector;
			string firstOrLastChild = selector == ":before" ? ":first-child" : ":last-child";
			baseSelectionValue = baseSelectionValue + firstOrLastChild + selector;
			return baseSelectionValue;
		}

		private static void GenerateFlowResetForBaseNode(string baseSelection, List<StyleRule> rules)
		{
			var flowResetRule = new StyleRule();
			flowResetRule.Value = baseSelection;
			flowResetRule.Declarations.Properties.Add(new Property("clear") { Term = new PrimitiveTerm(UnitType.Ident, "both")});
			flowResetRule.Declarations.Properties.Add(new Property("white-space") { Term = new PrimitiveTerm(UnitType.Ident, "pre-wrap") });
			rules.Add(flowResetRule);
		}
		/// <summary>
		/// Generates a selector for a class name that matches xhtml that is generated for the configNode.
		/// e.g. '.entry' or '.sense'
		/// </summary>
		/// <param name="configNode"></param>
		/// <param name="cache">defaults to null, necessary for generating correct css for custom field nodes</param>
		/// <returns></returns>
		private static string SelectClassName(ConfigurableDictionaryNode configNode, FdoCache cache = null)
		{
			var type = ConfiguredXHTMLGenerator.GetPropertyTypeForConfigurationNode(configNode, cache);
			return SelectClassName(configNode, type);
		}

		private static string SelectClassName(ConfigurableDictionaryNode configNode, ConfiguredXHTMLGenerator.PropertyType type)
		{
			switch(type)
			{
				case ConfiguredXHTMLGenerator.PropertyType.CollectionType:
				{
					// for collections we generate a css selector to match each item e.g '.senses .sense'
					return string.Format(".{0} .{1}", GetClassAttributeForConfig(configNode), GetClassAttributeForCollectionItem(configNode));
				}
				case ConfiguredXHTMLGenerator.PropertyType.CmPictureType:
				{
					return " img"; // Pictures are written out as img tags
				}
				case ConfiguredXHTMLGenerator.PropertyType.PrimitiveType:
				case ConfiguredXHTMLGenerator.PropertyType.MoFormType:
				{
					// for multi-lingual strings each language's string will have the contents generated in a span
					if(configNode.DictionaryNodeOptions is DictionaryNodeWritingSystemOptions)
					{
						return "." + GetClassAttributeForConfig(configNode) + "> span";
					}
					goto default;
				}
				default:
					return "." + GetClassAttributeForConfig(configNode);
			}
		}

		/// <summary>
		/// Generate the singular of the collection name: drop the final character ('s') or
		/// handle "entries" => "entry" or "analyses" to "analysis" or "glosses" to "gloss"
		/// </summary>
		internal static string GetClassAttributeForCollectionItem(ConfigurableDictionaryNode configNode)
		{
			var classNameBase = GetClassAttributeBase(configNode).ToLower();
			string singularBase;
			if(classNameBase.EndsWith("ies"))
				singularBase = classNameBase.Remove(classNameBase.Length - 3) + "y";
			else if (classNameBase.EndsWith("analyses"))
				singularBase = classNameBase.Remove(classNameBase.Length - 2) + "is";
			else if (classNameBase.EndsWith("sses"))
				singularBase = classNameBase.Remove(classNameBase.Length - 2);
			else
				singularBase = classNameBase.Remove(classNameBase.Length - 1);
			return singularBase + GetClassAttributeDupSuffix(configNode).ToLower();
		}

		/// <summary>
		/// For collection type nodes, generates a selector on the collection as a whole.  For all other nodes,
		/// calls SelectClassName to generate the selector.
		/// </summary>
		/// <remarks>
		/// Perhaps SelectClassName should have been changed, but that's a rather far reaching change.  Using the
		/// output of this method for :before and :after rules in the css is sufficient to fix the bug reported in
		/// LT-17048.  A better name might be nice, but this one is fairly descriptive.
		/// </remarks>
		private static string SelectBareClassName(ConfigurableDictionaryNode configNode, FdoCache cache = null)
		{
			var type = ConfiguredXHTMLGenerator.GetPropertyTypeForConfigurationNode(configNode, cache);
			if (type == ConfiguredXHTMLGenerator.PropertyType.CollectionType)
				return "." + GetClassAttributeForConfig(configNode);
			return SelectClassName(configNode, type);
		}

		/// <summary>
		/// Generates a class name for the given configuration for use by Css and XHTML.
		/// Uses SubField and CSSClassNameOverride attributes where found
		/// </summary>
		internal static string GetClassAttributeForConfig(ConfigurableDictionaryNode configNode)
		{
			return (GetClassAttributeBase(configNode) + GetClassAttributeDupSuffix(configNode)).ToLower();
		}

		private static string GetClassAttributeBase(ConfigurableDictionaryNode configNode)
		{
			// use the FieldDescription as the class name, and append a '_' followed by the SubField if it is defined.
			// Note that custom fields can have spaces in their names, which CSS can't handle.  Convert spaces to hyphens,
			// which CSS allows but FieldWorks doesn't use (except maybe in custom fields).
			if (string.IsNullOrEmpty(configNode.CSSClassNameOverride))
			{
				var classAttribute = string.Empty;
				if (configNode.DictionaryNodeOptions is DictionaryNodeGroupingOptions)
				{
					classAttribute += "grouping_";
				}
				classAttribute += configNode.FieldDescription.Replace(' ', '-') +
					(string.IsNullOrEmpty(configNode.SubField) ? "" : "_" + configNode.SubField);
				return classAttribute;
			}
			return configNode.CSSClassNameOverride;
		}

		private static string GetClassAttributeDupSuffix(ConfigurableDictionaryNode configNode)
		{
			return configNode.IsDuplicate
				? "_" + (configNode.LabelSuffix = Regex.Replace(configNode.LabelSuffix, "[^a-zA-Z0-9+]", "-"))
				: string.Empty;
		}

		internal static StyleDeclaration GetOnlyCharacterStyle(StyleDeclaration fullStyleDeclaration)
		{
			var declaration = new StyleDeclaration();
			foreach(var prop in fullStyleDeclaration.Where(prop => prop.Name.Contains("font") || prop.Name.Contains("color")))
			{
				declaration.Add(prop);
			}
			return declaration;
		}

		internal static StyleDeclaration GetOnlyParagraphStyle(StyleDeclaration fullStyleDeclaration)
		{
			var declaration = new StyleDeclaration();
			foreach(var prop in fullStyleDeclaration.Where(prop => !prop.Name.Contains("font") && !prop.Name.Contains("color") && !prop.Name.Contains("content") && !prop.Name.Contains("counter-increment")))
			{
				declaration.Add(prop);
			}
			return declaration;
		}

		internal static StyleDeclaration GetOnlyBulletContent(StyleDeclaration fullStyleDeclaration)
		{
			var declaration = new StyleDeclaration();
			foreach (var prop in fullStyleDeclaration.Where(prop => prop.Name.Contains("content") || prop.Name.Contains("counter-increment")))
			{
				declaration.Add(prop);
			}
			return declaration;
		}

		internal static string GetOnlyCounterResetContent(StyleDeclaration fullStyleDeclaration)
		{
			var counterProp = fullStyleDeclaration.FirstOrDefault(prop => prop.Name.Contains("counter-increment"));
			return counterProp != null ? counterProp.Term.ToString() : string.Empty;
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
			return GenerateCssStyleFromFwStyleSheet(styleName, wsId, null, mediator);
		}

		/// <summary>
		/// Generates a css StyleDeclaration for the requested FieldWorks style.
		/// <remarks>internal to facilitate separate unit testing.</remarks>
		/// </summary>
		/// <param name="styleName"></param>
		/// <param name="wsId">writing system id</param>
		/// <param name="node">The configuration node to use for generating paragraph margin in context</param>
		/// <param name="mediator"></param>
		/// <returns></returns>
		internal static StyleDeclaration GenerateCssStyleFromFwStyleSheet(string styleName, int wsId,
			ConfigurableDictionaryNode node, Mediator mediator)
		{
			return GenerateCssStyleFromFwStyleSheet(styleName, wsId, node, mediator, false)[0];
		}

		internal static List<StyleDeclaration> GenerateCssStyleFromFwStyleSheet(string styleName, int wsId, ConfigurableDictionaryNode node, Mediator mediator, bool calculateFirstSenseStyle)
		{
			var declaration = new StyleDeclaration();
			var styleSheet = FontHeightAdjuster.StyleSheetFromMediator(mediator);
			if(styleSheet == null || !styleSheet.Styles.Contains(styleName))
			{
				return new List<StyleDeclaration> {declaration};
			}
			var projectStyle = styleSheet.Styles[styleName];
			var exportStyleInfo = new ExportStyleInfo(projectStyle);
			var hangingIndent = 0.0f;

			// Tuple ancestorIndents used for ancestor components leadingIndent and hangingIndent.
			var ancestorIndents = new AncestorIndents(0.0f, 0.0f);
			if(exportStyleInfo.IsParagraphStyle && node != null)
				ancestorIndents = CalculateParagraphIndentsFromAncestors(node, styleSheet, ancestorIndents);

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
				// Handles both first-line and hanging indent, hanging-indent will result in a negative text-indent value
				var firstLineIndentValue = MilliPtToPt(exportStyleInfo.FirstLineIndent);

				if (firstLineIndentValue < 0.0f)
				{
					hangingIndent = firstLineIndentValue;
				}

				declaration.Add(new Property("text-indent") { Term = new PrimitiveTerm(UnitType.Point, firstLineIndentValue) } );
			}
			if(exportStyleInfo.HasKeepTogether)
			{
				throw new NotImplementedException("Keep Together style export not yet implemented.");
			}
			if(exportStyleInfo.HasKeepWithNext)
			{
				throw new NotImplementedException("Keep With Next style export not yet implemented.");
			}
			if(exportStyleInfo.HasLeadingIndent || hangingIndent < 0.0f || ancestorIndents.TextIndent < 0.0f)
			{
				var leadingIndent = CalculateMarginLeft(exportStyleInfo, ancestorIndents, hangingIndent);
				string marginDirection = "margin-left";
				if (exportStyleInfo.DirectionIsRightToLeft == TriStateBool.triTrue)
					marginDirection = "margin-right";
				declaration.Add(new Property(marginDirection) { Term = new PrimitiveTerm(UnitType.Point, leadingIndent) });
			}
			if(exportStyleInfo.HasLineSpacing)
			{
				var lineHeight = new Property("line-height");
				//m_relative means single, 1.5 or double line spacing was chosen. The CSS should be a number
				if(exportStyleInfo.LineSpacing.m_relative)
				{
					// The relative value is stored internally multiplied by 10000.  (FieldWorks code generally hates floating point.)
					// CSS expects to see the actual floating point value.  See https://jira.sil.org/browse/LT-16735.
					lineHeight.Term = new PrimitiveTerm(UnitType.Number, exportStyleInfo.LineSpacing.m_lineHeight / 10000.0F);
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
				string paddingDirection = "padding-right";
				if (exportStyleInfo.DirectionIsRightToLeft == TriStateBool.triTrue)
					paddingDirection = "padding-left";
				declaration.Add(new Property(paddingDirection) { Term = new PrimitiveTerm(UnitType.Point, MilliPtToPt(exportStyleInfo.TrailingIndent)) });
			}

			AddFontInfoCss(projectStyle, declaration, wsId, (FdoCache)mediator.PropertyTable.GetValue("cache"));

			if (exportStyleInfo.NumberScheme != 0)
			{
				var numScheme = exportStyleInfo.NumberScheme.ToString();
				if (BulletSymbolsCollection.ContainsKey(exportStyleInfo.NumberScheme.ToString()))
				{
					string selectedBullet = BulletSymbolsCollection[numScheme];
					declaration.Add(new Property("content") { Term = new PrimitiveTerm(UnitType.String, selectedBullet) });
				}
				else if (NumberingStylesCollection.ContainsKey(exportStyleInfo.NumberScheme.ToString()))
				{
					if (node != null)
					{
						string selectedNumStyle = NumberingStylesCollection[numScheme];
						declaration.Add(new Property("counter-increment") { Term = new PrimitiveTerm(UnitType.Attribute, " " + node.Label.ToLower()) });
						declaration.Add(new Property("content") { Term = new PrimitiveTerm(UnitType.Attribute, string.Format(" counter({0}, {1}) {2}", node.Label.ToLower(), selectedNumStyle, @"' '")) });
					}
				}
			}
			var styleList = new List<StyleDeclaration> {declaration};
			if (calculateFirstSenseStyle && ancestorIndents.Ancestor != null)
			{
				var senseOptions = ancestorIndents.Ancestor.DictionaryNodeOptions as DictionaryNodeSenseOptions;
				if (senseOptions != null && senseOptions.DisplayEachSenseInAParagraph)
				{
					ancestorIndents = CalculateParagraphIndentsFromAncestors(ancestorIndents.Ancestor, styleSheet, new AncestorIndents(0f, 0f));
					var marginLeft = CalculateMarginLeft(exportStyleInfo, ancestorIndents, hangingIndent);
					var firstSenseStyle = new StyleDeclaration();
					string marginDirection = "margin-left";
					if (exportStyleInfo.DirectionIsRightToLeft == TriStateBool.triTrue)
						marginDirection = "margin-right";
					firstSenseStyle.Properties.AddRange(declaration.Where(p => p.Name != marginDirection));
					firstSenseStyle.Properties.Add(new Property(marginDirection) { Term = new PrimitiveTerm(UnitType.Point, marginLeft) });
					styleList.Insert(0, firstSenseStyle);
				}
			}

			if (exportStyleInfo.DirectionIsRightToLeft != TriStateBool.triNotSet)
			{
				// REVIEW (Hasso) 2016.07: I think the only time this matters is when the user has paragraphs (senses, subentries, etc)
				// REVIEW (cont) whose directions oppose Dictionary-Normal. In this case, O Pesky Users, we will need to know which direction the
				// REVIEW (cont) paragraph is going when we generate the innermost strings. Implementing this will be pricy for paragraphy
				// REVIEW (cont) dictionaries, but beneficial for only our small bidirectional contingency. Alas, O Pesky Users.
				// REVIEW (cont) But we may need a CSS fix for bidirectionality until we can get GeckoFx 47+. O Fair Quill, Delicate Parchment.
				declaration.Add(new Property("direction") { Term = new PrimitiveTerm(UnitType.Ident, exportStyleInfo.DirectionIsRightToLeft == TriStateBool.triTrue ? "rtl" : "ltr") });
			}

			return styleList;
		}

		private static float CalculateMarginLeft(ExportStyleInfo exportStyleInfo, AncestorIndents ancestorIndents,
			float hangingIndent)
		{
			var leadingIndent = 0.0f;
			if (exportStyleInfo.HasLeadingIndent)
			{
				leadingIndent = MilliPtToPt(exportStyleInfo.LeadingIndent);
			}

			var ancestorMargin = ancestorIndents.Margin - ancestorIndents.TextIndent;
			leadingIndent -= ancestorMargin + hangingIndent;
			return leadingIndent;
		}

		private static AncestorIndents CalculateParagraphIndentsFromAncestors(ConfigurableDictionaryNode currentNode,
			FwStyleSheet styleSheet, AncestorIndents ancestorIndents)
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

		private static float GetHangingIndentIfAny(ExportStyleInfo exportStyleInfo)
		{
			// Handles both first-line and hanging indent: hanging indent represented as a negative first-line indent value
			return exportStyleInfo.HasFirstLineIndent && exportStyleInfo.FirstLineIndent < 0 ?
				MilliPtToPt(exportStyleInfo.FirstLineIndent) : 0.0f;
		}

		private static float GetLeadingIndent(ExportStyleInfo exportStyleInfo)
		{
			return exportStyleInfo.HasLeadingIndent ? MilliPtToPt(exportStyleInfo.LeadingIndent) : 0.0f;
		}

		private static bool IsParagraphStyle(ConfigurableDictionaryNode node, FwStyleSheet styleSheet)
		{
			if (node.StyleType == ConfigurableDictionaryNode.StyleTypes.Character)
				return false;
			var style = node.Style;
			return !string.IsNullOrEmpty(style) && styleSheet.Styles.Contains(style) && styleSheet.Styles[style].IsParagraphStyle;
		}

		/// <summary>
		/// Mapping the bullet symbols with the number system
		/// </summary>
		private static void LoadBulletUnicodes()
		{
			if (BulletSymbolsCollection.Count > 0)
				return;

			BulletSymbolsCollection.Add("kvbnBulletBase", "\\00B7");
			BulletSymbolsCollection.Add("101", "\\2022");
			BulletSymbolsCollection.Add("102", "\\25CF");
			BulletSymbolsCollection.Add("103", "\\274D");
			BulletSymbolsCollection.Add("104", "\\25AA");
			BulletSymbolsCollection.Add("105", "\\25A0");
			BulletSymbolsCollection.Add("106", "\\25AB");
			BulletSymbolsCollection.Add("107", "\\25A1");
			BulletSymbolsCollection.Add("108", "\\2751");
			BulletSymbolsCollection.Add("109", "\\2752");
			BulletSymbolsCollection.Add("110", "\\2B27");
			BulletSymbolsCollection.Add("111", "\\29EB");
			BulletSymbolsCollection.Add("112", "\\25C6");
			BulletSymbolsCollection.Add("113", "\\2756");
			BulletSymbolsCollection.Add("114", "\\2318");
			BulletSymbolsCollection.Add("115", "\\261E");
			BulletSymbolsCollection.Add("116", "\\271E");
			BulletSymbolsCollection.Add("117", "\\271E");
			BulletSymbolsCollection.Add("118", "\\2730");
			BulletSymbolsCollection.Add("119", "\\27A2");
			BulletSymbolsCollection.Add("120", "\\27B2");
			BulletSymbolsCollection.Add("121", "\\2794");
			BulletSymbolsCollection.Add("122", "\\2794");
			BulletSymbolsCollection.Add("123", "\\21E8");
			BulletSymbolsCollection.Add("124", "\\2713");
		}

		/// <summary>
		/// Mapping the numbering styles with the content's number format
		/// </summary>
		private static void LoadNumberingStyles()
		{
			if (NumberingStylesCollection.Count > 0)
				return;

			NumberingStylesCollection.Add("kvbnNumberBase", "decimal");
			NumberingStylesCollection.Add("kvbnArabic", "decimal");
			NumberingStylesCollection.Add("kvbnRomanLower", "lower-roman");
			NumberingStylesCollection.Add("kvbnRomanUpper", "upper-roman");
			NumberingStylesCollection.Add("kvbnLetterLower", "lower-alpha");
			NumberingStylesCollection.Add("kvbnLetterUpper", "upper-alpha");
			NumberingStylesCollection.Add("kvbnArabic01", "decimal-leading-zero");
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
		private static void AddFontInfoCss(BaseStyleInfo projectStyle, StyleDeclaration declaration, int wsId, FdoCache cache)
		{
			var wsFontInfo = projectStyle.FontInfoForWs(wsId);
			var defaultFontInfo = projectStyle.DefaultCharacterStyleInfo;

			// set fontName to the wsFontInfo publicly accessible InheritableStyleProp value if set, otherwise the
			// defaultFontInfo if set, or null.
			var fontName = wsFontInfo.m_fontName.ValueIsSet ? wsFontInfo.m_fontName.Value
				: defaultFontInfo.FontName.ValueIsSet ? defaultFontInfo.FontName.Value : null;

			// fontName still null means not set in Normal Style, then get default fonts from WritingSystems configuration.
			// Comparison, projectStyle.Name == "Normal", required to limit the font-family definition to the
			// empty span (ie span[lang|="en"]{}. If not included, font-family will be added to many more spans.
			if (fontName == null && projectStyle.Name == "Normal")
			{
				var lgWritingSysytem = cache.ServiceLocator.WritingSystemManager.get_EngineOrNull(wsId);
				if(lgWritingSysytem != null)
					fontName = lgWritingSysytem.DefaultFontName;
			}

			if (fontName != null)
			{
				var fontFamily = new Property("font-family");
				fontFamily.Term =
					new TermList(
						new PrimitiveTerm(UnitType.String, fontName),
						new PrimitiveTerm(UnitType.Ident, "serif"));
				declaration.Add(fontFamily);
			}

			// For the following additions, wsFontInfo is publicly accessible InheritableStyleProp value if set (ie. m_fontSize, m_bold, etc.),
			// checks for explicit overrides. Otherwise the defaultFontInfo if set (ie. FontSize, Bold, etc), or null.
			AddInfoFromWsOrDefaultValue(wsFontInfo.m_fontSize, defaultFontInfo.FontSize, "font-size", UnitType.Point, declaration);
			AddInfoFromWsOrDefaultValue(wsFontInfo.m_bold, defaultFontInfo.Bold, "font-weight", "bold", "normal", declaration);
			AddInfoFromWsOrDefaultValue(wsFontInfo.m_italic, defaultFontInfo.Italic, "font-style", "italic", "normal", declaration);
			AddInfoFromWsOrDefaultValue(wsFontInfo.m_fontColor, defaultFontInfo.FontColor, "color", declaration);
			AddInfoFromWsOrDefaultValue(wsFontInfo.m_backColor, defaultFontInfo.BackColor, "background-color", declaration);
			AddInfoFromWsOrDefaultValue(wsFontInfo.m_superSub, defaultFontInfo.SuperSub, declaration);

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
		private static void AddInfoFromWsOrDefaultValue(InheritableStyleProp<bool> wsFontInfo, IStyleProp<bool> defaultFontInfo,
														string propName, string trueValue, string falseValue,
														StyleDeclaration declaration)
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
		private static void AddInfoFromWsOrDefaultValue(InheritableStyleProp<Color> wsFontInfo, IStyleProp<Color> defaultFontInfo,
														string propName, StyleDeclaration declaration)
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
		private static void AddInfoFromWsOrDefaultValue(InheritableStyleProp<int> wsFontInfo, IStyleProp<int> defaultFontInfo,
														string propName, UnitType termType, StyleDeclaration declaration)
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
		/// <param name="declaration"></param>
		private static void AddInfoFromWsOrDefaultValue(InheritableStyleProp<FwSuperscriptVal> wsFontInfo,
														IStyleProp<FwSuperscriptVal> defaultFontInfo, StyleDeclaration declaration)
		{
			FwSuperscriptVal fontValue;
			if(!GetFontValue(wsFontInfo, defaultFontInfo, out fontValue))
				return;
			var sizeProp = new Property("font-size");
			sizeProp.Term = new PrimitiveTerm(UnitType.Ident, "58%"); //58% is what OpenOffice does
			declaration.Add(sizeProp);

			if (fontValue != FwSuperscriptVal.kssvOff)
			{
				var position = new Property("position");
				position.Term = new PrimitiveTerm(UnitType.Ident, "relative");
				var top = new Property("top");
				if (fontValue == FwSuperscriptVal.kssvSub)
					top.Term = new PrimitiveTerm(UnitType.Pixel, "0.3em");
				else
					top.Term = new PrimitiveTerm(UnitType.Pixel, "-0.6em");
				declaration.Add(position);
				declaration.Add(top);
			}
		}

		private static void AddInfoForUnderline(FontInfo wsFont, ICharacterStyleInfo defaultFont, StyleDeclaration declaration)
		{
			FwUnderlineType underlineType;
			if(!GetFontValue(wsFont.m_underline, defaultFont.Underline, out underlineType))
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

					// The wsFontInfo is publicly accessible InheritableStyleProp value if set, checks for explicit overrides.
					// Otherwise the defaultFontInfo if set, or null.
					AddInfoFromWsOrDefaultValue(wsFont.m_underlineColor, defaultFont.UnderlineColor, "border-bottom-color", declaration);
					goto case FwUnderlineType.kuntSingle; //fall through to single
				}
				case(FwUnderlineType.kuntSingle):
				{
					var fontProp = new Property("text-decoration");
					fontProp.Term = new PrimitiveTerm(UnitType.Ident, "underline");
					declaration.Add(fontProp);

					// The wsFontInfo is publicly accessible InheritableStyleProp value if set, checks for explicit overrides.
					// Otherwise the defaultFontInfo if set, or null.
					AddInfoFromWsOrDefaultValue(wsFont.m_underlineColor, defaultFont.UnderlineColor, "text-decoration-color", declaration);
					break;
				}
				case(FwUnderlineType.kuntStrikethrough):
				{
					var fontProp = new Property("text-decoration");
					fontProp.Term = new PrimitiveTerm(UnitType.Ident, "line-through");
					declaration.Add(fontProp);

					// The wsFontInfo is publicly accessible InheritableStyleProp value if set, checks for explicit overrides.
					// Otherwise the defaultFontInfo if set, or null.
					AddInfoFromWsOrDefaultValue(wsFont.m_underlineColor, defaultFont.UnderlineColor, "text-decoration-color", declaration);
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

					// The wsFontInfo is publicly accessible InheritableStyleProp value if set, checks for explicit overrides.
					// Otherwise the defaultFontInfo if set, or null.
					AddInfoFromWsOrDefaultValue(wsFont.m_underlineColor, defaultFont.UnderlineColor, "border-bottom-color", declaration);
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
		private static bool GetFontValue<T>(InheritableStyleProp<T> wsFontInfo, IStyleProp<T> defaultFontInfo,
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
			letHeadRule.Declarations.Properties.Add(new Property("-moz-column-count") { Term = new PrimitiveTerm(UnitType.Number, 1) });
			letHeadRule.Declarations.Properties.Add(new Property("-webkit-column-count") { Term = new PrimitiveTerm(UnitType.Number, 1) });
			letHeadRule.Declarations.Properties.Add(new Property("column-count") { Term = new PrimitiveTerm(UnitType.Number, 1) });
			letHeadRule.Declarations.Properties.Add(new Property("clear") { Term = new PrimitiveTerm(UnitType.Ident, "both") });
			letHeadRule.Declarations.Properties.Add(new Property("text-align") { Term = new PrimitiveTerm(UnitType.Ident, "center") });
			letHeadRule.Declarations.Properties.Add(new Property("width") { Term = new PrimitiveTerm(UnitType.Percentage, 100) });

			var letterRule = new StyleRule { Value = ".letter" };
			var cache = (FdoCache)mediator.PropertyTable.GetValue("cache");
			letterRule.Declarations.Properties.AddRange(GenerateCssStyleFromFwStyleSheet(LetterHeadingStyleName, cache.DefaultVernWs, mediator));
			return letHeadRule.ToString(true) + Environment.NewLine + letterRule.ToString(true) + Environment.NewLine;
		}

		public static string GenerateCssForPageButtons()
		{
			var screenPages = new StyleRule { Value = ".pages" };
			screenPages.Declarations.Properties.Add(new Property("display") { Term = new PrimitiveTerm(UnitType.Ident, "table") });
			screenPages.Declarations.Properties.Add(new Property("width") { Term = new PrimitiveTerm(UnitType.Percentage, 100) });
			var screen = new MediaRule { Condition = "screen", RuleSets = { screenPages } };
			var printPages = new StyleRule { Value = ".pages" };
			printPages.Declarations.Properties.Add(new Property("display") { Term = new PrimitiveTerm(UnitType.Ident, "none") });
			var print = new MediaRule { Condition = "print", RuleSets = { printPages } };
			var pageButtonHover = new StyleRule { Value = ".pagebutton:hover" };
			pageButtonHover.Declarations.Properties.Add(new Property("background") { Term = new PrimitiveTerm(UnitType.Grad, "linear-gradient(to bottom, #dfdfdf 5%, #ededed 100%)") });
			pageButtonHover.Declarations.Properties.Add(new Property("background-color") { Term = new PrimitiveTerm(UnitType.RGB, "#cdcdcd") });
			var pageButtonActive = new StyleRule { Value = ".pagebutton:active" };
			pageButtonActive.Declarations.Properties.Add(new Property("position") { Term = new PrimitiveTerm(UnitType.Ident, "relative") });
			pageButtonActive.Declarations.Properties.Add(new Property("top") { Term = new PrimitiveTerm(UnitType.Pixel, 1) });
			var pageButton = new StyleRule { Value = ".pagebutton" };
			pageButton.Declarations.Properties.Add(new Property("display") { Term = new PrimitiveTerm(UnitType.Ident, "table-cell") });
			pageButton.Declarations.Properties.Add(new Property("cursor") { Term = new PrimitiveTerm(UnitType.Ident, "pointer") });
			pageButton.Declarations.Properties.Add(new Property("color") { Term = new PrimitiveTerm(UnitType.RGB, "#777777") });
			pageButton.Declarations.Properties.Add(new Property("text-decoration") { Term = new PrimitiveTerm(UnitType.Ident, "none") });
			pageButton.Declarations.Properties.Add(new Property("text-align") { Term = new PrimitiveTerm(UnitType.Ident, "center") });
			pageButton.Declarations.Properties.Add(new Property("font-weight") { Term = new PrimitiveTerm(UnitType.Ident, "bold") });
			var shadowTerms = BuildTermList(TermList.TermSeparator.Space, new PrimitiveTerm(UnitType.Ident, "inset"), new PrimitiveTerm(UnitType.Pixel, 0),
				new PrimitiveTerm(UnitType.Pixel, 1), new PrimitiveTerm(UnitType.Pixel, 0), new PrimitiveTerm(UnitType.Pixel, 0), new PrimitiveTerm(UnitType.RGB, "#ffffff"));
			pageButton.Declarations.Properties.Add(new Property("box-shadow") { Term = shadowTerms });
			var textShadowTerms = BuildTermList(TermList.TermSeparator.Space, new PrimitiveTerm(UnitType.Pixel, 0), new PrimitiveTerm(UnitType.Pixel, 1),
				new PrimitiveTerm(UnitType.Pixel, 0), new PrimitiveTerm(UnitType.RGB, "#ffffff"));
			pageButton.Declarations.Properties.Add(new Property("text-shadow") { Term = textShadowTerms });
			var borderTerms = BuildTermList(TermList.TermSeparator.Space, new PrimitiveTerm(UnitType.Pixel, 1),
				new PrimitiveTerm(UnitType.Ident, "solid"), new PrimitiveTerm(UnitType.RGB, "#dcdcdc"));
			pageButton.Declarations.Properties.Add(new Property("border") { Term = borderTerms });
			pageButton.Declarations.Properties.Add(new Property("border-radius") { Term = new PrimitiveTerm(UnitType.Pixel, 6) });
			pageButton.Declarations.Properties.Add(new Property("background-color") { Term = new PrimitiveTerm(UnitType.RGB, "#ededed")});
			var currentButtonRule = new StyleRule { Value = "#currentPageButton" };
			currentButtonRule.Declarations.Properties.Add(new Property("background") { Term = new PrimitiveTerm(UnitType.Grad, "linear-gradient(to bottom, #dfdfdf 5%, #ededed 100%)") });
			currentButtonRule.Declarations.Properties.Add(new Property("background-color") { Term = new PrimitiveTerm(UnitType.RGB, "#cdcdcd") });

			return string.Format("{1}{0}{2}{0}{3}{0}{4}{0}{5}{0}{6}{0}", Environment.NewLine, screen.ToString(true), print.ToString(true),
				pageButton.ToString(true), pageButtonHover.ToString(true), pageButtonActive.ToString(true), currentButtonRule.ToString(true));
		}

		/// <summary>
		/// This method will build a css term list with all the provided terms separated by the provided separator
		/// </summary>
		private static TermList BuildTermList(TermList.TermSeparator separator, params Term[] terms)
		{
			var termList = new TermList();
			for(var i = 0; i < terms.Length; ++i)
			{
				if (i > 0)
				{
					termList.AddSeparator(separator);
				}
				termList.AddTerm(terms[i]);
			}
			return termList;
		}

		private class AncestorIndents
		{
			public AncestorIndents(float margin, float textIndent): this(null, margin, textIndent)
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
}
