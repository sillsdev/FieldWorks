// Copyright (c) 2006-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: StyleInfo.cs
// Responsibility: TE Team

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Resources;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.FwCoreDlgControls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class StyleInfo : BaseStyleInfo
	{
		#region Data Members
		private bool m_dirty = false;
		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="StyleInfo"/> class, representing an
		/// existing style.
		/// </summary>
		/// <param name="style">An StStyle.</param>
		/// ------------------------------------------------------------------------------------
		public StyleInfo(IStStyle style)
			: base(style)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="StyleInfo"/> class. This constructor
		/// is used to make a copy of a style with a new name.
		/// </summary>
		/// <param name="copyFrom">The copy from.</param>
		/// <param name="newName">name for the new copied style</param>
		/// ------------------------------------------------------------------------------------
		public StyleInfo(BaseStyleInfo copyFrom, string newName): base(copyFrom, newName)
		{
			m_dirty = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="StyleInfo"/> class. Creates a new
		/// style of a given type.
		/// </summary>
		/// <param name="name">new style name</param>
		/// <param name="basedOnStyle">style to be based on</param>
		/// <param name="styleType"></param>
		/// <param name="cache">FDO cache</param>
		/// ------------------------------------------------------------------------------------
		public StyleInfo(string name, StyleInfo basedOnStyle, StyleType styleType,
			FdoCache cache) : base(cache)
		{
			Name = name;
			m_styleType = styleType;
				m_basedOnStyle = basedOnStyle;
			if (basedOnStyle != null)
				m_basedOnStyleName = basedOnStyle.Name;
			else if (styleType == StyleType.kstParagraph)
			{
				throw new ArgumentNullException("basedOnStyle",
					"New paragraph styles are required to be based on an existing style.");
			}
			m_nextStyleName = name;
		}
		#endregion

		#region Properties for Inheritable access
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the bullet information
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public InheritableStyleProp<BulletInfo> IBullet
		{
			get { return m_bulletInfo; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the border thickness.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public InheritableStyleProp<BorderThicknesses> BorderThickness
		{
			get { return m_border; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Gets the paragraph direction</summary>
		/// ------------------------------------------------------------------------------------
		public InheritableStyleProp<TriStateBool> IRightToLeftStyle
		{
			get { return m_rtl; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Gets the alignment</summary>
		/// ------------------------------------------------------------------------------------
		public InheritableStyleProp<FwTextAlign> IAlignment
		{
			get { return m_alignment; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Gets the inter-line spacing in millipoints</summary>
		/// ------------------------------------------------------------------------------------
		public InheritableStyleProp<LineHeightInfo> ILineSpacing
		{
			get { return m_lineSpacing; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Gets the space above paragraph in millipoints</summary>
		/// ------------------------------------------------------------------------------------
		public InheritableStyleProp<int> ISpaceBefore
		{
			get { return m_spaceBefore; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Gets the space below paragraph in millipoints</summary>
		/// ------------------------------------------------------------------------------------
		public InheritableStyleProp<int> ISpaceAfter
		{
			get { return m_spaceAfter; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Gets the indentation of first line in millipoints</summary>
		/// ------------------------------------------------------------------------------------
		public InheritableStyleProp<int> IFirstLineIndent
		{
			get { return m_firstLineIndent; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Gets the indentation of paragraph from leading edge in millipoints</summary>
		/// ------------------------------------------------------------------------------------
		public InheritableStyleProp<int> ILeadingIndent
		{
			get { return m_leadingIndent; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Gets the indentation of paragraph from trailing edge in millipoints</summary>
		/// ------------------------------------------------------------------------------------
		public InheritableStyleProp<int> ITrailingIndent
		{
			get { return m_trailingIndent; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Gets the ARGB Color of borders</summary>
		/// ------------------------------------------------------------------------------------
		public InheritableStyleProp<Color> IBorderColor
		{
			get { return m_borderColor; }
		}
		#endregion

		#region ToString and helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Formats a string representing the style information.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return ToString(true, MsrSysType.Cm);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Formats a string representing the style information.
		/// </summary>
		/// <param name="useBiDiLabels">if set to <c>true</c> use labels suitable for describing
		/// the style's behavior when applied to bi-directional text.</param>
		/// <param name="userMeasurementType">User's preferred measurement units for
		/// indentation.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public string ToString(bool useBiDiLabels, MsrSysType userMeasurementType)
		{
			StringBuilder text = new StringBuilder();
			if (BasedOnStyle != null)
				text.Append(BasedOnStyle.Name + " + ");

			// Add default font information
			bool fForceMinimumDescription = IsParagraphStyle && !Inherits;
			AddDefaultFontInfo(text, fForceMinimumDescription);

			// Add paragraph information
			AppendParagraphInfo(text, useBiDiLabels, userMeasurementType);

			// Add bullet information
			AddBulletInfo(text);

			// Add border information
			AddBorderInfo(text, useBiDiLabels);

			// Add writing system specific information
			AddWsSpecificFontInfo(text);

			return text.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the width of the border. (Actually takes the max width since they can
		/// theoretically be different -- but they never are becaue UI doesn't allow it.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int BorderWidth
		{
			get { return Math.Max(Math.Max(BorderTop, BorderBottom), Math.Max(BorderLeading, BorderTrailing)); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the border info to the style description string
		/// </summary>
		/// <param name="text">The description string being built.</param>
		/// <param name="useBiDiLabels">if set to <c>true</c> use labels suitable for describing
		/// the style's behavior when applied to bi-directional text.</param>
		/// ------------------------------------------------------------------------------------
		private void AddBorderInfo(StringBuilder text, bool useBiDiLabels)
		{
			if (!m_border.IsExplicit && !m_borderColor.IsExplicit)
				return;

			int width = BorderWidth;
			if (width == 0)
			{
				AppendItem(text, FwCoreDlgControls.kstidNoBorder);
				return;
			}

			string sWidth = ((double)width / 1000).ToString("0.##");
			string sDetails;
			if (m_borderColor.IsExplicit)
				sDetails = string.Format(FwCoreDlgControls.kstidBorderDetailsWithColor,
					ColorUtil.ColorToName(m_borderColor.Value), sWidth);
			else
				sDetails = string.Format(FwCoreDlgControls.kstidBorderDetailsWithoutColor, sWidth);

			StringBuilder sbBorders = new StringBuilder();
			if (0 < Math.Min(Math.Min(BorderTop, BorderBottom), Math.Min(BorderLeading, BorderTrailing)))
				sbBorders.Append(FwCoreDlgControls.kstidAllBorders);
			else
			{
				if (BorderTop > 0)
					sbBorders.Append(FwCoreDlgControls.kstidTopBorder);
				if (BorderBottom > 0)
					AppendItem(sbBorders, FwCoreDlgControls.kstidBottomBorder);
				if (BorderLeading > 0)
					AppendItem(sbBorders, (useBiDiLabels) ? FwCoreDlgControls.ksLeading : FwCoreDlgControls.ksLeft);
				if (BorderTrailing > 0)
					AppendItem(sbBorders, (useBiDiLabels) ? FwCoreDlgControls.ksTrailing : FwCoreDlgControls.ksRight);
			}
			AppendItem(text, string.Format(FwCoreDlgControls.kstidBorder, sbBorders), sDetails);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the bullet info to the style description string
		/// </summary>
		/// <param name="text">The text.</param>
		/// ------------------------------------------------------------------------------------
		private void AddBulletInfo(StringBuilder text)
		{
			if (m_bulletInfo.IsExplicit)
			{
				StringBuilder tmp = new StringBuilder();

				if (m_bulletInfo.Value.m_numberScheme == VwBulNum.kvbnNone)
					AppendItem(text, FwCoreDlgControls.kstidNoBulletsorNumbering);
				else if (m_bulletInfo.Value.m_numberScheme >= VwBulNum.kvbnBulletBase)
				{
					AppendItem(text, FwCoreDlgControls.kstidBulleted);
					if ((int)m_bulletInfo.Value.m_numberScheme != (int)VwBulNum.kvbnBulletBase + 1)
					{
						int scheme = (int)m_bulletInfo.Value.m_numberScheme - (int)VwBulNum.kvbnBulletBase;
						AppendItem(tmp, FwCoreDlgControls.kstidSchema, scheme.ToString());
					}
					if (m_bulletInfo.Value.FontInfo.IsAnyExplicit)
						AppendItem(tmp, m_bulletInfo.Value.FontInfo.ToString());
				}
				else
				{
					AppendItem(text, FwCoreDlgControls.kstidNumbered);

					if (m_bulletInfo.Value.m_textBefore != string.Empty)
						AppendItem(tmp, FwCoreDlgControls.kstidTextBefore, m_bulletInfo.Value.m_textBefore);
					if (m_bulletInfo.Value.m_textAfter != string.Empty)
						AppendItem(tmp, FwCoreDlgControls.kstidTextAfter, m_bulletInfo.Value.m_textAfter);
					if (m_bulletInfo.Value.m_start != 1)
						AppendItem(tmp, FwCoreDlgControls.kstidStartAt, m_bulletInfo.Value.m_start.ToString());
					if (m_bulletInfo.Value.m_numberScheme != VwBulNum.kvbnArabic)
					{
						AppendItem(tmp, FwCoreDlgControls.kstidSchema,
							GetNumberSchemeNameForType(m_bulletInfo.Value.m_numberScheme));
					}
					if (m_bulletInfo.Value.FontInfo.IsAnyExplicit)
						AppendItem(tmp, m_bulletInfo.Value.FontInfo.ToString());
				}

				if (tmp.Length > 0)
				{
					text.Append(" (");
					text.Append(tmp);
					text.Append(")");
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the given type of (numeric) bullet.
		/// </summary>
		/// <param name="bulletType">Type of the bullet.</param>
		/// <returns>Name of the type</returns>
		/// ------------------------------------------------------------------------------------
		private string GetNumberSchemeNameForType(VwBulNum bulletType)
		{
			switch (bulletType)
			{
				case VwBulNum.kvbnRomanUpper: return "Uppercase Roman numbers";
				case VwBulNum.kvbnRomanLower: return "Lowercase Roman numbers";
				case VwBulNum.kvbnLetterUpper: return "Uppercase letters";
				case VwBulNum.kvbnLetterLower: return "Lowercase letters";
				case VwBulNum.kvbnArabic01: return "Zero-prefixed Arabic numbers";
				case VwBulNum.kvbnArabic:
				default:
					return "Arabic numbers";
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the ws specific font info to the style description
		/// </summary>
		/// <param name="text">The text description to append to</param>
		/// ------------------------------------------------------------------------------------
		private void AddWsSpecificFontInfo(StringBuilder text)
		{
			if (m_style == null)
				return;

			text.AppendLine();
			foreach (KeyValuePair<int, FontInfo> wsInfo in m_fontInfoOverrides)
			{
				if (wsInfo.Value.IsAnyExplicit)
				{
					var ws = m_style.Cache.ServiceLocator.WritingSystemManager.Get(wsInfo.Key);
					text.Append(ws.DisplayLabel);
					text.Append(": ");
					text.Append(wsInfo.Value.ToString());
					text.AppendLine();
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the default font info to the style description
		/// </summary>
		/// <param name="text">The text description to append to</param>
		/// <param name="fForceMinimumDescription">if set to <c>true</c> forces at least minimum
		/// description (i.e., font &amp; size) to be added.</param>
		/// ------------------------------------------------------------------------------------
		private void AddDefaultFontInfo(StringBuilder text, bool fForceMinimumDescription)
		{
			if (m_defaultFontInfo.IsAnyExplicit || fForceMinimumDescription)
				AppendItem(text, m_defaultFontInfo.ToString(fForceMinimumDescription));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the paragraph info to the style description
		/// </summary>
		/// <param name="text">The text description to append to</param>
		/// <param name="useBiDiLabels">if set to <c>true</c> use labels suitable for describing
		/// the style's behavior when applied to bi-directional text.</param>
		/// <param name="userMeasurementType">User's preferred measurement units for
		/// indentation.</param>
		/// ------------------------------------------------------------------------------------
		private void AppendParagraphInfo(StringBuilder text, bool useBiDiLabels,
			MsrSysType userMeasurementType)
		{
			if (m_rtl.IsExplicit)
				AppendRtl(text);

			if (m_alignment.IsExplicit)
				AppendAlignment(text);

			if (m_leadingIndent.IsExplicit)
			{
				AppendMeasure(text,
					(useBiDiLabels) ? FwCoreDlgControls.ksLeading : FwCoreDlgControls.ksLeft,
					userMeasurementType, m_leadingIndent.Value);
			}

			if (m_trailingIndent.IsExplicit)
			{
				AppendMeasure(text,
					(useBiDiLabels) ? FwCoreDlgControls.ksTrailing : FwCoreDlgControls.ksRight,
					userMeasurementType, m_trailingIndent.Value);
			}

			if (m_firstLineIndent.IsExplicit)
			{
				if (m_firstLineIndent.Value < 0)
					AppendMeasure(text, FwCoreDlgControls.ksFirstLineHanging, -m_firstLineIndent.Value);
				else if (m_firstLineIndent.Value > 0)
					AppendMeasure(text, FwCoreDlgControls.ksFirstLineIndent, m_firstLineIndent.Value);
				else
					AppendItem(text, FwCoreDlgControls.ksSpecialIndent, FwCoreDlgControls.ksNone);
			}

			if (m_spaceBefore.IsExplicit)
				AppendMeasure(text, FwCoreDlgControls.ksSpacingBefore, m_spaceBefore.Value);

			if (m_spaceAfter.IsExplicit)
				AppendMeasure(text, FwCoreDlgControls.ksSpacingAfter, m_spaceAfter.Value);

			if (m_lineSpacing.IsExplicit)
			{
				LineHeightInfo info = m_lineSpacing.Value;
				if (info.m_relative)
				{
					switch (info.m_lineHeight)
					{
						case 10000: AppendItem(text, FwCoreDlgControls.ksSingleSpacing); break;
						case 15000: AppendItem(text, FwCoreDlgControls.ksPlusSpacing); break;
						case 20000: AppendItem(text, FwCoreDlgControls.ksDoubleSpacing); break;
					}
				}
				else
				{
					if (info.m_lineHeight < 0)
						AppendMeasure(text, FwCoreDlgControls.ksExactSpacing, -info.m_lineHeight);
					else
						AppendMeasure(text, FwCoreDlgControls.ksAtLeastSpacing, info.m_lineHeight);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Append information about a measure value in points
		/// </summary>
		/// <param name="text">text string to append to</param>
		/// <param name="measureLabel">Label identifying what this measurement represents
		/// </param>
		/// <param name="value">measurement value (in millipoints) to write</param>
		/// ------------------------------------------------------------------------------------
		private void AppendMeasure(StringBuilder text, string measureLabel, int value)
		{
			AppendMeasure(text, measureLabel, MsrSysType.Point, value);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Append information about a measure value
		/// </summary>
		/// <param name="text">text string to append to</param>
		/// <param name="measureLabel">Label identifying what this measurement represents</param>
		/// <param name="msrType">Type representing the measurement units to which the value
		/// should be converted and with which it should be shown.</param>
		/// <param name="value">measurement value (in millipoints) to write</param>
		/// ------------------------------------------------------------------------------------
		private void AppendMeasure(StringBuilder text, string measureLabel, MsrSysType msrType,
			int value)
		{
			AppendItem(text, measureLabel, MeasurementUtils.FormatMeasurement(value, msrType));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Appends the RTL information
		/// </summary>
		/// <param name="text">The text.</param>
		/// ------------------------------------------------------------------------------------
		private void AppendRtl(StringBuilder text)
		{
			switch (m_rtl.Value)
			{
				case TriStateBool.triTrue:
					AppendItem(text, FwCoreDlgControls.ksRightToLeft); break;
				case TriStateBool.triFalse:
					AppendItem(text, FwCoreDlgControls.ksLeftToRight); break;
			}
		}

//		/// ------------------------------------------------------------------------------------
//		/// <summary>
//		/// Appends information about a color to the description string
//		/// </summary>
//		/// <param name="text">The text.</param>
//		/// <param name="colorName">the name of the color type</param>
//		/// <param name="color">The color.</param>
//		/// ------------------------------------------------------------------------------------
//		private void AppendColor(StringBuilder text, string colorName, Color color)
//		{
//			AppendItem(text, colorName, color.ToString());
//		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Appends the alignment information.
		/// </summary>
		/// <param name="text">The text.</param>
		/// ------------------------------------------------------------------------------------
		private void AppendAlignment(StringBuilder text)
		{
			switch (m_alignment.Value)
			{
				case FwTextAlign.ktalCenter:
					AppendItem(text, FwCoreDlgControls.ksCenter);
					break;
				case FwTextAlign.ktalJustify:
					AppendItem(text, FwCoreDlgControls.ksJustify);
					break;
				case FwTextAlign.ktalLeading:
					AppendItem(text, FwCoreDlgControls.ksLeading);
					break;
				case FwTextAlign.ktalLeft:
					AppendItem(text, FwCoreDlgControls.ksLeft);
					break;
				case FwTextAlign.ktalRight:
					AppendItem(text, FwCoreDlgControls.ksRight);
					break;
				case FwTextAlign.ktalTrailing:
					AppendItem(text, FwCoreDlgControls.ksTrailing);
					break;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Appends an item to the description string without a value
		/// </summary>
		/// <param name="text">description to append to</param>
		/// <param name="name">name of the item</param>
		/// ------------------------------------------------------------------------------------
		private void AppendItem(StringBuilder text, string name)
		{
			AppendItem(text, name, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Appends an item to the description string
		/// </summary>
		/// <param name="text">description to append to</param>
		/// <param name="name">name of the item</param>
		/// <param name="value">value, or null if none</param>
		/// ------------------------------------------------------------------------------------
		private void AppendItem(StringBuilder text, string name, string value)
		{
			if (text.Length > 2 && text[text.Length - 2] != '+')
				text.Append(FwCoreDlgControls.kstidItemSeparator);
			text.Append(name);
			if (value != null)
				text.Append(FwCoreDlgControls.kstidItemValueSeparator + value);
		}
		#endregion

		#region Methods for saving to database
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Saves the style information to the DB.
		/// </summary>
		/// <param name="style">The StStyle to save to.</param>
		/// <param name="existingStyle"><c>true</c> if the style exists; otherwise <c>false</c></param>
		/// <param name="isModified">if set to <c>true</c> the user has made changes to the
		/// properties of the style so that they may differ from the factory defaults
		/// (caller is not required to guarantee absolutely that the properties do in fact
		/// differ, since the user could set the properties back to the original values).</param>
		/// ------------------------------------------------------------------------------------
		public void SaveToDB(IStStyle style, bool existingStyle, bool isModified)
		{
			Debug.Assert(IsValid);

			m_style = style;
			style.Name = m_name;
			style.Usage.UserDefaultWritingSystem = Cache.TsStrFactory.MakeString(
				m_usage,
				Cache.ServiceLocator.WritingSystemManager.UserWs);
			style.Type = m_styleType;
			if (IsBuiltIn)
				style.IsModified = isModified;
			else
			{
				// We need to update the context, structure, and function to be what the base
				// style is. We only want to do this for user styles so we don't override what
				// is set in the stylesheet.
				// Go down the inheritance chain until we find a value to be based on.
				BaseStyleInfo basedOn = RealBasedOnStyleInfo;

				if (!existingStyle) // Never change the context, etc. for an existing style
				{
					if (basedOn != null)
					{
						// If the based-on style cannot be inherited from, then this style must
						// be a copy of another style, from which it will have inherited its
						// context, structure, and function.
						if (basedOn.CanInheritFrom)
						{
							m_context = basedOn.Context;
							m_structure = basedOn.Structure;
							m_function = basedOn.Function;
						}
					}
					else if (IsParagraphStyle)
						throw new ArgumentException("A user-defined paragraph style must have a real based-on style");
				}
			}
			style.Context = m_context;
			style.Structure = m_structure;
			style.Function = m_function;
			style.UserLevel = m_userLevel;

			// Build the text props
			ITsPropsBldr styleProps = TsPropsBldrClass.Create();

			if (m_defaultFontInfo.m_fontName.IsExplicit)
			{
				styleProps.SetStrPropValue((int)FwTextPropType.ktptFontFamily,
					m_defaultFontInfo.m_fontName.Value);
			}

			if (m_bulletInfo.IsExplicit)
				m_bulletInfo.Value.ConvertAsTextProps(styleProps);

			if (m_defaultFontInfo.m_bold.IsExplicit)
			{
				styleProps.SetIntPropValues((int)FwTextPropType.ktptBold,
					(int)FwTextPropVar.ktpvEnum,
					m_defaultFontInfo.m_bold.Value ?
					(int)FwTextToggleVal.kttvInvert :
					(int)FwTextToggleVal.kttvOff);
			}

			if (m_defaultFontInfo.m_italic.IsExplicit)
			{
				styleProps.SetIntPropValues((int)FwTextPropType.ktptItalic,
					(int)FwTextPropVar.ktpvEnum,
					m_defaultFontInfo.m_italic.Value ?
					(int)FwTextToggleVal.kttvInvert :
					(int)FwTextToggleVal.kttvOff);
			}

			if (m_defaultFontInfo.m_superSub.IsExplicit)
			{
				styleProps.SetIntPropValues((int)FwTextPropType.ktptSuperscript, 0,
					(int)m_defaultFontInfo.m_superSub.Value);
			}

			if (m_defaultFontInfo.m_fontSize.IsExplicit)
			{
				styleProps.SetIntPropValues((int)FwTextPropType.ktptFontSize, 0,
					m_defaultFontInfo.m_fontSize.Value);
			}

			if (m_defaultFontInfo.m_fontColor.IsExplicit)
			{
				styleProps.SetIntPropValues((int)FwTextPropType.ktptForeColor, 0,
					(int)ColorUtil.ConvertColorToBGR(m_defaultFontInfo.m_fontColor.Value));
			}

			if (m_defaultFontInfo.m_backColor.IsExplicit)
			{
				styleProps.SetIntPropValues((int)FwTextPropType.ktptBackColor, 0,
					(int)ColorUtil.ConvertColorToBGR(m_defaultFontInfo.m_backColor.Value));
			}

			if (m_defaultFontInfo.m_offset.IsExplicit)
			{
				styleProps.SetIntPropValues((int)FwTextPropType.ktptOffset,
					(int)FwTextPropVar.ktpvMilliPoint,
					m_defaultFontInfo.m_offset.Value);
			}

			if (m_defaultFontInfo.m_underline.IsExplicit)
			{
				styleProps.SetIntPropValues((int)FwTextPropType.ktptUnderline, 0,
					(int)m_defaultFontInfo.m_underline.Value);
			}

			if (m_defaultFontInfo.m_underlineColor.IsExplicit)
			{
				styleProps.SetIntPropValues((int)FwTextPropType.ktptUnderColor, 0,
					(int)ColorUtil.ConvertColorToBGR(m_defaultFontInfo.m_underlineColor.Value));
			}

			if (m_defaultFontInfo.m_features.IsExplicit)
			{
				styleProps.SetStrPropValue((int)FwTextPropType.ktptFontVariations,
					m_defaultFontInfo.m_features.Value);
			}

			if (m_rtl.IsExplicit && m_rtl.Value != TriStateBool.triNotSet)
			{
				styleProps.SetIntPropValues((int)FwTextPropType.ktptRightToLeft, 0,
					(m_rtl.Value == TriStateBool.triTrue) ? 1 : 0);
			}

			if (m_alignment.IsExplicit)
			{
				styleProps.SetIntPropValues((int)FwTextPropType.ktptAlign,
					(int)FwTextPropVar.ktpvEnum, (int)m_alignment.Value);
			}

			if (m_spaceBefore.IsExplicit)
			{
				styleProps.SetIntPropValues((int)FwTextPropType.ktptSpaceBefore, 0,
					m_spaceBefore.Value);
			}

			if (m_spaceAfter.IsExplicit)
			{
				styleProps.SetIntPropValues((int)FwTextPropType.ktptSpaceAfter, 0,
					m_spaceAfter.Value);
			}

			if (m_firstLineIndent.IsExplicit)
			{
				styleProps.SetIntPropValues((int)FwTextPropType.ktptFirstIndent, 0,
					m_firstLineIndent.Value);
			}

			if (m_leadingIndent.IsExplicit)
			{
				styleProps.SetIntPropValues((int)FwTextPropType.ktptLeadingIndent, 0,
					m_leadingIndent.Value);
			}

			if (m_trailingIndent.IsExplicit)
			{
				styleProps.SetIntPropValues((int)FwTextPropType.ktptTrailingIndent, 0,
					m_trailingIndent.Value);
			}

			if (m_lineSpacing.IsExplicit)
			{
				styleProps.SetIntPropValues((int)FwTextPropType.ktptLineHeight,
					m_lineSpacing.Value.m_relative ?
					(int)FwTextPropVar.ktpvRelative : (int)FwTextPropVar.ktpvMilliPoint,
					m_lineSpacing.Value.m_lineHeight);
			}

			if (m_border.IsExplicit)
			{
				styleProps.SetIntPropValues((int)FwTextPropType.ktptBorderTop,
					0, m_border.Value.Top);
				styleProps.SetIntPropValues((int)FwTextPropType.ktptBorderBottom,
					0, m_border.Value.Bottom);
				styleProps.SetIntPropValues((int)FwTextPropType.ktptBorderLeading,
					0, m_border.Value.Leading);
				styleProps.SetIntPropValues((int)FwTextPropType.ktptBorderTrailing,
					0, m_border.Value.Trailing);
			}

			if (m_borderColor.IsExplicit)
			{
				styleProps.SetIntPropValues((int)FwTextPropType.ktptBorderColor, 0,
					(int)ColorUtil.ConvertColorToBGR(m_borderColor.Value));
			}

			if (m_keepWithNext.IsExplicit)
			{
				styleProps.SetIntPropValues((int)FwTextPropType.ktptKeepWithNext, 0,
					(m_keepWithNext.Value) ? 1 : 0);
			}

			if (m_keepTogether.IsExplicit)
			{
				styleProps.SetIntPropValues((int)FwTextPropType.ktptKeepTogether, 0,
					(m_keepTogether.Value) ? 1 : 0);
			}

			if (m_widowOrphanControl.IsExplicit)
			{
				styleProps.SetIntPropValues((int)FwTextPropType.ktptWidowOrphanControl, 0,
					(m_widowOrphanControl.Value) ? 1 : 0);
			}

			SaveFontOverridesToBuilder(m_fontInfoOverrides, styleProps);

			style.Rules = styleProps.GetTextProps();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Saves the based on and following styles for the styleinfo to the database
		/// Only set the BasedOn/Next style if valid. Otherwise, changes to the BasedOn style
		/// or Next style should be ignored.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SaveBasedOnAndFollowingToDB()
		{
			Debug.Assert(m_style != null);
			Debug.Assert(IsValid);
			if (m_basedOnStyle != null)
			{
				if (m_basedOnStyle.IsValid)
					m_style.BasedOnRA = m_basedOnStyle.RealStyle;
			}
			else
				m_style.BasedOnRA = null;

			if (m_nextStyle != null)
			{
				if (m_nextStyle.IsValid)
					m_style.NextRA = m_nextStyle.RealStyle;
			}
			else
				m_style.NextRA = null;
		}

//		/// ------------------------------------------------------------------------------------
//		/// <summary>
//		/// Saves a boolean property value to a props builder that will go to the database.
//		/// </summary>
//		/// <param name="props">props builder</param>
//		/// <param name="boolProp">The boolean property to write</param>
//		/// <param name="propTag">The property tag</param>
//		/// ------------------------------------------------------------------------------------
//		private void SaveBoolProp(ITsPropsBldr props, InheritableStyleProp<bool> boolProp,
//			int propTag)
//		{
//			if (!boolProp.IsInherited)
//				props.SetIntPropValues(propTag, 0, boolProp.Value ? 1 : 0);
//		}
		#endregion

		#region Other public properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether this StyleInfo has changes that have not
		/// been saved to the underlying style objects.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool Dirty
		{
			get { return m_dirty; }
			set
			{
				m_dirty = value;
				m_isModified |= value;
			}
		}
		#endregion

		#region Public setters
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Saves the name.
		/// </summary>
		/// <param name="name">The name.</param>
		/// ------------------------------------------------------------------------------------
		public void SaveName(string name)
		{
			if (name != m_name)
				m_dirty = true;
			m_name = name;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Saves the based on style.
		/// </summary>
		/// <param name="basedOnStyle">The based on style.</param>
		/// ------------------------------------------------------------------------------------
		public void SaveBasedOn(string basedOnStyle)
		{
			if (basedOnStyle == ResourceHelper.DefaultParaCharsStyleName || basedOnStyle == string.Empty)
				basedOnStyle = null;

			if (basedOnStyle != m_basedOnStyleName)
				m_dirty = true;
			m_basedOnStyleName = basedOnStyle;
			if (basedOnStyle == null)
				m_basedOnStyle = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Saves the following style.
		/// </summary>
		/// <param name="followingStyle">The following style.</param>
		/// ------------------------------------------------------------------------------------
		public void SaveFollowing(string followingStyle)
		{
			if (followingStyle == ResourceHelper.DefaultParaCharsStyleName || followingStyle == string.Empty)
				followingStyle = null;

			if (followingStyle != m_nextStyleName)
				m_dirty = true;
			m_nextStyleName = followingStyle;
			if (followingStyle == null)
				m_nextStyle = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Saves the usage description.
		/// </summary>
		/// <param name="usage">The usage string.</param>
		/// ------------------------------------------------------------------------------------
		public void SaveDescription(string usage)
		{
			if (usage != m_usage)
				m_dirty = true;
			m_usage = usage;
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets all properties to inherited.
		/// </summary>
		/// <param name="SetFactoryPropertyValues">Delegate to retrieve and set any property
		/// values that have non-default values specified in the factory stylesheet.</param>
		/// ------------------------------------------------------------------------------------
		public void ResetAllPropertiesToFactoryValues(Action SetFactoryPropertyValues)
		{
			SetAllPropertiesToInherited();
			SetFactoryPropertyValues();
			m_isModified = false;
			m_dirty = true;
		}
	}
}
