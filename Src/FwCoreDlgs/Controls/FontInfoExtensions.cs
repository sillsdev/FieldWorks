// Copyright (c) 2013-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Text;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.DomainServices;
using SIL.FieldWorks.Resources;

namespace SIL.FieldWorks.FwCoreDlgs.Controls
{
	/// <summary>
	/// UI extensions methods for FontInfo
	/// </summary>
	public static class FontInfoExtensions
	{
		/// <summary>
		/// Gets the name of the UI font.
		/// </summary>
		public static string UIFontName(this FontInfo fontInfo)
		{
			return fontInfo.m_fontName.Value == StyleServices.DefaultFont ? ResourceHelper.GetResourceString("kstidDefaultFont") : fontInfo.m_fontName.Value;
		}

		/// <summary>
		/// Returns a <see cref="T:System.String"></see> that represents the current
		/// <see cref="T:System.Object"></see>.  This method returns a human-readable
		/// string that is culture-sensitive
		/// </summary>
		/// <param name="fontInfo"></param>
		/// <param name="fForceMinimumDescription">if set to <c>true</c> forces at least minimum
		/// description (i.e., font and size) to be returned.</param>
		/// <returns>
		/// A <see cref="T:System.String"></see> that represents the current
		/// <see cref="T:System.Object"></see>.
		/// </returns>
		public static string ToString(this FontInfo fontInfo, bool fForceMinimumDescription)
		{
			var text = new StringBuilder();
			if (fontInfo.m_fontName.IsExplicit || fForceMinimumDescription)
			{
				AppendToString(text, fontInfo.UIFontName());
			}
			if (fontInfo.m_fontSize.IsExplicit || fForceMinimumDescription)
			{
				AppendToString(text, string.Format(Strings.ksXPt, fontInfo.m_fontSize.Value / 1000));
			}
			if (fontInfo.m_fontColor.IsExplicit || fontInfo.m_backColor.IsExplicit)
			{
				AppendFontColor(fontInfo, text);
			}
			if (fontInfo.m_underline.IsExplicit || fontInfo.m_underlineColor.IsExplicit)
			{
				AppendUnderline(fontInfo, text);
			}
			if (fontInfo.m_bold.IsExplicit)
			{
				AppendToString(text, fontInfo.m_bold.Value ? Strings.ksBold : Strings.ksNotBold);
			}
			if (fontInfo.m_italic.IsExplicit)
			{
				AppendToString(text, fontInfo.m_italic.Value ? Strings.ksItalic : Strings.ksNotItalic);
			}
			if (fontInfo.m_superSub.IsExplicit)
			{
				AppendSuperSub(text, fontInfo.m_superSub.Value);
			}
			if (fontInfo.m_offset.IsExplicit)
			{
				AppendFontOffset(text, fontInfo.m_offset.Value);
			}

			return text.ToString();
		}

		#region Helper methods and properties to build font description

		/// <summary>
		/// Appends the font and background color information to the description
		/// </summary>
		private static void AppendFontColor(FontInfo fontInfo, StringBuilder text)
		{
			if (fontInfo.m_fontColor.IsInherited)
			{
				AppendToString(text, string.Format(Strings.ksBackgroundIsX, ColorUtil.ColorToName(fontInfo.m_backColor.Value)));
			}
			else if (fontInfo.m_backColor.IsInherited)
			{
				AppendToString(text, string.Format(Strings.ksTextIsX, ColorUtil.ColorToName(fontInfo.m_fontColor.Value)));
			}
			else
			{
				AppendToString(text, string.Format(Strings.ksTextIsXonY, ColorUtil.ColorToName(fontInfo.m_fontColor.Value), ColorUtil.ColorToName(fontInfo.m_backColor.Value)));
			}
		}

		/// <summary>
		/// Appends the underline information to the description.
		/// </summary>
		private static void AppendUnderline(FontInfo fontInfo, StringBuilder text)
		{
			var sUnder = string.Empty;
			if (fontInfo.m_underlineColor.IsExplicit)
			{
				var sColor = ColorUtil.ColorToName(fontInfo.m_underlineColor.Value);
				if (fontInfo.m_underline.IsExplicit)
				{
					switch (fontInfo.m_underline.Value)
					{
						case FwUnderlineType.kuntNone:
							sUnder = string.Format(Strings.ksNoColorUnderline, sColor);
							break;
						case FwUnderlineType.kuntSingle:
							sUnder = string.Format(Strings.ksSingleColorUnderline, sColor);
							break;
						case FwUnderlineType.kuntDouble:
							sUnder = string.Format(Strings.ksDoubleColorUnderline, sColor);
							break;
						case FwUnderlineType.kuntDotted:
							sUnder = string.Format(Strings.ksDottedColorUnderline, sColor);
							break;
						case FwUnderlineType.kuntDashed:
							sUnder = string.Format(Strings.ksDashedColorUnderline, sColor);
							break;
						case FwUnderlineType.kuntStrikethrough:
							sUnder = string.Format(Strings.ksColorStrikethrough, sColor);
							break;
					}
				}
				else
				{
					sUnder = string.Format(Strings.ksColorUnderline, sColor);
				}
			}
			else if (fontInfo.m_underline.IsExplicit)
			{
				switch (fontInfo.m_underline.Value)
				{
					case FwUnderlineType.kuntNone:
						sUnder = Strings.ksNoUnderline;
						break;
					case FwUnderlineType.kuntSingle:
						sUnder = Strings.ksSingleUnderline;
						break;
					case FwUnderlineType.kuntDouble:
						sUnder = Strings.ksDoubleUnderline;
						break;
					case FwUnderlineType.kuntDotted:
						sUnder = Strings.ksDottedUnderline;
						break;
					case FwUnderlineType.kuntDashed:
						sUnder = Strings.ksDashedUnderline;
						break;
					case FwUnderlineType.kuntStrikethrough:
						sUnder = Strings.ksStrikethrough;
						break;
				}
			}
			AppendToString(text, sUnder);
		}

		/// <summary>
		/// Appends the superscript/subscript information to the description
		/// </summary>
		private static void AppendSuperSub(StringBuilder text, FwSuperscriptVal value)
		{
			switch (value)
			{
				case FwSuperscriptVal.kssvOff:
					AppendToString(text, Strings.ksNoSuperSubscript);
					break;
				case FwSuperscriptVal.kssvSub:
					AppendToString(text, Strings.ksSubscript);
					break;
				case FwSuperscriptVal.kssvSuper:
					AppendToString(text, Strings.ksSuperscript);
					break;
			}
		}

		/// <summary>
		/// Appends the font offset information to the description
		/// </summary>
		private static void AppendFontOffset(StringBuilder text, int value)
		{
			if (value > 0)
			{
				AppendToString(text, string.Format(Strings.ksRaisedXpt, value / 1000));
			}
			else if (value < 0)
			{
				AppendToString(text, string.Format(Strings.ksLoweredXpt, -value / 1000));
			}
			else
			{
				AppendToString(text, Strings.ksNotRaisedLowered);
			}
		}

		/// <summary>
		/// Appends to string.
		/// </summary>
		private static void AppendToString(StringBuilder text, string value)
		{
			if (text.Length > 0)
			{
				text.Append(Strings.ksListSep);
			}
			text.Append(value);
		}
		#endregion
	}
}