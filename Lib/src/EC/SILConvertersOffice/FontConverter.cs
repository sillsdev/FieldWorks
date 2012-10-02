#define Csc30   // turn off CSC30 features

using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using ECInterfaces;
using SilEncConverters31;

namespace SILConvertersOffice
{
/*
#if !Csc30
	using SpellingFixer30;
#else
	using SpellingFixerEC;
#endif
*/
	/// <summary>
	/// Collection of the FontConverter items.
	/// The key to this Dictionary is the font name
	///  (use .cstrAllFonts to have it apply to all fonts)
	/// </summary>
	internal class FontConverters : Dictionary<string, FontConverter>
	{
		public const string cstrAllFonts = "FontConverterAppliesToAllFonts";   // something unique
	}

	internal class FontConverter
	{
		public const float cdFontSize = 14;

		protected Font m_font = null;
		protected Font m_fontRhs = null;    // for the after-conversion textbox display
		protected DirectableEncConverter m_aEC = null;

		/// <summary>
		/// FontConverter constructor for when we know the font name, but not
		/// the EncConverter to use (this triggers the IEC.AutoSelect method).
		/// </summary>
		/// <param name="strFontName"></param>
		public FontConverter(string strFontName)
		{
			m_font = CreateFontSafe(strFontName);
			string strTitle = String.Format("Select Converter for '{0}' font", strFontName);
			EncConverters aECs = OfficeApp.GetEncConverters;
			if (aECs != null)
				m_aEC = new DirectableEncConverter(aECs.AutoSelectWithTitle(ConvType.Unknown, strTitle));
		}

		/*
		/// <summary>
		/// FontConverter constructor for when you know the SpellingFixer30 project to use,
		/// (which includes the EncConverter and the Font associated with the project.
		/// </summary>
		/// <param name="aSF"></param>
#if !Csc30
		public FontConverter(SpellingFixer aSF)
#else
		public FontConverter(SpellingFixerEC aSF)
#endif
		{
			m_aEC = new DirectableEncConverter(aSF.SpellFixerEncConverter);
			m_font = aSF.ProjectFont;
		}
		*/
		/// <summary>
		/// FontConverter constructor for when you know the EncConverter to use, and it
		/// works for all fonts (i.e. without prompting the user)
		/// </summary>
		/// <param name="aEC"></param>
		public FontConverter(DirectableEncConverter aEC)
		{
			m_aEC = aEC;
		}

		/// <summary>
		/// FontConverter constructor for when you know both the font and EncConverter to use
		/// </summary>
		public FontConverter(string strFontName, DirectableEncConverter aEC)
		{
			m_font = CreateFontSafe(strFontName);
			m_aEC = aEC;
		}

		public Font Font
		{
			get { return m_font; }
			set { m_font = value; }
		}

		public Font RhsFont
		{
			get { return m_fontRhs; }
			set { m_fontRhs = value; }
		}

		public IEncConverter EncConverter
		{
			get { return m_aEC.GetEncConverter; }
		}

		public DirectableEncConverter DirectableEncConverter
		{
			get { return m_aEC; }
		}

		public override string ToString()
		{
			string str = null;
			if ((Font != null) && (EncConverter != null))
			{
				if (RhsFont != null)
					str = String.Format(" to '{0}'", RhsFont.Name);

				str = String.Format("'{0}'{1} using '{2}'",
					Font.Name, str, EncConverter.Name);
			}
			return str;
		}

		protected Font CreateFontSafe(string strFontName)
		{
			Font font = null;
			try
			{
				font = new Font(strFontName, cdFontSize);
			}
			catch (Exception ex)
			{
				if (ex.Message.IndexOf("' does not support style '") != -1)
				{
					try
					{
						font = new Font(strFontName, cdFontSize, FontStyle.Bold);
					}
					catch
					{
						if (ex.Message.IndexOf("' does not support style '") != -1)
						{
							try
							{
								font = new Font(strFontName, cdFontSize, FontStyle.Italic);
							}
							catch { }
						}
					}
				}
			}
			finally
			{
				if (font == null)
					font = new Font("Microsoft Sans Serif", 12);
			}

			return font;
		}
	}
}
