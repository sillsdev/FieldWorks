// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FontInfo.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

using System;
using System.Drawing;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Resources;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.DomainServices
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Stores Font information.
	/// </summary>
	/// <remarks>BaseStyleInfo uses one of these objects to store the font-specific properties
	/// for a style, but any writing system can have overrides with its own font information.
	/// </remarks>
	/// ----------------------------------------------------------------------------------------
	public class FontInfo : ICharacterStyleInfo
	{
		#region Constants
		/// <summary>10 pts is the default if nothing is set explicitly</summary>
		public static readonly int kDefaultFontSize = 10;
		#endregion

		#region Data Members
		/// <summary>Font name</summary>
		public InheritableStyleProp<string> m_fontName = new InheritableStyleProp<string>();
		/// <summary>Size in millipoints</summary>
		public InheritableStyleProp<int> m_fontSize = new InheritableStyleProp<int>();
		/// <summary>Fore color (ARGB)</summary>
		public InheritableStyleProp<Color> m_fontColor = new InheritableStyleProp<Color>();
		/// <summary>Background color (ARGB)</summary>
		public InheritableStyleProp<Color> m_backColor = new InheritableStyleProp<Color>();
		/// <summary>Indicates whether font is bold or not</summary>
		public InheritableStyleProp<bool> m_bold = new InheritableStyleProp<bool>();
		/// <summary>Indicates whether font is italic or not</summary>
		public InheritableStyleProp<bool> m_italic = new InheritableStyleProp<bool>();
		/// <summary>Superscript, Subscript, or normal</summary>
		public InheritableStyleProp<FwSuperscriptVal> m_superSub = new InheritableStyleProp<FwSuperscriptVal>();
		/// <summary>Indicates that this style is Underline</summary>
		public InheritableStyleProp<FwUnderlineType> m_underline = new InheritableStyleProp<FwUnderlineType>();
		/// <summary>Underline color (ARGB)</summary>
		public InheritableStyleProp<Color> m_underlineColor = new InheritableStyleProp<Color>();
		/// <summary>Vertical offset</summary>
		public InheritableStyleProp<int> m_offset = new InheritableStyleProp<int>();
		/// <summary>Font features (used for Graphite fonts)</summary>
		public InheritableStyleProp<string> m_features = new InheritableStyleProp<string>();
		/// <summary><c>true</c> if this FontInfo is dirty.</summary>
		public bool IsDirty;
		#endregion

		#region constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FontInfo"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FontInfo()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copy constructor
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FontInfo(FontInfo copyFrom)
		{
			m_fontName = new InheritableStyleProp<string>(copyFrom.m_fontName);
			m_fontSize = new InheritableStyleProp<int>(copyFrom.m_fontSize);
			m_fontColor = new InheritableStyleProp<Color>(copyFrom.m_fontColor);
			m_backColor = new InheritableStyleProp<Color>(copyFrom.m_backColor);
			m_bold = new InheritableStyleProp<bool>(copyFrom.m_bold);
			m_italic = new InheritableStyleProp<bool>(copyFrom.m_italic);
			m_superSub = new InheritableStyleProp<FwSuperscriptVal>(copyFrom.m_superSub);
			m_underline = new InheritableStyleProp<FwUnderlineType>(copyFrom.m_underline);
			m_underlineColor = new InheritableStyleProp<Color>(copyFrom.m_underlineColor);
			m_offset = new InheritableStyleProp<int>(copyFrom.m_offset);
			m_features = new InheritableStyleProp<string>(copyFrom.m_features);
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance has any explicit values
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsAnyExplicit
		{
			get
			{
				return m_fontName.IsExplicit || m_fontSize.IsExplicit ||
					m_fontColor.IsExplicit || m_backColor.IsExplicit || m_bold.IsExplicit ||
					m_italic.IsExplicit || m_superSub.IsExplicit || m_underline.IsExplicit ||
					m_underlineColor.IsExplicit || m_offset.IsExplicit || m_features.IsExplicit;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a <see cref="T:System.String"></see> that represents the current
		/// <see cref="T:System.Object"></see>.  This method returns a human-readable
		/// string that is culture-sensitive
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"></see> that represents the current
		/// <see cref="T:System.Object"></see>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return ToString(false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a <see cref="T:System.String"></see> that represents the current
		/// <see cref="T:System.Object"></see>.  This method returns a human-readable
		/// string that is culture-sensitive
		/// </summary>
		/// <param name="fForceMinimumDescription">if set to <c>true</c> forces at least minimum
		/// description (i.e., font and size) to be returned.</param>
		/// <returns>
		/// A <see cref="T:System.String"></see> that represents the current
		/// <see cref="T:System.Object"></see>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public string ToString(bool fForceMinimumDescription)
		{
			StringBuilder text = new StringBuilder();

			if (m_fontName.IsExplicit || fForceMinimumDescription)
				AppendToString(text, UIFontName);

			if (m_fontSize.IsExplicit || fForceMinimumDescription)
				AppendToString(text, string.Format(Strings.ksXPt, m_fontSize.Value / 1000));

			if (m_fontColor.IsExplicit || m_backColor.IsExplicit)
				AppendFontColor(text);

			if (m_underline.IsExplicit || m_underlineColor.IsExplicit)
				AppendUnderline(text);

			if (m_bold.IsExplicit)
				AppendToString(text, m_bold.Value ? Strings.ksBold : Strings.ksNotBold);

			if (m_italic.IsExplicit)
				AppendToString(text, m_italic.Value ? Strings.ksItalic : Strings.ksNotItalic);

			if (m_superSub.IsExplicit)
				AppendSuperSub(text, m_superSub.Value);

			if (m_offset.IsExplicit)
				AppendFontOffset(text, m_offset.Value);

			return text.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified <see cref="T:System.Object"></see> is equal to the
		/// current <see cref="T:System.Object"></see>.
		/// </summary>
		/// <param name="obj">The <see cref="T:System.Object"></see> to compare with the current
		/// <see cref="T:System.Object"></see>.</param>
		/// <returns>
		/// true if the specified <see cref="T:System.Object"></see> is equal to the current
		/// <see cref="T:System.Object"></see>; otherwise, false.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override bool Equals(object obj)
		{
			FontInfo other = obj as FontInfo;
			if (other == null)
				return false;

			return m_backColor.Value.ToArgb().Equals(other.m_backColor.Value.ToArgb()) &&
				m_bold.Equals(other.m_bold) && m_features.Equals(other.m_features) &&
				m_fontColor.Value.ToArgb().Equals(other.m_fontColor.Value.ToArgb()) &&
				m_fontName.Equals(other.m_fontName) && m_fontSize.Equals(other.m_fontSize) &&
				m_italic.Equals(other.m_italic) && m_offset.Equals(other.m_offset) &&
				m_superSub.Equals(other.m_superSub) && m_underline.Equals(other.m_underline) &&
				m_underlineColor.Value.ToArgb().Equals(other.m_underlineColor.Value.ToArgb());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Serves as a hash function for a particular type.
		/// <see cref="M:System.Object.GetHashCode"></see> is suitable for use in hashing
		/// algorithms and data structures like a hash table.
		/// </summary>
		/// <returns>
		/// A hash code for the current <see cref="T:System.Object"></see>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets all default values.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SetAllDefaults()
		{
			m_backColor.SetDefaultValue(Color.Empty);
			m_bold.SetDefaultValue(false);
			m_fontColor.SetDefaultValue(Color.Black);
			m_fontName.SetDefaultValue(StyleServices.DefaultFont);
			m_fontSize.SetDefaultValue(kDefaultFontSize *1000);
			m_italic.SetDefaultValue(false);
			m_superSub.SetDefaultValue(FwSuperscriptVal.kssvOff);
			m_underline.SetDefaultValue(FwUnderlineType.kuntNone);
			m_offset.SetDefaultValue(0);
			m_features.SetDefaultValue(null);
			m_underlineColor.SetDefaultValue(Color.Black);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the font or a UI-compatible (i.e., localizable) token to represent
		/// a magic font value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string UIFontName
		{
			get { return GetUIFontName(m_fontName.Value); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the UI font.
		/// </summary>
		/// <param name="fontName">Name of the font.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static string GetUIFontName(string fontName)
		{
			if (fontName == StyleServices.DefaultFont)
				return ResourceHelper.GetResourceString("kstidDefaultFont");
			return fontName;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the internal font name for the given UI font.
		/// </summary>
		/// <param name="fontNameUI">UI name of the font.</param>
		/// <returns>Internal font name</returns>
		/// ------------------------------------------------------------------------------------
		public static string GetInternalFontName(string fontNameUI)
		{
			if (fontNameUI == ResourceHelper.GetResourceString("kstidDefaultFont"))
				return StyleServices.DefaultFont;
			return fontNameUI;
		}

		#region Helper methods and properties to build font description
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Appends the font and background color information to the description
		/// </summary>
		/// <param name="text">The text.</param>
		/// ------------------------------------------------------------------------------------
		private void AppendFontColor(StringBuilder text)
		{
			if (m_fontColor.IsInherited)
				AppendToString(text, String.Format(Strings.ksBackgroundIsX,
					ColorUtil.ColorToName(m_backColor.Value)));
			else if (m_backColor.IsInherited)
				AppendToString(text, String.Format(Strings.ksTextIsX,
					ColorUtil.ColorToName(m_fontColor.Value)));
			else
				AppendToString(text, string.Format(Strings.ksTextIsXonY,
					ColorUtil.ColorToName(m_fontColor.Value),
					ColorUtil.ColorToName(m_backColor.Value)));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Appends the underline information to the description.
		/// </summary>
		/// <param name="text">The text.</param>
		/// ------------------------------------------------------------------------------------
		private void AppendUnderline(StringBuilder text)
		{
			string sUnder = "";
			if (m_underlineColor.IsExplicit)
			{
				string sColor = ColorUtil.ColorToName(m_underlineColor.Value);
				if (m_underline.IsExplicit)
				{
					switch (m_underline.Value)
					{
						case FwUnderlineType.kuntNone:
							sUnder = String.Format(Strings.ksNoColorUnderline, sColor);
							break;
						case FwUnderlineType.kuntSingle:
							sUnder = String.Format(Strings.ksSingleColorUnderline, sColor);
							break;
						case FwUnderlineType.kuntDouble:
							sUnder = String.Format(Strings.ksDoubleColorUnderline, sColor);
							break;
						case FwUnderlineType.kuntDotted:
							sUnder = String.Format(Strings.ksDottedColorUnderline, sColor);
							break;
						case FwUnderlineType.kuntDashed:
							sUnder = String.Format(Strings.ksDashedColorUnderline, sColor);
							break;
						case FwUnderlineType.kuntStrikethrough:
							sUnder = String.Format(Strings.ksColorStrikethrough, sColor);
							break;
					}
				}
				else
				{
					sUnder = String.Format(Strings.ksColorUnderline, sColor);
				}
			}
			else if (m_underline.IsExplicit)
			{
				switch (m_underline.Value)
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Appends the superscript/subscript information to the description
		/// </summary>
		/// <param name="text">The text.</param>
		/// <param name="value">The superscript/subscript val.</param>
		/// ------------------------------------------------------------------------------------
		private void AppendSuperSub(StringBuilder text, FwSuperscriptVal value)
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Appends the font offset information to the description
		/// </summary>
		/// <param name="text">The text.</param>
		/// <param name="value">The value.</param>
		/// ------------------------------------------------------------------------------------
		private void AppendFontOffset(StringBuilder text, int value)
		{
			if (value > 0)
				AppendToString(text, string.Format(Strings.ksRaisedXpt, value / 1000));
			else if (value < 0)
				AppendToString(text, string.Format(Strings.ksLoweredXpt, -value / 1000));
			else
				AppendToString(text, Strings.ksNotRaisedLowered);

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Appends to string.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <param name="value">The value.</param>
		/// ------------------------------------------------------------------------------------
		private void AppendToString(StringBuilder text, string value)
		{
			if (text.Length > 0)
				text.Append(Strings.ksListSep);
			text.Append(value);
		}
		#endregion

		/// <summary>Name of font to use</summary>
		public IStyleProp<string> FontName
		{
			get { return m_fontName; }
		}

		/// <summary>Size in millipoints</summary>
		public IStyleProp<int> FontSize
		{
			get { return m_fontSize; }
		}

		/// <summary>Fore color (ARGB)</summary>
		public IStyleProp<Color> FontColor
		{
			get { return m_fontColor; }
		}

		/// <summary>Background color (ARGB)</summary>
		public IStyleProp<Color> BackColor
		{
			get { return m_backColor; }
		}

		/// <summary>Indicates whether font is bold or not</summary>
		public IStyleProp<bool> Bold
		{
			get { return m_bold; }
		}

		/// <summary>Indicates whether font is italic or not</summary>
		public IStyleProp<bool> Italic
		{
			get { return m_italic; }
		}

		/// <summary>Superscript, Subscript, or normal</summary>
		public IStyleProp<FwSuperscriptVal> SuperSub
		{
			get { return m_superSub; }
		}

		/// <summary>Indicates that this style is Underline</summary>
		public IStyleProp<FwUnderlineType> Underline
		{
			get { return m_underline; }
		}

		/// <summary>Underline color (ARGB)</summary>
		public IStyleProp<Color> UnderlineColor
		{
			get { return m_underlineColor; }
		}

		/// <summary>Vertical offset</summary>
		public IStyleProp<int> Offset
		{
			get { return m_offset; }
		}

		/// <summary>Font features (used for Graphite fonts)</summary>
		public IStyleProp<string> Features
		{
			get { return m_features; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets all properties to inherited.
		/// </summary>
		/// <param name="basedOnFontInfo">The font info from which to get the inherited values.
		/// </param>
		/// ------------------------------------------------------------------------------------
		internal void SetAllPropertiesToInherited(FontInfo basedOnFontInfo)
		{
			m_fontName.ResetToInherited(basedOnFontInfo.m_fontName);
			m_fontSize.ResetToInherited(basedOnFontInfo.m_fontSize);
			m_fontColor.ResetToInherited(basedOnFontInfo.m_fontColor);
			m_backColor.ResetToInherited(basedOnFontInfo.m_backColor);
			m_bold.ResetToInherited(basedOnFontInfo.m_bold);
			m_italic.ResetToInherited(basedOnFontInfo.m_italic);
			m_superSub.ResetToInherited(basedOnFontInfo.m_superSub);
			m_underline.ResetToInherited(basedOnFontInfo.m_underline);
			m_underlineColor.ResetToInherited(basedOnFontInfo.m_underlineColor);
			m_offset.ResetToInherited(basedOnFontInfo.m_offset);
			m_features.ResetToInherited(basedOnFontInfo.m_features);
		}
	}
}
