using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;

namespace SIL.FieldWorks.SharpViews.Utilities
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class to represent an RTF style, based on an IStyle, and possibly a writing system.
	/// If the writing system is zero, it represents the common characteristics of the base
	/// Style. If the writing system is specified, it represents those common characteristics
	/// plus any overrides which the IStyle specifies for that writing system.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class RtfStyle
	{
		private int m_defaultWs;
		private IStyle Style { get; set; }

		/// <summary>
		/// Initializes a new instance
		/// </summary>
		public RtfStyle(IStyle style, int defaultWs)
		{
			Style = style;
			m_defaultWs = defaultWs;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Format the style information as an RTF style string with composed Unicode characters.
		/// </summary>
		/// <param name="styleName">name of the style to export</param>
		/// <param name="styleTable"><c>true</c> to write data for the RTF style table,
		/// <c>false</c> for a usage instance in data</param>
		/// ------------------------------------------------------------------------------------
		public string ToString(string styleName, bool styleTable)
		{
			if (Style.IsParagraphStyle)
				return TsStringUtils.Compose(ParaStyleToString(styleName, styleTable));
			return TsStringUtils.Compose(CharStyleToString(styleName, styleTable));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets all colors associated with this RTF style entry.
		/// </summary>
		/// <returns>Enumerator to a unique list of colors</returns>
		/// ------------------------------------------------------------------------------------
		public IEnumerable<Color> GetAllColors()
		{
			var colors = new List<Color>();
			AddFontInfoColors(colors, Style.DefaultCharacterStyleInfo);
			if (Style.OverrideCharacterStyleInfo(m_defaultWs) != null)
			{
				AddFontInfoColors(colors, Style.OverrideCharacterStyleInfo(m_defaultWs));
			}
			//if (!m_borderColor.IsInherited && !colors.Contains(m_borderColor.Value))
			//    colors.Add(m_borderColor.Value);
			return colors;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets all explicit font names associated with this RTF style entry.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public IEnumerable<string> GetAllExplicitFontNames()
		{
			var fontNames = new List<string>();
			AddFontInfoName(fontNames, Style.DefaultCharacterStyleInfo);
			if (Style.OverrideCharacterStyleInfo(m_defaultWs) != null)
			{
				AddFontInfoName(fontNames, Style.OverrideCharacterStyleInfo(m_defaultWs));
			}
			return fontNames;
		}

		#region private methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the font info colors.
		/// </summary>
		/// <param name="colors">The list of colors (RGB values).</param>
		/// <param name="fontInfo">The font info.</param>
		/// ------------------------------------------------------------------------------------
		private static void AddFontInfoColors(List<Color> colors, ICharacterStyleInfo fontInfo)
		{
			if (fontInfo.FontColor.ValueIsSet && !colors.Contains(fontInfo.FontColor.Value))
				colors.Add(fontInfo.FontColor.Value);
			if (fontInfo.BackColor.ValueIsSet && !colors.Contains(fontInfo.BackColor.Value))
				colors.Add(fontInfo.BackColor.Value);
			if (!fontInfo.UnderlineColor.ValueIsSet && !colors.Contains(fontInfo.UnderlineColor.Value))
				colors.Add(fontInfo.UnderlineColor.Value);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the font name to the list if it is a real font name (not magic).
		/// </summary>
		/// <param name="fontNames">The list of font names.</param>
		/// <param name="fontInfo">The font info.</param>
		/// ------------------------------------------------------------------------------------
		private static void AddFontInfoName(List<string> fontNames, ICharacterStyleInfo fontInfo)
		{
			if (!fontInfo.FontName.ValueIsSet)
				return;
			string fontName = fontInfo.FontName.Value;
			if (!fontNames.Contains(fontName) && !StyleServices.IsMagicFontName(fontName))
				fontNames.Add(fontName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The base class stores all measurement values in millipoints (72000 mp = 1 inch).
		/// RTF requires units to be twips (20th of a point).
		/// </summary>
		/// <param name="mpValue">Measurement value in millipoints</param>
		/// ------------------------------------------------------------------------------------
		private static int ConvertMillipointsToTwips(int mpValue)
		{
			return mpValue / 50;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a font size tag for a value given in millipoints
		/// </summary>
		/// <remarks>
		/// The base class stores all measurement values in millipoints (72000 mp = 1 inch).
		/// RTF requires font units to be double the size in points (this allows for half-point
		/// sizes).
		/// </remarks>
		/// <param name="mpValue">Measurement value in millipoints</param>
		/// ------------------------------------------------------------------------------------
		private string FontSizeTag(int mpValue)
		{
			return IntegerWithTag(mpValue / 500, @"\fs");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make the superscript/subscript setting into a string representation
		/// </summary>
		/// <param name="fontInfo">The font info.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private static string SuperSubString(ICharacterStyleInfo fontInfo)
		{
			if (fontInfo != null && fontInfo.SuperSub != null)
			{
				if (fontInfo.SuperSub.ValueIsSet)
				{
					switch (fontInfo.SuperSub.Value)
					{
						case FwSuperscriptVal.kssvOff:
							return @"\nosupersub";
						case FwSuperscriptVal.kssvSuper:
							return @"\super";
						case FwSuperscriptVal.kssvSub:
							return @"\sub";
					}
				}
			}
			return string.Empty;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Format a paragraph style as an RTF style string
		/// </summary>
		/// <param name="styleName">Name of the style.</param>
		/// <param name="styleTable"><c>true</c> to write data for the RTF style table,
		/// <c>false</c> for a usage instance in data</param>
		/// ------------------------------------------------------------------------------------
		private string ParaStyleToString(string styleName, bool styleTable)
		{
			string basePortion = string.Empty;
			var paraInfo = Style.ParagraphStyleInfo;
			var defInfo = Style.DefaultCharacterStyleInfo;
			var overInfo = Style.OverrideCharacterStyleInfo(m_defaultWs);

			if (paraInfo != null)
			{
				string directionString = string.Empty;
				//if (DirectionIsRightToLeft == TriStateBool.triTrue)
				//    directionString = @"\rtlpar";
				//else if (!m_rtl.IsInherited && DirectionIsRightToLeft == TriStateBool.triFalse)
				//    directionString = @"\ltrpar";

				string alignmentString = @"\ql";
				//if (!m_alignment.IsInherited ||
				//    m_alignment.Value != FwTextAlign.ktalLeading ||
				//    DirectionIsRightToLeft == TriStateBool.triTrue)
				//{

				if (paraInfo.Alignment != null)
				{
					switch (paraInfo.Alignment.Value)
					{
						case FwTextAlign.ktalLeft:
							break;
						case FwTextAlign.ktalCenter:
							alignmentString = @"\qc";
							break;
							//case FwTextAlign.ktalTrailing:
							//    alignmentString = (DirectionIsRightToLeft == TriStateBool.triTrue) ? @"\ql" : @"\qr";
							//    break;
							//case FwTextAlign.ktalLeading:
							//    alignmentString = (DirectionIsRightToLeft == TriStateBool.triTrue) ? @"\qr" : @"\ql";
							//    break;
						case FwTextAlign.ktalRight:
							alignmentString = @"\qr";
							break;
						case FwTextAlign.ktalJustify:
							alignmentString = @"\qj";
							break;
					}
				}

				// If a negative first line indent was given then it means "hanging" indent. To work
				// properly, it needs to be compensated for in the left indent.
				int leadingIndent = 0;
				if (paraInfo.MarginLeading != null)
				{
					leadingIndent = paraInfo.MarginLeading.Value;
					if (paraInfo.FirstLineIndent != null && paraInfo.FirstLineIndent.Value < 0)
						leadingIndent -= paraInfo.FirstLineIndent.Value;
				}

				basePortion = @"\s" + StyleNumber
							  + directionString
							  + alignmentString
							  + (paraInfo.FirstLineIndent != null
									? IntegerWithTag(ConvertMillipointsToTwips(paraInfo.FirstLineIndent.Value), @"\fi")
									: IntegerWithTag(0, @"\fi"))
							  + IntegerWithTag(ConvertMillipointsToTwips(leadingIndent), @"\lin")
							  + (paraInfo.MarginTrailing != null
									? IntegerWithTag(ConvertMillipointsToTwips(paraInfo.MarginTrailing.Value), @"\rin")
									: IntegerWithTag(0, @"\rin"))
							  + (paraInfo.MarginTop != null
									? IntegerWithTag(ConvertMillipointsToTwips(paraInfo.MarginTop.Value), @"\sb")
									: IntegerWithTag(0, @"\sb"))
							  + (paraInfo.MarginBottom != null
									? IntegerWithTag(ConvertMillipointsToTwips(paraInfo.MarginBottom.Value), @"\sa")
									: IntegerWithTag(0, @"\sa"))
							  + (paraInfo.PadLeading != null
									? IntegerWithTag(ConvertMillipointsToTwips(paraInfo.PadLeading.Value), @"\tscellpadl")
									: IntegerWithTag(0, @"\tscellpadl"))
							  + (paraInfo.PadTrailing != null
									? IntegerWithTag(ConvertMillipointsToTwips(paraInfo.PadTrailing.Value), @"\tscellpadr")
									: IntegerWithTag(0, @"\tscellpadr"))
							  + (paraInfo.PadTop != null
									? IntegerWithTag(ConvertMillipointsToTwips(paraInfo.PadTop.Value), @"\tscellpadt")
									: IntegerWithTag(0, @"\tscellpadt"))
							  + (paraInfo.PadBottom != null
									? IntegerWithTag(ConvertMillipointsToTwips(paraInfo.PadBottom.Value), @"\tscellpadb")
									: IntegerWithTag(0, @"\tscellpadb"))
							  + LineSpacingAsString(paraInfo)
							  + BorderAsString(paraInfo);
			}

			if (defInfo != null || overInfo != null)
			{
				basePortion += (overInfo != null && overInfo.FontName != null ? FontTagForWs(overInfo) : string.Empty)
							   + GetPropValue(defInfo, overInfo, over => over.Bold, val => val ? @"\b" : string.Empty) // \b0 to force off?
								 + GetPropValue(defInfo, overInfo, over => over.Italic, val => val ? @"\i" : string.Empty)
								 + GetPropValue(defInfo, overInfo, over => over.FontSize, FontSizeTag)
								 + GetPropValue(defInfo, overInfo, over => over.FontColor, val => @"\cf" + Colors[val])
								 + GetPropValue(defInfo, overInfo, over => over.UnderlineColor, val => @"\ulc" + Colors[val])
								 + SuperSubString(defInfo);
			}
			if (styleTable)
			{
				return basePortion +
					   @"\additive" +
					   " " + ConvertString(styleName);
			}
			if(defInfo != null || overInfo != null)
				basePortion += GetPropValue(defInfo, overInfo, over => over.BackColor, val => @"\highlight" + Colors[val]);
			return basePortion;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a string representing the line spacing setting. This setting is optional
		/// and requires an additional tag based on its value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static string LineSpacingAsString(IParaStyleInfo paraStyleInfo)
		{
			int height = 0;
			if (paraStyleInfo.LineHeight != null)
				height = paraStyleInfo.LineHeight.Value.m_lineHeight;
			// If the line spacing is not specified, then do not put anything into the style
			if (height == 0)
				return string.Empty;

			if (paraStyleInfo.LineHeight.Value.m_relative)
			{
				switch (height)
				{
					case 10000:
						return string.Empty;
					case 15000:
						return @"\sl360\slmult1";
					case 20000:
						return @"\sl480\slmult1";
					default:
						Debug.Assert(false);
						return string.Empty;
				}
			}

			int lineSpacingInTwips = ConvertMillipointsToTwips(height);
			// Negative line spacing is interpreted as "exact" and requires an \slmult0 tag
			// following it.
			if (lineSpacingInTwips < 0)
				return @"\sl" + lineSpacingInTwips + @"\slmult0";
			return @"\sl" + lineSpacingInTwips;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a string representing the border settings. These settings are optional
		/// and require an additional tag based on the values given
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static string BorderAsString(IParaStyleInfo paraStyleInfo)
		{
			// Assume that there is no border unless proven otherwise
			string borderString = string.Empty;
			var border = new BorderThicknesses(0, 0, 0, 0);

			if (paraStyleInfo.BorderLeading != null)
				border.Leading = paraStyleInfo.BorderLeading.Value;
			if (paraStyleInfo.BorderTrailing != null)
				border.Trailing = paraStyleInfo.BorderTrailing.Value;
			if (paraStyleInfo.BorderTop != null)
				border.Top = paraStyleInfo.BorderTop.Value;
			if (paraStyleInfo.BorderBottom != null)
				border.Bottom = paraStyleInfo.BorderBottom.Value;

			string borderColorString = ((paraStyleInfo.BorderColor == null || paraStyleInfo.BorderColor.Value == Color.Empty)
											? string.Empty
											: @"\brdrcf" + paraStyleInfo.BorderColor.Value.ToKnownColor());
			if (border.Top > 0)
			{
				borderString += @"\brdrt\brdrs\brdrw" + ConvertMillipointsToTwips(border.Top)
								+ @"\brsp20" + borderColorString;
			}
			if (border.Bottom > 0)
			{
				borderString += @"\brdrb\brdrs\brdrw" + ConvertMillipointsToTwips(border.Bottom)
								+ @"\brsp20" + borderColorString;
			}
			if (border.Leading > 0)
			{
				borderString += @"\brdrl\brdrs\brdrw" + ConvertMillipointsToTwips(border.Leading)
								+ @"\brsp80" + borderColorString;
			}
			if (border.Trailing > 0)
			{
				borderString += @"\brdrr\brdrs\brdrw" + ConvertMillipointsToTwips(border.Trailing)
								+ @"\brsp80" + borderColorString;
			}
			return borderString;
		}

		internal int StyleNumber { get; set; }
		internal Dictionary<string, int> Fonts { get; set; }
		internal Dictionary<Color, int> Colors { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Formats a character style as an RTF style string
		/// </summary>
		/// <param name="styleName">Name of the style.</param>
		/// <param name="styleTable"><c>true</c> to write data for the RTF style table,
		/// <c>false</c> for a usage instance in data</param>
		/// ------------------------------------------------------------------------------------
		private string CharStyleToString(string styleName, bool styleTable)
		{
			var defInfo = Style.DefaultCharacterStyleInfo;
			var overInfo = Style.OverrideCharacterStyleInfo(m_defaultWs);

			string basePortion = @"\*\cs" + StyleNumber
								 + GetPropValue(defInfo, overInfo, over => over.Bold, val => val ? @"\b" : string.Empty)
								 // \b0 to force off?
								 + GetPropValue(defInfo, overInfo, over => over.Italic, val => val ? @"\i" : string.Empty)
								 + GetPropValue(defInfo, overInfo, over => over.FontName, val => @"\f" + Fonts[val])
								 + GetPropValue(defInfo, overInfo, over => over.FontSize, FontSizeTag)
								 + GetPropValue(defInfo, overInfo, over => over.FontColor, val => @"\cf" + Colors[val])
								 + GetPropValue(defInfo, overInfo, over => over.UnderlineColor, val => @"\ulc" + Colors[val]);
			//+ SuperSubString(defInfo);

			if (styleTable)
			{
				return basePortion +
					   @"\additive" +
					   " " + ConvertString(styleName);
			}
			return basePortion
				   + GetPropValue(defInfo, overInfo, over => over.BackColor, val => @"\highlight" + Colors[val]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Converts the string for use in RTF.
		/// </summary>
		/// <param name="text">The text to convert.</param>
		/// <returns>The converted string</returns>
		/// ------------------------------------------------------------------------------------
		public static string ConvertString(String text)
		{
			if (text == null)
				text = string.Empty;

			// Build a string to write. For RTF, unicode characters need to be written
			// in the form "\uc0\uX " where X is the decimal unicode character value.
			var bldr = new StringBuilder(1000);

			// RTF uses signed 16-bit values so any unicode characters greater than 32,767 will
			// be expressed as negative numbers.
			foreach (char ch in text)
			{
				var chValue = (short)ch;
				if (ch == '\\')
					bldr.Append(@"\\");
				else if (ch == '{' || ch == '}')
				{
					bldr.Append(@"\");
					bldr.Append(ch);
				}
				else if (chValue >= 0 && ch <= 127)
					bldr.Append(ch);
				else if (chValue == StringUtils.kChHardLB)
					bldr.Append(@"\line ");
				else
					bldr.Append(@"\uc0\u" + chValue + " ");
			}
			return bldr.ToString();
		}


		static string GetPropValue<T>(ICharacterStyleInfo defProps, ICharacterStyleInfo overrides, Func<ICharacterStyleInfo, IStyleProp<T>> getter,
			Func<T, string> writer)
		{
			var def = getter(defProps);
			if (overrides != null)
			{
				var over = getter(overrides);
				if (over != null && over.ValueIsSet)
					return writer(over.Value);
			}
			if (def != null && def.ValueIsSet)
				return writer(def.Value);
			return "";
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Convert a boolean expression to a string representation
		/// </summary>
		/// <param name="expr">expression to evaluate</param>
		/// <param name="trueString">string for true condition</param>
		/// <param name="falseString">string for false condition</param>
		/// <returns>the true or false string based on the value of the expression</returns>
		/// ------------------------------------------------------------------------------------
		private string ExpressionToString(bool expr, string trueString, string falseString)
		{
			return expr ? trueString : falseString;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make a string tag for an integer value if the value is not zero
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static string IntegerWithTag(int intValue, string tag)
		{
			if (intValue == 0)
				return string.Empty;
			return tag + intValue;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the font tag for the given writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private string FontTagForWs(ICharacterStyleInfo characterStyleInfo)
		{
			string sFontName = characterStyleInfo.FontName.Value;
			int fontId;
			Fonts.TryGetValue(sFontName, out fontId);
			return sFontName == null || fontId < 1 ? string.Empty :
				IntegerWithTag(fontId, @"\f");
		}
		#endregion
	}
}
