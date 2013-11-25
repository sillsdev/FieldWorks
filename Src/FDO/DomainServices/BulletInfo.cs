// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: BulletInfo.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.DomainServices
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Stores bullet information
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public struct BulletInfo
	{
		/// <summary>Bullet scheme</summary>
		public VwBulNum m_numberScheme;
		/// <summary>Starting number</summary>
		public int m_start;
		/// <summary>Text before the number</summary>
		public string m_textBefore;
		/// <summary>Text after the number</summary>
		public string m_textAfter;
		/// <summary>The font information</summary>
		private FontInfo m_fontInfo;

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:BulletInfo"/> class.
		/// </summary>
		/// <param name="other">The other bullet info to copy from.</param>
		/// ------------------------------------------------------------------------------------
		public BulletInfo(BulletInfo other)
		{
			m_numberScheme = other.m_numberScheme;
			m_start = other.m_start;
			m_textBefore = other.m_textBefore;
			m_textAfter = other.m_textAfter;
			m_fontInfo = new FontInfo(other.FontInfo);
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares the contents to another instance of a BulletInfo
		/// </summary>
		/// <param name="other">item to compare to</param>
		/// <returns>true if the contents are equal</returns>
		/// ------------------------------------------------------------------------------------
		public bool CompareEquals(BulletInfo other)
		{
			return
				(m_numberScheme == other.m_numberScheme) &&
				(m_start == other.m_start) &&
				(m_textAfter == other.m_textAfter) &&
				(m_textBefore == other.m_textBefore) &&
				(m_fontInfo == other.m_fontInfo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the encoded font info as a string BLOB suitable for storing in the
		/// database.
		/// </summary>
		/// <value>The encoded font info.</value>
		/// ------------------------------------------------------------------------------------
		public string EncodedFontInfo
		{
			get { return EncodeFontInfo(m_fontInfo); }
			set { m_fontInfo = DecodeFontInfo(value); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the font info.
		/// </summary>
		/// <value>The font info.</value>
		/// ------------------------------------------------------------------------------------
		public FontInfo FontInfo
		{
			get
			{
				if (m_fontInfo == null)
				{
					m_fontInfo = new FontInfo();
					m_fontInfo.SetAllDefaults();
				}
				return m_fontInfo;
			}
			set { m_fontInfo = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Encodes the font info as a string BLOB suitable for storing in the database.
		/// </summary>
		/// <param name="fontInfo">The font information.</param>
		/// <returns>Font info BLOB</returns>
		/// ------------------------------------------------------------------------------------
		private static string EncodeFontInfo(FontInfo fontInfo)
		{
			List<char> binaryProps = new List<char>();
			SetIntProp(binaryProps, fontInfo.m_backColor.IsExplicit, FwTextPropType.ktptBackColor,
				ColorUtil.ConvertColorToBGR(fontInfo.m_backColor.Value));
			SetIntProp(binaryProps, fontInfo.m_bold.IsExplicit, FwTextPropType.ktptBold,
				Convert.ToUInt32(fontInfo.m_bold.Value));
			SetIntProp(binaryProps, fontInfo.m_fontColor.IsExplicit, FwTextPropType.ktptForeColor,
				ColorUtil.ConvertColorToBGR(fontInfo.m_fontColor.Value));
			SetIntProp(binaryProps, fontInfo.m_fontSize.IsExplicit, FwTextPropType.ktptFontSize,
				(uint)fontInfo.m_fontSize.Value);
			SetIntProp(binaryProps, fontInfo.m_italic.IsExplicit, FwTextPropType.ktptItalic,
				Convert.ToUInt32(fontInfo.m_italic.Value));
			SetIntProp(binaryProps, fontInfo.m_underline.IsExplicit, FwTextPropType.ktptUnderline,
				(uint)fontInfo.m_underline.Value);
			SetIntProp(binaryProps, fontInfo.m_underlineColor.IsExplicit, FwTextPropType.ktptUnderColor,
				ColorUtil.ConvertColorToBGR(fontInfo.m_underlineColor.Value));
			SetIntProp(binaryProps, fontInfo.m_offset.IsExplicit, FwTextPropType.ktptOffset,
				(uint)fontInfo.m_offset.Value);
			SetIntProp(binaryProps, fontInfo.m_superSub.IsExplicit, FwTextPropType.ktptSuperscript,
				(uint)fontInfo.m_superSub.Value);
			SetStrProp(binaryProps, fontInfo.m_fontName.IsExplicit, FwTextPropType.ktptFontFamily,
				fontInfo.m_fontName.Value);
			SetStrProp(binaryProps, fontInfo.m_features.IsExplicit, FwTextPropType.ktptFontVariations,
				fontInfo.m_features.Value);

			StringBuilder bldr = new StringBuilder();
			bldr.Append(binaryProps.ToArray());
			return bldr.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets an integer property.
		/// </summary>
		/// <param name="binaryProps">The binary properties.</param>
		/// <param name="fIsExplicit"><c>true</c> if the value is explicitly set and should be
		/// added to the props list.</param>
		/// <param name="tpt">The property type.</param>
		/// <param name="value">The property value.</param>
		/// ------------------------------------------------------------------------------------
		private static void SetIntProp(List<char> binaryProps, bool fIsExplicit, FwTextPropType tpt,
			uint value)
		{
			if (fIsExplicit)
			{
				binaryProps.Add((char)tpt);
				binaryProps.Add(Convert.ToChar(value & 0xFFFF));
				binaryProps.Add(Convert.ToChar(value >> 16));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets a string property.
		/// </summary>
		/// <param name="binaryProps">The binary properties.</param>
		/// <param name="fIsExplicit"><c>true</c> if the value is explicitly set and should be
		/// added to the props list.</param>
		/// <param name="tpt">The property type.</param>
		/// <param name="value">The property value.</param>
		/// ------------------------------------------------------------------------------------
		private static void SetStrProp(List<char> binaryProps, bool fIsExplicit, FwTextPropType tpt,
			string value)
		{
			if (fIsExplicit)
			{
				binaryProps.Add((char)tpt);
				binaryProps.AddRange(value.ToCharArray());
				binaryProps.Add((char)0);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Decodes the font info.
		/// </summary>
		/// <param name="blob">The BLOB.</param>
		/// <returns>The font information represented in the BLOB.</returns>
		/// ------------------------------------------------------------------------------------
		private static FontInfo DecodeFontInfo(string blob)
		{
			FontInfo fontInfo = new FontInfo();
			fontInfo.SetAllDefaults();

			if (blob != null)
			{
				for (int i = 0; i < blob.Length; )
				{
					int iPropLim = blob.IndexOf('\0', i);
					if (iPropLim < 0)
						iPropLim = blob.Length;

					FwTextPropType tpt = (FwTextPropType)blob[i++];
					if (tpt == FwTextPropType.ktptFontFamily)
					{
						fontInfo.m_fontName.ExplicitValue = blob.Substring(i, iPropLim - i);
						i += (iPropLim - i + 1);
						break;
					}
					else if (tpt == FwTextPropType.ktptFontVariations)
					{
						fontInfo.m_features.ExplicitValue = blob.Substring(i, iPropLim - i);
						i += (iPropLim - i + 1);
						break;
					}

					int nVal = (int)blob[i] + (int)(blob[i + 1] << 16);
					i += 2;

					switch (tpt)
					{
						case FwTextPropType.ktptBold:
							fontInfo.m_bold.ExplicitValue = nVal == 1;
							break;
						case FwTextPropType.ktptItalic:
							fontInfo.m_italic.ExplicitValue = nVal == 1;
							break;
						case FwTextPropType.ktptSuperscript:
							fontInfo.m_superSub.ExplicitValue = (FwSuperscriptVal)nVal;
							break;
						case FwTextPropType.ktptUnderline:
							fontInfo.m_underline.ExplicitValue = (FwUnderlineType)nVal;
							break;
						case FwTextPropType.ktptFontSize:
							fontInfo.m_fontSize.ExplicitValue = nVal;
							break;
						case FwTextPropType.ktptOffset:
							fontInfo.m_offset.ExplicitValue = nVal;
							break;
						case FwTextPropType.ktptBackColor:
							fontInfo.m_backColor.ExplicitValue = ColorUtil.ConvertBGRtoColor((uint)nVal);
							break;
						case FwTextPropType.ktptForeColor:
							fontInfo.m_fontColor.ExplicitValue = ColorUtil.ConvertBGRtoColor((uint)nVal);
							break;
						case FwTextPropType.ktptUnderColor:
							fontInfo.m_underlineColor.ExplicitValue = ColorUtil.ConvertBGRtoColor((uint)nVal);
							break;
					}
				}
			}

			return fontInfo;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Converts this bullet info to a text props object
		/// </summary>
		/// <param name="styleProps">The style props</param>
		/// ------------------------------------------------------------------------------------
		public void ConvertAsTextProps(ITsPropsBldr styleProps)
		{
			if (styleProps == null)
				return;

			styleProps.SetIntPropValues((int)FwTextPropType.ktptBulNumScheme,
				(int)FwTextPropVar.ktpvEnum, (int)m_numberScheme);

			if (m_numberScheme == VwBulNum.kvbnNone)
			{	// no bullets
				styleProps.SetIntPropValues((int)FwTextPropType.ktptBulNumStartAt,
					-1, -1);
				styleProps.SetStrPropValue((int)FwTextPropType.ktptBulNumTxtBef, null);
				styleProps.SetStrPropValue((int)FwTextPropType.ktptBulNumTxtAft, null);
			}
			else if ((int)m_numberScheme >= (int)VwBulNum.kvbnBulletBase)
			{
				// bullets
				styleProps.SetIntPropValues((int)FwTextPropType.ktptBulNumStartAt,
					-1, -1);
				styleProps.SetStrPropValue((int)FwTextPropType.ktptBulNumTxtBef, null);
				styleProps.SetStrPropValue((int)FwTextPropType.ktptBulNumTxtAft, null);
				styleProps.SetStrPropValue((int)FwTextPropType.ktptBulNumFontInfo, EncodedFontInfo);
			}
			else
			{
				// numbered
				styleProps.SetIntPropValues((int)FwTextPropType.ktptBulNumStartAt,
					(int)FwTextPropVar.ktpvDefault, m_start);
				styleProps.SetStrPropValue((int)FwTextPropType.ktptBulNumTxtBef, m_textBefore);
				styleProps.SetStrPropValue((int)FwTextPropType.ktptBulNumTxtAft, m_textAfter);
				styleProps.SetStrPropValue((int)FwTextPropType.ktptBulNumFontInfo, EncodedFontInfo);
			}
		}
	}
}
