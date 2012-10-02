
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.Utils;

namespace SIL.FieldWorks.SharpViews
{
	/// <summary>
	/// AssembledStyles (roughly equivalent to the old VwPropertyStore) represents the results of applying various styles (and explicit formatting).
	/// It is designed to be especially efficient when the same properties are independently derived for different parts of the display; for example,
	/// if parts of several lexical entries are independently marked bold, only a single AssembledStyles object is created to represent the effect
	/// of adding 'Bold' to whatever the context AssembledStyles was.
	/// Enhance JohnT: this is only a skeleton at present, it eventually needs most if not all the properties in VwPropertyStore.
	/// </summary>
	public class AssembledStyles
	{
		private AssembledStylesCache m_styleCache;
		private LgCharRenderProps m_chrp;
		private Color m_borderColor;
		private Thickness m_margins;
		private Thickness m_pads;
		private Thickness m_borders;
		private int m_lineHeight;
		// The name of the most recently applied style, if any.
		private string m_styleName;
		/// <summary>
		/// This is probably yagni, only m_chrp.ttvBold is currently really used by anything.
		/// But I wanted to match the functionality of the old VwPropertyStore at this level at least.
		/// </summary>
		private int m_weight;

		/// <summary>
		/// The font name which is recognized by writing system factories as indicating the default font for the
		/// writing system.
		/// </summary>
		public const string DefaultFontName = "<default font>";

		/// <summary>
		/// A set of the properties of interest for rendering text, in a form suitable for our rendering engines.
		/// </summary>
		public LgCharRenderProps Chrp { get { return m_chrp; }}

		public static string FaceNameFromChrp(LgCharRenderProps chrp)
		{
			return MarshalEx.UShortToString(chrp.szFaceName).Trim();
		}

		public int FontWeight
		{
			get { return m_weight; }
			private set
			{
				m_weight = value;
				m_chrp.ttvBold = m_weight > 400 ? (int)FwTextToggleVal.kttvForceOn : (int)FwTextToggleVal.kttvOff;
			}
		}

		public AssembledStyles WithFontWeight(int weight)
		{
			return m_styleCache.GetDerivedStyle(this, (int) FwTextPropType.ktptBold, weight,
												(newStyles, w) => newStyles.FontWeight = weight);
		}

		public bool FontItalic { get { return m_chrp.ttvItalic != (int)FwTextToggleVal.kttvOff; } }
		public AssembledStyles WithFontItalic(FwTextToggleVal ttv)
		{
			int val = (int) ttv;
			if (ttv == FwTextToggleVal.kttvInvert)
				val = FontItalic ? (int) FwTextToggleVal.kttvOff : (int) FwTextToggleVal.kttvForceOn;
			return m_styleCache.GetDerivedStyle(this, (int)FwTextPropType.ktptItalic, val,
												(newStyles, w) => newStyles.m_chrp.ttvItalic = val);
		}

		public AssembledStyles WithFontBold(FwTextToggleVal ttv)
		{
			int val = (int)ttv;
			if (ttv == FwTextToggleVal.kttvInvert)
				val = m_chrp.ttvBold == (int)FwTextToggleVal.kttvForceOn ? (int)FwTextToggleVal.kttvOff : (int)FwTextToggleVal.kttvForceOn;
			var weight = (val == (int) FwTextToggleVal.kttvForceOn) ? (int)VwFontWeight.kvfwBold : (int)VwFontWeight.kvfwNormal;
			return m_styleCache.GetDerivedStyle(this, (int)FwTextPropType.ktptBold, weight,
												(newStyles, w) => newStyles.FontWeight = weight);
		}
		public int Ws { get { return m_chrp.ws; } }
		public AssembledStyles WithWs(int ws)
		{
			// Todo JohnT: to handle the case of a paragraph style that sets ws-specific overrides
			// which can't be applied until we know the WS of a run within the paragraph,
			// we will have to have WithNamedStyle record the style name, and this routine must
			// check whether a named style is recorded, and if so, apply also any overrides
			// specified for this WS.
			return m_styleCache.GetDerivedStyle(this, (int)FwTextPropType.ktptWs, ws,
												(newStyles, w) => newStyles.m_chrp.ws = ws);
		}

		/// <summary>
		/// Todo JohnT: implement this.
		/// </summary>
		public bool RightToLeft { get { return false;}}

		/// <summary>
		/// Height in millipoints.
		/// </summary>

		public int FontSize { get { return m_chrp.dympHeight; } }
		public AssembledStyles WithFontSize(int mp)
		{
			return m_styleCache.GetDerivedStyle(this, (int)FwTextPropType.ktptFontSize, mp,
												(newStyles, w) => newStyles.m_chrp.dympHeight = mp);
		}

		public string FaceName
		{
			get { return MarshalEx.UShortToString(m_chrp.szFaceName); }
		}
		public AssembledStyles WithFaceName(string faceName)
		{
			return m_styleCache.GetDerivedStyle(this, (int)FwTextPropType.ktptFontFamily, faceName,
												(newStyles, w) => newStyles.SetFaceName(faceName));
		}

		private void SetFaceName(string fontName)
		{
			SetFaceName(ref m_chrp, fontName);
		}

		internal static void SetFaceName(ref LgCharRenderProps chrp, string fontName)
		{
			chrp.szFaceName = new ushort[32];
			MarshalEx.StringToUShort(fontName, chrp.szFaceName);
		}

		public AssembledStyles WithNamedStyle(string styleName)
		{
			return m_styleCache.GetDerivedStyle(this, (int)FwTextPropType.ktptNamedStyle, styleName,
												(newStyles, w) => newStyles.StyleName = styleName);
		}

		public string StyleName
		{
			get
			{
				return m_styleName;
			}
			private set
			{
				m_styleName = value;
				PropSetter effects;
				var stylesheet = m_styleCache.Stylesheet;
				if (stylesheet == null)
					return; // new AS will be unchanged and discarded, but old one will be cached for style name.
				var style = stylesheet.Style(value);
				if (style == null)
					return;
				ApplyCharacterStyleInfo(style.DefaultCharacterStyleInfo);
				// if WS is known apply overrides.
				if (Ws != 0)
					ApplyCharacterStyleInfo(style.OverrideCharacterStyleInfo(Ws));
				// Todo: check other properties of style (if a paragraph style)
				// Todo: to handle the special case of a paragraph style that has WS-specific overrides
				// that can't be applied until we know the WS of a run within the paragraph, we will need
				// to remember the name of the style.
			}
		}

		internal IStylesheet Stylesheet
		{
			get { return m_styleCache.Stylesheet; }
		}

		private void ApplyCharacterStyleInfo(ICharacterStyleInfo fontInfo)
		{
			if (fontInfo == null)
				return;
			if (fontInfo.Bold != null && fontInfo.Bold.ValueIsSet)
				FontWeight = fontInfo.Bold.Value ? (int) VwFontWeight.kvfwBold : (int) VwFontWeight.kvfwNormal;
			if (fontInfo.FontSize != null && fontInfo.FontSize.ValueIsSet)
				m_chrp.dympHeight = fontInfo.FontSize.Value;
			if (fontInfo.BackColor != null && fontInfo.BackColor.ValueIsSet)
				m_chrp.clrBack = ColorUtil.ConvertColorToBGR(fontInfo.BackColor.Value);
			if (fontInfo.FontColor != null && fontInfo.FontColor.ValueIsSet)
				m_chrp.clrFore = ColorUtil.ConvertColorToBGR(fontInfo.FontColor.Value);
			if (fontInfo.Italic != null && fontInfo.Italic.ValueIsSet)
				m_chrp.ttvItalic = fontInfo.Italic.Value ? (int)FwTextToggleVal.kttvForceOn : (int)FwTextToggleVal.kttvOff;
			if (fontInfo.Offset != null && fontInfo.Offset.ValueIsSet)
				m_chrp.dympOffset = fontInfo.Offset.Value;
			if (fontInfo.UnderlineColor != null && fontInfo.UnderlineColor.ValueIsSet)
				m_chrp.clrUnder = ColorUtil.ConvertColorToBGR(fontInfo.UnderlineColor.Value);
			if (fontInfo.FontName != null && fontInfo.FontName.ValueIsSet)
				SetFaceName(fontInfo.FontName.Value);
			if (fontInfo.Underline != null && fontInfo.Underline.ValueIsSet)
				m_chrp.unt = (int)fontInfo.Underline.Value;
			// Todo: supersub, font features
		}


		/// <summary>
		/// Distance in millipoints that the baseline of the text having this property is raised
		/// (or lowered if negative) above normal text.
		/// </summary>
		public int BaselineOffset { get { return m_chrp.dympOffset; } }
		public AssembledStyles WithBaselineOffset(int mp)
		{
			return m_styleCache.GetDerivedStyle(this, (int)FwTextPropType.ktptOffset, mp,
												(newStyles, w) => newStyles.m_chrp.dympOffset = mp);
		}

		/// <summary>
		/// Distance in millipoints from the baseline of one line of text to the baseline of the next,
		/// within a paragraph. If positive, it is a minimum; if negative, it is exact.
		/// </summary>
		public int LineHeight { get { return m_lineHeight; } }
		public AssembledStyles WithLineHeight(int mp)
		{
			return m_styleCache.GetDerivedStyle(this, (int)FwTextPropType.ktptLineHeight, mp,
												(newStyles, w) => newStyles.m_lineHeight = mp);
		}

		/// <summary>
		/// The kind of underlining the text has (in black by default, or UnderlineColor).
		/// </summary>
		public FwUnderlineType Underline { get { return (FwUnderlineType)m_chrp.unt; } }
		public AssembledStyles WithUnderline(FwUnderlineType underlineType)
		{
			return m_styleCache.GetDerivedStyle(this, (int)FwTextPropType.ktptUnderline, underlineType,
												(newStyles, w) => newStyles.m_chrp.unt = (int)underlineType);
		}
		/// <summary>
		/// Foreground (text glyphs, lines except borders and underline) color.
		/// </summary>
		public Color ForeColor { get { return ColorUtil.ConvertBGRtoColor(m_chrp.clrFore); } }

		public AssembledStyles WithForeColor(Color foreColor)
		{
			return m_styleCache.GetDerivedStyle(this, (int)FwTextPropType.ktptForeColor, ColorUtil.ConvertColorToBGR(foreColor),
												(newStyles, w) => newStyles.m_chrp.clrFore = ColorUtil.ConvertColorToBGR(foreColor));
		}

		/// <summary>
		/// Underline color.
		/// </summary>
		public Color UnderlineColor { get { return ColorUtil.ConvertBGRtoColor(m_chrp.clrUnder); } }

		public AssembledStyles WithUnderlineColor(Color clrUnder)
		{
			return m_styleCache.GetDerivedStyle(this, (int)FwTextPropType.ktptUnderColor, ColorUtil.ConvertColorToBGR(clrUnder),
												(newStyles, w) => newStyles.m_chrp.clrUnder = ColorUtil.ConvertColorToBGR(clrUnder));
		}
		/// <summary>
		/// Foreground (text glyphs, line) color.
		/// </summary>
		public Color BorderColor { get { return m_borderColor; } }

		public AssembledStyles WithBorderColor(Color borderColor)
		{
			return m_styleCache.GetDerivedStyle(this, (int)FwTextPropType.ktptBorderColor, borderColor.ToArgb(),
												(newStyles, w) => newStyles.m_borderColor = borderColor);
		}
		/// <summary>
		/// Margins represents white (actually color of containing box) space outside any border of the box.
		/// Top and Bottom margins of adjacent boxes in a pile may overlap.
		/// </summary>
		public Thickness Margins { get { return m_margins; } }

		/// <summary>
		/// Return a new styles (or existing canonical one) like the current one but with the specified margins.
		/// </summary>
		public AssembledStyles WithMargins(Thickness margins)
		{
			// It's arbitrary which of the ktptMargin constants we use.
			return m_styleCache.GetDerivedStyle(this, (int)FwTextPropType.ktptMarginLeading, margins,
												(newStyles, w) => newStyles.m_margins = margins);
		}

		/// <summary>
		/// Pads represents space inside any border of the box, drawn in the box's own backcolor if that
		/// is not transparent.
		/// </summary>
		public Thickness Pads { get { return m_pads; } }

		/// <summary>
		/// Return a new styles (or existing canonical one) like the current one but with the specified pads.
		/// </summary>
		public AssembledStyles WithPads(Thickness pads)
		{
			// It's arbitrary which of the ktptMargin constants we use.
			return m_styleCache.GetDerivedStyle(this, (int)FwTextPropType.ktptPadLeading, pads,
												(newStyles, w) => newStyles.m_pads = pads);
		}

		/// <summary>
		/// Borders represents the thickness of any borders a box has.
		/// </summary>
		public Thickness Borders { get { return m_borders; } }

		/// <summary>
		/// Return a new styles (or existing canonical one) like the current one but with the specified margins.
		/// </summary>
		public AssembledStyles WithBorders(Thickness borders)
		{
			// It's arbitrary which of the ktptMargin constants we use.
			return m_styleCache.GetDerivedStyle(this, (int)FwTextPropType.ktptBorderLeading, borders,
												(newStyles, w) => newStyles.m_borders = borders);
		}
		/// <summary>
		/// Background (behind text, fill solids, fill boxes) color.
		/// </summary>
		public Color BackColor { get { return ColorUtil.ConvertBGRtoColor(m_chrp.clrBack); } }

		public AssembledStyles WithBackColor(Color backColor)
		{
			return m_styleCache.GetDerivedStyle(this, (int)FwTextPropType.ktptBackColor, ColorUtil.ConvertColorToBGR(backColor),
												(newStyles, w) => newStyles.m_chrp.clrBack = ColorUtil.ConvertColorToBGR(backColor));
		}


		internal AssembledStyles CanonicalInstance(AssembledStyles astyles)
		{
			return m_styleCache.CanonicalInstance(astyles);
		}

		/// <summary>
		/// Default constructor sets initial state
		/// </summary>
		public AssembledStyles()
			:this((IStylesheet)null)
		{

		}
		/// <summary>
		/// Default constructor sets initial state
		/// </summary>
		public AssembledStyles(IStylesheet stylesheet)
		{
			m_styleCache = new AssembledStylesCache(stylesheet);
			FontWeight = (int)VwFontWeight.kvfwNormal;
			m_chrp.dympHeight = 10000; // default 10 pt
			SetFaceName(DefaultFontName);
			m_chrp.clrFore = ColorUtil.ConvertColorToBGR(Color.Black);
			m_chrp.clrUnder = m_chrp.clrFore;
			m_borderColor = Color.Black;
			m_chrp.clrBack = ColorUtil.ConvertColorToBGR(Color.Transparent);
			SetNonInheritedDefaults();
			m_styleCache.m_canonicalStyles[this] = this; // AFTER setting all props!!
		}

		void SetNonInheritedDefaults()
		{
			// It's better for nested boxes to be treated as transparent, then we don't waste time
			// repainting their background if it is the same.
			int clrTrans = (int)FwTextColor.kclrTransparent;
			m_chrp.clrBack = (uint)clrTrans;
			m_margins = Thickness.Default;
			m_pads = Thickness.Default;
			m_borders = Thickness.Default;
		}
		/// <summary>
		/// Intended for use only by AssembledStylesCache to make derived styles.
		/// </summary>
		/// <param name="basedOn"></param>
		internal AssembledStyles(AssembledStyles basedOn) : this(basedOn, false)
		{}

		/// <summary>
		/// Intended for use only by AssembledStylesCache to make derived styles.
		/// If inheritedOnly is true, only those properties are copied which should be inherited by
		/// nested boxes.
		/// </summary>
		internal AssembledStyles(AssembledStyles basedOn, bool inheritedOnly)
		{
			m_styleCache = basedOn.m_styleCache; // all derived styles share it
			m_chrp = basedOn.m_chrp;
			m_lineHeight = basedOn.m_lineHeight;
			m_styleName = basedOn.m_styleName;
			if (inheritedOnly)
			{
				SetNonInheritedDefaults();
			}
			else
			{
				// copy everything else, too.
				m_margins = basedOn.m_margins;
				m_borders = basedOn.m_borders;
				m_pads = basedOn.m_pads;
				m_borderColor = basedOn.m_borderColor;
				m_weight = basedOn.m_weight;
			}
		}

		internal AssembledStyles(AssembledStyles basedOn, PropSetter setter) : this(basedOn)
		{
			var impl = (PropSetterImpl) setter;
			impl.SetProperty(this);
		}

		public override bool Equals(object obj)
		{
			AssembledStyles other = obj as AssembledStyles;
			return Equals(other);
		}

		public bool Equals(AssembledStyles other)
		{
			if (other == null)
				return false;
			LgCharRenderProps chrp = other.m_chrp;
			return other.FontWeight == FontWeight
					&& m_chrp.ttvItalic == chrp.ttvItalic
					&& m_chrp.clrFore == chrp.clrFore
					&& m_chrp.clrBack == chrp.clrBack
					&& m_chrp.clrUnder == chrp.clrUnder
				   && m_chrp.ws == chrp.ws
				   && m_chrp.dympHeight == chrp.dympHeight
				   && m_chrp.dympOffset == chrp.dympOffset
				   && m_chrp.unt == chrp.unt
				   && other.m_borderColor == m_borderColor
				   && other.m_margins == m_margins
				   && other.m_pads == m_pads
				   && other.m_borders == m_borders
				   && other.FaceName == FaceName
				   && other.m_lineHeight == m_lineHeight
					&& other.m_styleName == m_styleName;
	   }

		public override int GetHashCode()
		{
			return FontWeight ^ m_chrp.ttvItalic ^ (int)m_chrp.clrFore ^ (int)m_chrp.clrBack ^ (int)m_chrp.clrUnder ^ m_chrp.ws
				^ m_chrp.dympHeight ^ m_chrp.dympOffset ^ m_chrp.unt
				^ m_borderColor.ToArgb() ^ m_margins.GetHashCode() ^ m_pads.GetHashCode()
				^ m_borders.GetHashCode()
				^ FaceName.GetHashCode()
				^ m_lineHeight
				^ (m_styleName == null ? 0 : m_styleName.GetHashCode());
		}

		/// <summary>
		/// Compute the assembled styles that results from applying the properties specified in the text props to this.
		/// We might start with a root assembled styles that says to use a 10-point font,
		/// then apply a paragraph style which says to use a 12-point font, except for French use 14-point.
		/// Then in another text props we may tell it the writing system is French, and must get 14-point as the
		/// result. Or, in a single TsTextProps, we may tell it the WS is French and to apply a character
		/// style which says to use 16-point, except for French 18-point; the result needs to be 18-point.
		/// It's also theoretically possible that the same text props again says directly to use 20-point; that
		/// should win over all the others.
		/// We achieve most of this by simply looking for the ws, then the named style, then everything else
		/// (and when we process a style, if we already know a ws we include the overrides for that ws).
		/// However, when we process the paragraph style, we don't know what ws a run in that paragraph will have.
		/// </summary>
		/// <param name="props"></param>
		/// <returns></returns>
		public AssembledStyles ApplyTextProps(ITsTextProps props)
		{
			AssembledStyles result = this;
			// Apply writing system, if present, first, so that it can be used to select
			// a named style effect.
			int ttv;
			int ws = props.GetIntPropValues((int) FwTextPropType.ktptWs, out ttv);
			if (ttv != -1)
				result = result.WithWs(ws);
			// Apply named style next, if present, so that style effects can be overridden by explicit ones.
			var namedStyle = props.GetStrPropValue((int) FwTextPropType.ktptNamedStyle);
			if (namedStyle != null)
				result = result.WithNamedStyle(namedStyle);
			int count = props.IntPropCount;
			for (int i = 0; i < count; i++)
			{
				int tpt;
				int val = props.GetIntProp(i, out tpt, out ttv);
				switch (tpt)
				{
					case (int) FwTextPropType.ktptWs: // handled first
						break;
					case (int) FwTextPropType.ktptBold:
						int weight;

						Debug.Assert(ttv == (int) FwTextPropVar.ktpvEnum);
						switch (val)
						{
							case (int) FwTextToggleVal.kttvForceOn:
								weight = (int) VwFontWeight.kvfwBold;
								break;
								// todo JohnT: several others.
							default:
								weight = (int)VwFontWeight.kvfwNormal;
								break;
						}
						result = result.WithFontWeight(weight);
						break;
				}
			}
			return result;
		}

		/// <summary>
		/// Return an assembled styles with inherited properties the same as this, but
		/// non-inherited ones reset to defaults.
		/// </summary>
		internal AssembledStyles ResetNonInherited()
		{
			// Todo JohnT: implement non-trivially. Use a fake ttp constant to store the result
			// in the style cache (ignore value).
			return m_styleCache.GetInheritedOnlyStyle(this);
		}

		/// <summary>
		/// Return an object which, when passed to WithProperties, will return an assembled styles
		/// with the specified font weight.
		/// </summary>
		public static PropSetter FontWeightSetter(int weight)
		{
			var result = new FontWeightPropSetter();
			result.Value = weight;
			return result;
		}

		/// <summary>
		/// Return an object which, when passed to WithProperties, will return an assembled styles
		/// with the specified font size.
		/// </summary>
		public static PropSetter FontSizeSetter(int height)
		{
			var result = new FontSizePropSetter();
			result.Value = height;
			return result;
		}
		/// <summary>
		/// Return an object which, when passed to WithProperties, will return an assembled styles
		/// with the specified baseline offset.
		/// </summary>
		public static PropSetter BaselineOffsetSetter(int height)
		{
			var result = new BaselineOffsetPropSetter();
			result.Value = height;
			return result;
		}

		/// <summary>
		/// Return an object which, when passed to WithProperties, will return an assembled styles
		/// with the specified line height.
		/// </summary>
		public static PropSetter LineHeightSetter(int height)
		{
			var result = new LineHeightPropSetter();
			result.Value = height;
			return result;
		}
		/// <summary>
		/// Return an object which, when passed to WithProperties, will return an assembled styles
		/// with the specified (or possibly inverted) italic setting.
		/// </summary>
		public static PropSetter FontItalicSetter(FwTextToggleVal ttv)
		{
			var result = new FontItalicPropSetter();
			result.Value = (int)ttv;
			return result;
		}

		/// <summary>
		/// Return an object which, when passed to WithProperties, will return an assembled styles
		/// with the specified font face.
		/// </summary>
		public static PropSetter FaceNameSetter(string name)
		{
			var result = new FaceNamePropSetter();
			result.Value = name;
			return result;
		}

		/// <summary>
		/// Return an object which, when passed to WithProperties, will return an assembled styles
		/// with the specified style.
		/// </summary>
		public static PropSetter StyleSetter(string name)
		{
			var result = new StylePropSetter();
			result.Value = name;
			return result;
		}

		/// <summary>
		/// Return an object which, when passed to WithProperties, will return an assembled styles
		/// with the specified underline type.
		/// </summary>
		public static PropSetter UnderlineSetter(FwUnderlineType unt)
		{
			var result = new UnderlinePropSetter();
			result.Value = (int)unt;
			return result;
		}

		/// <summary>
		/// Return an object which, when passed to WithProperties, will return an assembled styles
		/// with the specified margins (space in the color of the containing box, outside the border,
		/// allowed to overlap with adjacent boxes top and bottom).
		/// Enhance JohnT: may want a mechanism to allow only certain margins to be modified.
		/// </summary>
		public static PropSetter MarginsSetter(Thickness margins)
		{
			var result = new MarginsPropSetter();
			result.Value = margins;
			return result;
		}
		/// <summary>
		/// Return an object which, when passed to WithProperties, will return an assembled styles
		/// with the specified pads (space in the box's own background color, if any, inside the border).
		/// Enhance JohnT: may want a mechanism to allow only certain margins to be modified.
		/// </summary>
		public static PropSetter PadsSetter(Thickness margins)
		{
			var result = new PadsPropSetter();
			result.Value = margins;
			return result;
		}
		/// <summary>
		/// Return an object which, when passed to WithProperties, will return an assembled styles
		/// with the specified borders.
		/// Enhance JohnT: may want a mechanism to allow only certain borders to be modified.
		/// </summary>
		public static PropSetter BordersSetter(Thickness margins)
		{
			var result = new BordersPropSetter();
			result.Value = margins;
			return result;
		}
		/// <summary>
		/// Return an object which, when passed to WithProperties, will return an assembled styles
		/// with the specified foreground color.
		/// </summary>
		public static PropSetter ForeColorSetter(Color foreColor)
		{
			var result = new ForeColorPropSetter();
			result.Value = foreColor;
			return result;
		}

		/// <summary>
		/// Return an object which, when passed to WithProperties, will return an assembled styles
		/// with the specified foreground color.
		/// </summary>
		public static PropSetter BackColorSetter(Color backColor)
		{
			var result = new BackColorPropSetter();
			result.Value = backColor;
			return result;
		}

		/// <summary>
		/// Return an object which, when passed to WithProperties, will return an assembled styles
		/// with the specified foreground color.
		/// </summary>
		public static PropSetter BorderColorSetter(Color backColor)
		{
			var result = new BorderColorPropSetter();
			result.Value = backColor;
			return result;
		}

		/// <summary>
		/// Return an object which, when passed to WithProperties, will return an assembled styles
		/// with the specified underline color.
		/// </summary>
		public static PropSetter UnderlineColorSetter(Color underlineColor)
		{
			var result = new UnderlineColorPropSetter();
			result.Value = underlineColor;
			return result;
		}
		/// <summary>
		/// Return an AssembledStyles that has the properties of the recipient, modified as specified by
		/// the given setter (and any that have been appended to it).
		/// </summary>
		public AssembledStyles WithProperties(PropSetter setter)
		{
			if (setter == null)
				return this; // no setter, no change.
			return m_styleCache.GetDerivedStyle(this, setter);
		}

		public interface PropSetter
		{
			void AppendProp(PropSetter props);
		}

		/// <summary>
		/// A family of little classes used to set properties of an AssembledStyles.
		/// </summary>
		private abstract class PropSetterImpl : PropSetter
		{
			private PropSetterImpl m_next;
			internal abstract void SetOwnProperty(AssembledStyles styles);
			// This is set true when SetProperty is called, that is, the setter has been used
			// to set a style. Appending further settings would confuse the use of the setting as a key,
			// and is not allowed.
			private bool m_fHasBeenUsed;
			internal void SetProperty(AssembledStyles styles)
			{
				m_fHasBeenUsed = true;
				SetOwnProperty(styles);
				if (m_next != null)
					m_next.SetProperty(styles);
			}

			static internal PropSetterImpl BuildChain(PropSetterImpl start, PropSetterImpl add)
			{
				if (start == null)
					return add; // it becomes the whole chain
				start.AppendProp(add);
				return start;
			}

			/// <summary>
			/// Append the specified settter to your list, making a prop setter that will set all the indicated properties.
			/// </summary>
			/// <param name="props"></param>
			public void AppendProp(PropSetter props)
			{
				if (m_fHasBeenUsed)
					throw new ArgumentException("Cannot append to a PropSettings that has been used to make an assembled styles");
				if (m_next == null)
					m_next = (PropSetterImpl) props;
				else
					m_next.AppendProp(props); // Put it on the end of the chain so this one happens first
			}

			public override bool Equals(object obj)
			{
				if (obj == null || obj.GetType() != this.GetType())
					return false;
				var otherNext = ((PropSetterImpl) obj).m_next;
				return (m_next == null && otherNext == null) || m_next.Equals(otherNext);
			}

			public override int GetHashCode()
			{
				return m_next == null ? 0 : m_next.GetHashCode();
			}

		}

		private abstract class IntPropSetter : PropSetterImpl
		{
			internal int Value;

			public override bool Equals(object obj)
			{
				return base.Equals(obj) && Value == ((IntPropSetter) obj).Value;
			}

			public override int GetHashCode()
			{
				return base.GetHashCode() ^ Value.GetHashCode();
			}

		}

		private abstract class ThicknessPropSetter : PropSetterImpl
		{
			internal Thickness Value;

			public override bool Equals(object obj)
			{
				return base.Equals(obj) && Value == ((ThicknessPropSetter)obj).Value;
			}

			public override int GetHashCode()
			{
				return base.GetHashCode() ^ Value.GetHashCode();
			}
		}

		private abstract class StringPropSetter : PropSetterImpl
		{
			internal string Value;

			public override bool Equals(object obj)
			{
				return base.Equals(obj) && Value == ((StringPropSetter)obj).Value;
			}

			public override int GetHashCode()
			{
				return base.GetHashCode() ^ Value.GetHashCode();
			}
		}
		private class FaceNamePropSetter : StringPropSetter
		{
			internal override void SetOwnProperty(AssembledStyles styles)
			{
				styles.SetFaceName(Value);
			}
		}
		private class StylePropSetter : StringPropSetter
		{
			internal override void SetOwnProperty(AssembledStyles styles)
			{
				styles.StyleName = Value;
			}
		}
		private class FontWeightPropSetter: IntPropSetter
		{
			internal override void SetOwnProperty(AssembledStyles styles)
			{
				styles.FontWeight = Value;
			}
		}
		private class FontSizePropSetter : IntPropSetter
		{
			internal override void SetOwnProperty(AssembledStyles styles)
			{
				styles.m_chrp.dympHeight = Value;
			}
		}
		private class BaselineOffsetPropSetter : IntPropSetter
		{
			internal override void SetOwnProperty(AssembledStyles styles)
			{
				styles.m_chrp.dympOffset = Value;
			}
		}
		private class LineHeightPropSetter : IntPropSetter
		{
			internal override void SetOwnProperty(AssembledStyles styles)
			{
				styles.m_lineHeight = Value;
			}
		}
		private class FontItalicPropSetter : IntPropSetter
		{
			internal override void SetOwnProperty(AssembledStyles styles)
			{
				if (Value == (int) FwTextToggleVal.kttvInvert)
					styles.m_chrp.ttvItalic = styles.FontItalic ? (int) FwTextToggleVal.kttvOff : (int) FwTextToggleVal.kttvForceOn;
				else
					styles.m_chrp.ttvItalic = Value;
			}

			public override string ToString()
			{
				string extra;
				switch (Value)
				{
					case (int) FwTextToggleVal.kttvOff:
						extra = "no italics";
						break;
					case (int) FwTextToggleVal.kttvForceOn:
						extra = "forced italics";
						break;
					case (int) FwTextToggleVal.kttvInvert:
						extra = "inverted italics";
						break;
					default:
						extra = "some unexpected value " + Value;
						break;
				}
				return "FontItalicPropSetter for " + extra;
			}
		}

		private class UnderlinePropSetter : IntPropSetter
		{
			internal override void SetOwnProperty(AssembledStyles styles)
			{
				styles.m_chrp.unt = Value;
			}

			public override string ToString()
			{
				return "UnderlinPropSetter for " + ((FwUnderlineType)Value);
			}
		}

		/// <summary>
		/// Base class for properties set as colors. Keep this for when we do BorderColor or anything else
		/// that doesn't have to follow the old Chrp model.
		/// </summary>
		private abstract class ColorSetter : PropSetterImpl
		{
			internal Color Value;
			internal uint Argb { get { return (uint) Value.ToArgb(); } }

			public override bool Equals(object obj)
			{
				return base.Equals(obj) && Value.ToArgb() == ((ColorSetter)obj).Value.ToArgb();
			}

			public override int GetHashCode()
			{
				return base.GetHashCode() ^ Value.ToArgb().GetHashCode();
			}
		}


		/// <summary>
		/// Base class for properties that should be set to a BGR (rather than ARGB) value.
		/// </summary>
		private abstract class BgrSetter : PropSetterImpl
		{
			internal Color Value;
			internal uint Bgr { get { return ColorUtil.ConvertColorToBGR(Value); } }

			public override bool Equals(object obj)
			{
				return base.Equals(obj) && ColorUtil.ConvertColorToBGR(Value) ==
					ColorUtil.ConvertColorToBGR(((BgrSetter)obj).Value);
			}

			public override int GetHashCode()
			{
				return base.GetHashCode() ^ ColorUtil.ConvertColorToBGR(Value).GetHashCode();
			}
		}

		private class ForeColorPropSetter : BgrSetter
		{
			internal override void SetOwnProperty(AssembledStyles styles)
			{
				styles.m_chrp.clrFore = Bgr;
			}

		}
		private class BackColorPropSetter : BgrSetter
		{
			internal override void SetOwnProperty(AssembledStyles styles)
			{
				styles.m_chrp.clrBack = Bgr;
			}
		}
		private class BorderColorPropSetter : ColorSetter
		{
			internal override void SetOwnProperty(AssembledStyles styles)
			{
				styles.m_borderColor = Value;
			}
		}
		private class UnderlineColorPropSetter : BgrSetter
		{
			internal override void SetOwnProperty(AssembledStyles styles)
			{
				styles.m_chrp.clrUnder = Bgr;
			}
		}
		private class MarginsPropSetter: ThicknessPropSetter
		{
			internal override void SetOwnProperty(AssembledStyles styles)
			{
				styles.m_margins = Value;
			}
		}
		private class PadsPropSetter : ThicknessPropSetter
		{
			internal override void SetOwnProperty(AssembledStyles styles)
			{
				styles.m_pads = Value;
			}
		}
		private class BordersPropSetter : ThicknessPropSetter
		{
			internal override void SetOwnProperty(AssembledStyles styles)
			{
				styles.m_borders = Value;
			}
		}
	}


	/// <summary>
	/// This is a class that retrieves assembled styles, either by matching an existing one, or by the path used to create it.
	/// We keep track of the path using two dictionaries, one for object properties and one for integer ones.
	/// The object one could serve for everything, but a lot of integers would
	/// get boxed, and we'd have to do all kinds of trickery to avoid the nuisance that two independent boxes of the same integer
	/// are not equal.
	/// We could achieve the same functionality without the Triple-keyed dictionaries at all, but much less efficiently: we'd have
	/// to create the derived AS every time before finding out that it already exists.
	/// </summary>
	internal class AssembledStylesCache
	{
		// Function to modify an AS baed on an integer value.
		internal delegate void IntDeriver(AssembledStyles newStyles, int val);
		// Function to modify an AS baed on an object value.
		internal delegate void ObjDeriver(AssembledStyles newStyles, object val);

		/// <summary>
		/// A dictionary of all styles we have created, which is shared by all styles derived from a common base.
		/// The dictionary maps a style to itself. This allows us, given a newly created AS, to find the canonical one
		/// with the same properties, if it exists.
		/// </summary>
		internal Dictionary<AssembledStyles, AssembledStyles> m_canonicalStyles = new Dictionary<AssembledStyles, AssembledStyles>();

		/// <summary>
		/// A dictionary used to efficiently retrieve a style derived from an existing one. The key is a triple of the style we are
		/// deriving from, an integer (mostly one of the FwTextPropVal enumeration) indicating which property, and the value.
		/// </summary>
		private Dictionary<Triple<AssembledStyles, int, int>, AssembledStyles> m_intStyleDerivations =
			new Dictionary<Triple<AssembledStyles, int, int>, AssembledStyles>();
		/// <summary>
		/// A dictionary used to efficiently retrieve a style derived from an existing one. The key is a triple of the style we are
		/// deriving from, an integer (mostly one of the FwTextPropVal enumeration) indicating which property, and the value.
		/// </summary>
		private Dictionary<Triple<AssembledStyles, int, object>, AssembledStyles> m_objStyleDerivations =
			new Dictionary<Triple<AssembledStyles, int, object>, AssembledStyles>();

		/// <summary>
		/// Similarly this dictionary allows us to efficiently find a style derived from an existing one using
		/// a PropSetter.
		/// </summary>
		private Dictionary<Tuple<AssembledStyles, AssembledStyles.PropSetter>, AssembledStyles> m_PropSetterDerivations =
			new Dictionary<Tuple<AssembledStyles, AssembledStyles.PropSetter>, AssembledStyles>();

		/// <summary>
		/// The stylesheet which determines the meaning of named styles for AssembledStyles in this tree.
		/// </summary>
		internal IStylesheet Stylesheet { get; private set; }

		public AssembledStylesCache(IStylesheet stylesheet)
		{
			Stylesheet = stylesheet;
		}

		internal AssembledStyles CanonicalInstance(AssembledStyles astyles)
		{
			AssembledStyles result;
			if (m_canonicalStyles.TryGetValue(astyles, out result))
				return result;
			m_canonicalStyles[astyles] = astyles;
			return astyles;
		}

		internal AssembledStyles GetDerivedStyle(AssembledStyles basedOn, int ttp, int val, IntDeriver deriver)
		{
			AssembledStyles result;
			var key = new Triple<AssembledStyles, int, int>(basedOn, ttp, val);
			if (m_intStyleDerivations.TryGetValue(key, out result))
				return result;
			result = new AssembledStyles(basedOn);
			deriver(result, val);
			result = CanonicalInstance(result); // AFTER finalizing values!
			m_intStyleDerivations[key] = result; // for next time.
			return result;
		}
		internal AssembledStyles GetDerivedStyle(AssembledStyles basedOn, int ttp, object val, ObjDeriver deriver)
		{
			AssembledStyles result;
			var key = new Triple<AssembledStyles, int, object>(basedOn, ttp, val);
			if (m_objStyleDerivations.TryGetValue(key, out result))
				return result;
			result = new AssembledStyles(basedOn);
			deriver(result, val);
			result = CanonicalInstance(result); // AFTER finalizing values!
			m_objStyleDerivations[key] = result; // for next time.
			return result;
		}
		/// <summary>
		/// Key used to look up non-inherited style in cache. Must not match any kttp.
		/// </summary>
		private const int kInheritedOnlyKey = -1;

		internal AssembledStyles GetInheritedOnlyStyle(AssembledStyles basedOn)
		{
			AssembledStyles result;
			// Using this as the third key is arbitrary, we just need some non-null object that will
			// be the same when we look it up again.
			var key = new Triple<AssembledStyles, int, object>(basedOn, kInheritedOnlyKey, this);
			if (m_objStyleDerivations.TryGetValue(key, out result))
				return result;
			result = new AssembledStyles(basedOn, true);
			result = CanonicalInstance(result); // AFTER finalizing values!
			m_objStyleDerivations[key] = result; // for next time.
			return result;
		}

		internal AssembledStyles GetDerivedStyle(AssembledStyles basedOn, AssembledStyles.PropSetter setter)
		{
			AssembledStyles result;
			var key = new Tuple<AssembledStyles, AssembledStyles.PropSetter>(basedOn, setter);
			if (m_PropSetterDerivations.TryGetValue(key, out result))
				return result;
			result = new AssembledStyles(basedOn, setter);
			result = CanonicalInstance(result); // AFTER finalizing values!
			m_PropSetterDerivations[key] = result; // for next time.
			return result;
		}
	}

	/// <summary>
	/// Similar to System.Windows.Thickness, this represents the four thicknesses of the sides of a rectangle
	/// in 96ths of an inch. It is used for the thickness of a border, padding, and margins.
	/// It is not a struct because it Thickness variables are often null, and we do not need to allocate space
	/// for four doubles.
	/// Another reason to define it is to avoid using PresentationFramework, which may be a problem for Mono.
	/// </summary>
	public class Thickness
	{
		public readonly double Leading; // Left in an LTR language, else right
		public readonly double Top;
		public readonly double Trailing; // Right in an LTR language, else left.
		public readonly double Bottom;

		/// <summary>
		/// Make one with uniform thickness.
		/// </summary>
		public Thickness(double thickness) : this(thickness, thickness, thickness, thickness)
		{
		}

		public Thickness(double leading, double top, double trailing, double bottom)
		{
			Leading = leading;
			Top = top;
			Trailing = trailing;
			Bottom = bottom;
		}

		int ToMillipoints(double points)
		{
			if (points == 0)
				return 0;
			var result = (int) Math.Round(points*1000);
			if (result == 0)
				return points > 0 ? 1 : -1;
			return result;
		}

		/// <summary> Millipoint version of Top. </summary>
		internal int TopMp { get { return ToMillipoints(Top);}}
		/// <summary> Millipoint version of Leading. </summary>
		internal int LeadingMp { get { return ToMillipoints(Leading); } }
		/// <summary> Millipoint version of Bottom. </summary>
		internal int BottomMp { get { return ToMillipoints(Bottom); } }
		/// <summary> Millipoint version of Trailing. </summary>
		internal int TrailingMp { get { return ToMillipoints(Trailing); } }

		/// <summary>
		/// A default value, no thickness: use this rather than null wherever
		/// </summary>
		public static readonly Thickness Default = new Thickness(0.0);

		public bool Equals(Thickness other)
		{
			return other.Leading == Leading && other.Top == Top && other.Trailing == Trailing && other.Bottom == Bottom;
		}

		public override bool Equals(object obj)
		{
			return obj is Thickness && Equals((Thickness)obj);
		}

		public override int GetHashCode()
		{
			return Leading.GetHashCode() ^ Top.GetHashCode() ^ Trailing.GetHashCode() ^ Bottom.GetHashCode();
		}

		public static bool operator ==(Thickness t1, Thickness t2)
		{
			if (Object.ReferenceEquals(t1, null)) // Don't use ==! Infinite recursion!
				return Object.ReferenceEquals(t2, null);
			return t1.Equals(t2);
		}

		public static bool operator !=(Thickness t1, Thickness t2)
		{
			return !(t1 == t2);
		}
	}
}
