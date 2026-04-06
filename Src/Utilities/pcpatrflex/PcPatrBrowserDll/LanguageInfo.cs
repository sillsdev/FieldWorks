using System;
using System.IO;
using System.Drawing;
using System.Xml;
using System.Xml.Serialization;

namespace SIL.PcPatrBrowser
{
	/// <summary>
	/// Summary description for LanguageInfo.
	/// </summary>
	[Serializable]
	public class LanguageInfo : Object
	{
		string m_sLanguageName;
		private MyFontInfo m_mfiNT;
		private MyFontInfo m_mfiLex;
		private MyFontInfo m_mfiGloss;
		bool m_fUseRTL;
		string m_cDecompChar;

		// layout info

		public LanguageInfo() { }

		public LanguageInfo(
			string sName,
			MyFontInfo NT,
			MyFontInfo Lex,
			MyFontInfo Gloss,
			bool fRTL,
			string cDecomp
		)
		{
			m_sLanguageName = sName;
			m_mfiNT = NT;
			m_mfiLex = Lex;
			m_mfiGloss = Gloss;
			m_fUseRTL = fRTL;
			m_cDecompChar = cDecomp;
		}

		/// <summary>
		/// Gets/sets NT info.
		/// </summary>
		[XmlIgnore]
		public MyFontInfo NTInfo
		{
			get { return m_mfiNT; }
			set { m_mfiNT = value; }
		}

		/// <summary>
		/// Gets/sets NT Color name value.  (Do not use this; use NTInfo instead.)
		/// </summary>
		/// For XML Serialization
		public string NTColorName
		{
			get { return m_mfiNT.ColorName; }
			set { m_mfiNT.ColorName = value; }
		}

		/// <summary>
		/// Gets/sets NT Font Face. (Do not use this; use NTInfo instead.)
		/// </summary>
		/// For XML Serialization
		public string NTFontFace
		{
			get { return m_mfiNT.FontFace; }
			set { m_mfiNT.FontFace = value; }
		}

		/// <summary>
		/// Gets/sets NT Font Size. (Do not use this; use NTInfo instead.)
		/// </summary>
		/// For XML Serialization
		public float NTFontSize
		{
			get { return m_mfiNT.FontSize; }
			set { m_mfiNT.FontSize = value; }
		}

		/// <summary>
		/// Gets/sets NT Font Style (Do not use this; use NTInfo instead.)
		/// </summary>
		/// For XML Serialization
		public FontStyle NTFontStyle
		{
			get { return m_mfiNT.FontStyle; }
			set { m_mfiNT.FontStyle = value; }
		}

		/// <summary>
		/// Gets/sets Lex Color name value.  (Do not use this; use LexInfo instead.)
		/// </summary>
		/// For XML Serialization
		public string LexColorName
		{
			get { return m_mfiLex.ColorName; }
			set { m_mfiLex.ColorName = value; }
		}

		/// <summary>
		/// Gets/sets Lex info.
		/// </summary>
		[XmlIgnore]
		public MyFontInfo LexInfo
		{
			get { return m_mfiLex; }
			set { m_mfiLex = value; }
		}

		/// <summary>
		/// Gets/sets Lex Font Face. (Do not use this; use LexInfo instead.)
		/// </summary>
		/// For XML Serialization
		public string LexFontFace
		{
			get { return m_mfiLex.FontFace; }
			set { m_mfiLex.FontFace = value; }
		}

		/// <summary>
		/// Gets/sets Lex Font Size. (Do not use this; use LexInfo instead.)
		/// </summary>
		/// For XML Serialization
		public float LexFontSize
		{
			get { return m_mfiLex.FontSize; }
			set { m_mfiLex.FontSize = value; }
		}

		/// <summary>
		/// Gets/sets Lex Font Style (Do not use this; use LexInfo instead.)
		/// </summary>
		/// For XML Serialization
		public FontStyle LexFontStyle
		{
			get { return m_mfiLex.FontStyle; }
			set { m_mfiLex.FontStyle = value; }
		}

		/// <summary>
		/// Gets/sets Gloss info.
		/// </summary>
		[XmlIgnore]
		public MyFontInfo GlossInfo
		{
			get { return m_mfiGloss; }
			set { m_mfiGloss = value; }
		}

		/// <summary>
		/// Gets/sets Gloss Color name value.  (Do not use this; use GlossInfo instead.)
		/// </summary>
		/// For XML Serialization
		public string GlossColorName
		{
			get { return m_mfiGloss.ColorName; }
			set { m_mfiGloss.ColorName = value; }
		}

		/// <summary>
		/// Gets/sets Gloss Font Face. (Do not use this; use GlossInfo instead.)
		/// </summary>
		/// For XML Serialization
		public string GlossFontFace
		{
			get { return m_mfiGloss.FontFace; }
			set { m_mfiGloss.FontFace = value; }
		}

		/// <summary>
		/// Gets/sets Gloss Font Size. (Do not use this; use GlossInfo instead.)
		/// </summary>
		/// For XML Serialization
		public float GlossFontSize
		{
			get { return m_mfiGloss.FontSize; }
			set { m_mfiGloss.FontSize = value; }
		}

		/// <summary>
		/// Gets/sets Gloss Font Style (Do not use this; use GlossInfo instead.)
		/// </summary>
		/// For XML Serialization
		public FontStyle GlossFontStyle
		{
			get { return m_mfiGloss.FontStyle; }
			set { m_mfiGloss.FontStyle = value; }
		}

		/// <summary>
		/// Gets/sets language name.
		/// </summary>
		public string LanguageName
		{
			get { return m_sLanguageName; }
			set { m_sLanguageName = value; }
		}

		/// <summary>
		/// Gets/sets use right-to-left orientation.
		/// </summary>
		public bool UseRTL
		{
			get { return m_fUseRTL; }
			set { m_fUseRTL = value; }
		}

		/// <summary>
		/// Gets/sets morpheme decomposition separation character.
		/// </summary>
		public string DecompChar
		{
			get { return m_cDecompChar; }
			set { m_cDecompChar = value; }
		}

		public void LoadInfo(string sFileName)
		{
			XmlDocument doc = new XmlDocument();
			doc.Load(sFileName);
			LanguageName = GetNodeContent("LanguageInfo/LanguageName", doc);

			LexColorName = GetNodeContent("LanguageInfo/LexColorName", doc);
			LexFontFace = GetNodeContent("LanguageInfo/LexFontFace", doc);
			LexFontSize = (float)Convert.ToDouble(GetNodeContent("LanguageInfo/LexFontSize", doc));
			LexFontStyle = GetFontStyle("LanguageInfo/LexFontFace", doc);
			m_mfiLex = new MyFontInfo(LexFontFace, LexFontSize, LexFontStyle, LexColorName);

			NTColorName = GetNodeContent("LanguageInfo/NTColorName", doc);
			NTFontFace = GetNodeContent("LanguageInfo/NTFontFace", doc);
			NTFontSize = (float)Convert.ToDouble(GetNodeContent("LanguageInfo/NTFontSize", doc));
			NTFontStyle = GetFontStyle("LanguageInfo/NTFontFace", doc);
			m_mfiNT = new MyFontInfo(NTFontFace, NTFontSize, NTFontStyle, NTColorName);

			GlossColorName = GetNodeContent("LanguageInfo/GlossColorName", doc);
			GlossFontFace = GetNodeContent("LanguageInfo/GlossFontFace", doc);
			GlossFontSize = (float)
				Convert.ToDouble(GetNodeContent("LanguageInfo/GlossFontSize", doc));
			GlossFontStyle = GetFontStyle("LanguageInfo/GlossFontFace", doc);
			m_mfiGloss = new MyFontInfo(
				GlossFontFace,
				GlossFontSize,
				GlossFontStyle,
				GlossColorName
			);

			DecompChar = GetNodeContent("LanguageInfo/DecompChar", doc);
			string s = GetNodeContent("LanguageInfo/UseRTL", doc);
			if (s == "true")
				UseRTL = true;
			else
				UseRTL = false;

			// did not work and I don't know why
			//XmlSerializer mySerializer = new XmlSerializer(m_language.GetType());
			//StreamReader myReader = new StreamReader(sFileName);
			//LanguageInfo lang = new LanguageInfo();
			//lang = (LanguageInfo)mySerializer.Deserialize(myReader);
			//myReader.Close();
		}

		private FontStyle GetFontStyle(string sXPath, XmlDocument doc)
		{
			FontStyle result = FontStyle.Regular;
			string sStyle = GetNodeContent(sXPath, doc);
			switch (sStyle)
			{
				case "Regular":
					result = FontStyle.Regular;
					break;
				case "Italic":
					result = FontStyle.Italic;
					break;
					// etc. and check for combos
			}
			return result;
		}

		private string GetNodeContent(string sXPath, XmlDocument doc)
		{
			string sResult = null;
			XmlNode node = doc.SelectSingleNode(sXPath);
			if (node != null)
				sResult = node.InnerText;
			return sResult;
		}

		public void SaveInfo(string sFileName)
		{
			XmlSerializer mySerializer = new XmlSerializer(this.GetType());
			StreamWriter myWriter = new StreamWriter(sFileName);
			mySerializer.Serialize(myWriter, this);
			myWriter.Close();
		}
	}
}
