// Copyright (c) 2006-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.SfmToXml
{
	/// <summary>
	/// This class contains the Language info that is used and displayed in the UI.
	/// It's primarily used for mapping the FW writing system and encoding converter to
	/// the key that is used in the map file.
	/// </summary>
	public class LanguageInfoUI
	{
		public LanguageInfoUI(string key, string fwName, string enc, string icu)
		{
			Key = key;
			FwName = fwName;
			EncodingConverterName = enc;
			if (EncodingConverterName == SfmToXmlServices.AlreadyInUnicode)
			{
				EncodingConverterName = string.Empty;
			}
			ICUName = icu;
		}

		public string Key { get; }

		public string FwName { get; }

		public string ICUName { get; }

		public string EncodingConverterName { get; }

		public override string ToString()
		{
			return FwName == SfmToXmlServices.Ignore ? string.Format(SfmToXmlStrings.XIgnored, Key) : Key;
		}

		public ClsLanguage ClsLanguage => new ClsLanguage(Key, ICUName, EncodingConverterName);
	}

}
