// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FontInfo.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System.Drawing;
using SIL.FieldWorks.Common.COMInterfaces;

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
