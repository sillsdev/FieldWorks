using System;

namespace Sfm2Xml
{
	/// <summary>
	/// Interface definition that is used to nail down the interaction from the
	/// lex wizard and this code that isn't dependant on FW.
	/// </summary>
	public interface ILanguageInfoUI
	{
		//		static public string AlreadyInUnicode() { return "<Already in Unicode>"; }
		string Key { get; }		// map lang key
		string FwName { get; }	// fw name to use
		string ICUName { get; }	// icu value
		string EncodingConverterName { get; }	// encoding convert for this ws
		string ToString();
		Sfm2Xml.ClsLanguage ClsLanguage { get;}
	}

	/// <summary>
	/// This class contains the Language info that is used and displayed in the UI.
	/// It's primarly used for mapping the FW writing system and encoding converter to
	/// the key that is used in the map file.
	/// </summary>
	public class LanguageInfoUI : ILanguageInfoUI
	{
		private string m_key;		// map lang key
		private string m_fwName;	// fw name to use
		private string m_enc;		// encoding converter for this ws
		private string m_icu;		// icu value

		public LanguageInfoUI(string key, string fwName, string enc, string icu)
		{
			m_key = key;
			m_fwName = fwName;
			m_enc = enc;
			if (m_enc == STATICS.AlreadyInUnicode)
				m_enc = "";
			m_icu = icu;
		}
		public string Key { get { return m_key; } }
		public string FwName { get { return m_fwName; } }
		public string ICUName { get { return m_icu; } }
		public string EncodingConverterName { get { return m_enc; } }
		public override string ToString()
		{
			if (FwName == STATICS.Ignore)
				return String.Format(Sfm2XmlStrings.XIgnored, m_key);
			else
				return m_key;
		}	// description value now, was m_fwName; }

		public Sfm2Xml.ClsLanguage ClsLanguage { get { return new Sfm2Xml.ClsLanguage(m_key, m_icu, m_enc); } }
	}

}
