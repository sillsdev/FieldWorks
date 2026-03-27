using System;
using System.Drawing;

namespace SIL.PcPatrBrowser
{
	/// <summary>
	/// Summary description for MyFontInfo.
	/// </summary>
	public class MyFontInfo : Object
	{
		Font m_font;
		string m_sFontFace;
		float m_fFontSize;
		FontStyle m_fontStyle;
		Color m_color;
		string m_sColorName;

		public MyFontInfo(Font myfont, Color mycolor)
		{
			m_font = myfont;
			m_color = mycolor;
			m_sColorName = mycolor.Name;
			m_sFontFace = m_font.Name;
			m_fFontSize = m_font.Size;
			m_fontStyle = m_font.Style;
		}

		public MyFontInfo(string sFontFace, float fFontSize, FontStyle fontStyle, string sFontColor)
		{
			m_font = new Font(sFontFace, fFontSize, fontStyle);
			m_sFontFace = sFontFace;
			m_fFontSize = fFontSize;
			m_fontStyle = FontStyle;
			m_color = Color.FromName(sFontColor);
			m_sColorName = sFontColor;
		}

		/// <summary>
		/// Gets/sets Color.
		/// </summary>
		public Color Color
		{
			get { return m_color; }
			set
			{
				m_color = value;
				m_sColorName = value.Name;
			}
		}

		/// <summary>
		/// Gets/sets ColorName.
		/// </summary>
		public string ColorName
		{
			get { return m_sColorName; }
			set
			{
				m_sColorName = value;
				m_color = Color.FromName(value);
			}
		}

		/// <summary>
		/// Gets/sets Font.
		/// </summary>
		public Font Font
		{
			get { return m_font; }
			set
			{
				m_font = value;
				m_sFontFace = value.Name;
				m_fFontSize = value.Size;
				m_fontStyle = value.Style;
			}
		}

		/// <summary>
		/// Gets/sets Font Face. (Do not use this; use Font instead.)
		/// </summary>
		/// For XML Serialization
		public string FontFace
		{
			get { return m_sFontFace; }
			set { m_sFontFace = value; }
		}

		/// <summary>
		/// Gets/sets Font Size. (Do not use this; use Font instead.)
		/// </summary>
		/// For XML Serialization
		public float FontSize
		{
			get { return m_fFontSize; }
			set { m_fFontSize = value; }
		}

		/// <summary>
		/// Gets/sets Font Style (Do not use this; use Font instead.)
		/// </summary>
		/// For XML Serialization
		public FontStyle FontStyle
		{
			get { return m_fontStyle; }
			set { m_fontStyle = value; }
		}
	}
}
