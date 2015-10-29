// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Resources;
using SIL.Utils;

namespace SIL.FieldWorks.FwCoreDlgControls
{
	/// <summary>
	/// UI extensions methods for FontInfo
	/// </summary>
	public static class FontInfoExtensions
	{
		/// <summary>
		/// Gets the name of the UI font.
		/// </summary>
		/// <returns></returns>
		public static string UIFontName(this FontInfo fontInfo)
		{
			if (fontInfo.m_fontName.Value == StyleServices.DefaultFont)
				return ResourceHelper.GetResourceString("kstidDefaultFont");
			return fontInfo.m_fontName.Value;
		}

		/// ------------------------------------------------------------------------------------
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
		/// ------------------------------------------------------------------------------------
		public static string ToString(this FontInfo fontInfo, bool fForceMinimumDescription)
		{
			var text = new StringBuilder();

			if (fontInfo.m_fontName.IsExplicit || fForceMinimumDescription)
				AppendToString(text, fontInfo.UIFontName());

			if (fontInfo.m_fontSize.IsExplicit || fForceMinimumDescription)
				AppendToString(text, string.Format(FwCoreDlgControls.ksXPt, fontInfo.m_fontSize.Value / 1000));

			if (fontInfo.m_fontColor.IsExplicit || fontInfo.m_backColor.IsExplicit)
				AppendFontColor(fontInfo, text);

			if (fontInfo.m_underline.IsExplicit || fontInfo.m_underlineColor.IsExplicit)
				AppendUnderline(fontInfo, text);

			if (fontInfo.m_bold.IsExplicit)
				AppendToString(text, fontInfo.m_bold.Value ? FwCoreDlgControls.ksBold : FwCoreDlgControls.ksNotBold);

			if (fontInfo.m_italic.IsExplicit)
				AppendToString(text, fontInfo.m_italic.Value ? FwCoreDlgControls.ksItalic : FwCoreDlgControls.ksNotItalic);

			if (fontInfo.m_superSub.IsExplicit)
				AppendSuperSub(text, fontInfo.m_superSub.Value);

			if (fontInfo.m_offset.IsExplicit)
				AppendFontOffset(text, fontInfo.m_offset.Value);

			return text.ToString();
		}

		#region Helper methods and properties to build font description

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Appends the font and background color information to the description
		/// </summary>
		/// <param name="fontInfo"></param>
		/// <param name="text">The text.</param>
		/// ------------------------------------------------------------------------------------
		private static void AppendFontColor(FontInfo fontInfo, StringBuilder text)
		{
			if (fontInfo.m_fontColor.IsInherited)
				AppendToString(text, String.Format(FwCoreDlgControls.ksBackgroundIsX,
					ColorUtil.ColorToName(fontInfo.m_backColor.Value)));
			else if (fontInfo.m_backColor.IsInherited)
				AppendToString(text, String.Format(FwCoreDlgControls.ksTextIsX,
					ColorUtil.ColorToName(fontInfo.m_fontColor.Value)));
			else
				AppendToString(text, string.Format(FwCoreDlgControls.ksTextIsXonY,
					ColorUtil.ColorToName(fontInfo.m_fontColor.Value),
					ColorUtil.ColorToName(fontInfo.m_backColor.Value)));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Appends the underline information to the description.
		/// </summary>
		/// <param name="fontInfo"></param>
		/// <param name="text">The text.</param>
		/// ------------------------------------------------------------------------------------
		private static void AppendUnderline(FontInfo fontInfo, StringBuilder text)
		{
			string sUnder = "";
			if (fontInfo.m_underlineColor.IsExplicit)
			{
				string sColor = ColorUtil.ColorToName(fontInfo.m_underlineColor.Value);
				if (fontInfo.m_underline.IsExplicit)
				{
					switch (fontInfo.m_underline.Value)
					{
						case FwUnderlineType.kuntNone:
							sUnder = String.Format(FwCoreDlgControls.ksNoColorUnderline, sColor);
							break;
						case FwUnderlineType.kuntSingle:
							sUnder = String.Format(FwCoreDlgControls.ksSingleColorUnderline, sColor);
							break;
						case FwUnderlineType.kuntDouble:
							sUnder = String.Format(FwCoreDlgControls.ksDoubleColorUnderline, sColor);
							break;
						case FwUnderlineType.kuntDotted:
							sUnder = String.Format(FwCoreDlgControls.ksDottedColorUnderline, sColor);
							break;
						case FwUnderlineType.kuntDashed:
							sUnder = String.Format(FwCoreDlgControls.ksDashedColorUnderline, sColor);
							break;
						case FwUnderlineType.kuntStrikethrough:
							sUnder = String.Format(FwCoreDlgControls.ksColorStrikethrough, sColor);
							break;
					}
				}
				else
				{
					sUnder = String.Format(FwCoreDlgControls.ksColorUnderline, sColor);
				}
			}
			else if (fontInfo.m_underline.IsExplicit)
			{
				switch (fontInfo.m_underline.Value)
				{
					case FwUnderlineType.kuntNone:
						sUnder = FwCoreDlgControls.ksNoUnderline;
						break;
					case FwUnderlineType.kuntSingle:
						sUnder = FwCoreDlgControls.ksSingleUnderline;
						break;
					case FwUnderlineType.kuntDouble:
						sUnder = FwCoreDlgControls.ksDoubleUnderline;
						break;
					case FwUnderlineType.kuntDotted:
						sUnder = FwCoreDlgControls.ksDottedUnderline;
						break;
					case FwUnderlineType.kuntDashed:
						sUnder = FwCoreDlgControls.ksDashedUnderline;
						break;
					case FwUnderlineType.kuntStrikethrough:
						sUnder = FwCoreDlgControls.ksStrikethrough;
						break;
				}
			}
			AppendToString(text, sUnder);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Appends the superscript/subscript information to the description
		/// </summary>
		/// <param name="text">The text.</param>
		/// <param name="value">The superscript/subscript val.</param>
		/// ------------------------------------------------------------------------------------
		private static void AppendSuperSub(StringBuilder text, FwSuperscriptVal value)
		{
			switch (value)
			{
				case FwSuperscriptVal.kssvOff:
					AppendToString(text, FwCoreDlgControls.ksNoSuperSubscript);
					break;
				case FwSuperscriptVal.kssvSub:
					AppendToString(text, FwCoreDlgControls.ksSubscript);
					break;
				case FwSuperscriptVal.kssvSuper:
					AppendToString(text, FwCoreDlgControls.ksSuperscript);
					break;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Appends the font offset information to the description
		/// </summary>
		/// <param name="text">The text.</param>
		/// <param name="value">The value.</param>
		/// ------------------------------------------------------------------------------------
		private static void AppendFontOffset(StringBuilder text, int value)
		{
			if (value > 0)
				AppendToString(text, string.Format(FwCoreDlgControls.ksRaisedXpt, value / 1000));
			else if (value < 0)
				AppendToString(text, string.Format(FwCoreDlgControls.ksLoweredXpt, -value / 1000));
			else
				AppendToString(text, FwCoreDlgControls.ksNotRaisedLowered);

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Appends to string.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <param name="value">The value.</param>
		/// ------------------------------------------------------------------------------------
		private static void AppendToString(StringBuilder text, string value)
		{
			if (text.Length > 0)
				text.Append(FwCoreDlgControls.ksListSep);
			text.Append(value);
		}
		#endregion
	}
}
