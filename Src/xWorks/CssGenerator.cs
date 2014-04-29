using System;
using System.Collections.Generic;
using ExCSS;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO.DomainServices;
using XCore;
using Property = ExCSS.Property;

namespace SIL.FieldWorks.XWorks
{
	public static class CssGenerator
	{
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
			var beforeAfterSelectors = GenerateSelectorsFromNode(baseSelection, configNode, out baseSelection);
			rule.Value = baseSelection;
			// if the configuration node defines a style then add all the rules generated from that style
			if(!String.IsNullOrEmpty(configNode.Style))
			{
				rule.Declarations.Properties.AddRange(GenerateCssStyleFromFwStyleSheet(configNode.Style, mediator).Properties);
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

		/// <summary>
		/// This method will generate before and after rules if the configuration node requires them. It also generates the selector for the node
		/// </summary>
		/// <param name="parentInfo"></param>
		/// <param name="configNode"></param>
		/// <param name="baseSelection"></param>
		/// <returns></returns>
		private static IEnumerable<StyleRule> GenerateSelectorsFromNode(string parentInfo, ConfigurableDictionaryNode configNode, out string baseSelection)
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
			return "." + configNode.FieldDescription.ToLower();
		}

		/// <summary>
		/// Generates a css StyleDeclaration for the requested FieldWorks style.
		/// <remarks>internal to facilitate separate unit testing.</remarks>
		/// </summary>
		/// <param name="styleName"></param>
		/// <param name="mediator"></param>
		/// <returns></returns>
		internal static StyleDeclaration GenerateCssStyleFromFwStyleSheet(string styleName, Mediator mediator)
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
				borderLeft.Term = new PrimitiveTerm(UnitType.Point, exportStyleInfo.BorderLeading);
				var borderRight = new Property("border-right-width");
				borderRight.Term = new PrimitiveTerm(UnitType.Point, exportStyleInfo.BorderTrailing);
				var borderTop = new Property("border-top-width");
				borderTop.Term = new PrimitiveTerm(UnitType.Point, exportStyleInfo.BorderTop);
				var borderBottom = new Property("border-bottom-width");
				borderBottom.Term = new PrimitiveTerm(UnitType.Point, exportStyleInfo.BorderBottom);
				declaration.Add(borderLeft);
				declaration.Add(borderRight);
				declaration.Add(borderTop);
				declaration.Add(borderBottom);
			}
			if(exportStyleInfo.HasFirstLineIndent)
			{
				//TODO
			}
			if(exportStyleInfo.HasKeepTogether)
			{
				//TODO
			}
			if(exportStyleInfo.HasKeepWithNext)
			{
				//TODO
			}
			if(exportStyleInfo.HasLeadingIndent)
			{
				declaration.Add(new Property("padding-left") { Term = new PrimitiveTerm(UnitType.Point, exportStyleInfo.LeadingIndent) });
			}
			if(exportStyleInfo.HasLineSpacing)
			{
				//TODO
			}
			if(exportStyleInfo.HasSpaceAfter)
			{
				declaration.Add(new Property("padding-bottom") { Term = new PrimitiveTerm(UnitType.Point, exportStyleInfo.SpaceAfter) });
			}
			if(exportStyleInfo.HasSpaceBefore)
			{
				declaration.Add(new Property("padding-top") { Term = new PrimitiveTerm(UnitType.Point, exportStyleInfo.SpaceBefore) });
			}
			if(exportStyleInfo.HasTrailingIndent)
			{
				declaration.Add(new Property("padding-right") { Term = new PrimitiveTerm(UnitType.Point, exportStyleInfo.TrailingIndent) });
			}

			// Build css rules for font properties
			if(projectStyle.DefaultCharacterStyleInfo.FontName.ValueIsSet)
			{
				var fontFamily = new Property("font-family");
				fontFamily.Term = new TermList(new PrimitiveTerm(UnitType.String, projectStyle.DefaultCharacterStyleInfo.FontName.Value),
														 new PrimitiveTerm(UnitType.Ident, "serif"));
				declaration.Add(fontFamily);
			}
			var fontSize = new Property("font-size");
			fontSize.Term = new PrimitiveTerm(UnitType.Point, projectStyle.DefaultCharacterStyleInfo.FontSize.Value);
			declaration.Add(fontSize);

			var fontStyle = new Property("font-style");
			fontStyle.Term = new PrimitiveTerm(UnitType.Ident, projectStyle.DefaultCharacterStyleInfo.Italic.Value ? "italic" : "normal");
			declaration.Add(fontStyle);

			var fontWeight = new Property("font-weight");
			fontWeight.Term = new PrimitiveTerm(UnitType.Ident,
															projectStyle.DefaultCharacterStyleInfo.Bold.Value ? "bold" : "normal");
			declaration.Add(fontWeight);

			if(exportStyleInfo.DefaultCharacterStyleInfo.FontColor.ValueIsSet)
			{
				var styleColorValue = projectStyle.DefaultCharacterStyleInfo.FontColor.Value;
				var fontColor = new Property("color");
				fontColor.Term = new PrimitiveTerm(UnitType.RGB,
															  HtmlColor.FromRgba(styleColorValue.R, styleColorValue.G, styleColorValue.B, styleColorValue.A).ToString());
				declaration.Add(fontColor);
			}

			if(exportStyleInfo.DefaultCharacterStyleInfo.BackColor.ValueIsSet)
			{
				var styleColorValue = projectStyle.DefaultCharacterStyleInfo.BackColor.Value;
				var fontColor = new Property("background-color");
				fontColor.Term = new PrimitiveTerm(UnitType.RGB,
															  HtmlColor.FromRgba(styleColorValue.R, styleColorValue.G, styleColorValue.B, styleColorValue.A).ToString());
				declaration.Add(fontColor);
			}

			return declaration;
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
	}
}