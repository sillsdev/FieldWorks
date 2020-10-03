// Copyright (c) 2006-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.SfmToXml
{
	/// <summary>
	/// This class contains the Language info that is used and displayed in the UI.
	/// It's primarily used for mapping the FW writing system and encoding converter to
	/// the key that is used in the map file.
	/// </summary>
	internal sealed class LanguageInfoUI
	{
		internal LanguageInfoUI(string key, string fwName, string enc, string icu)
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

		internal string Key { get; }

		internal string FwName { get; }

		internal string ICUName { get; }

		internal string EncodingConverterName { get; }

		public override string ToString()
		{
			return FwName == SfmToXmlServices.Ignore ? string.Format(SfmToXmlStrings.XIgnored, Key) : Key;
		}

		internal ClsLanguage ClsLanguage => new ClsLanguage(Key, ICUName, EncodingConverterName);
	}
}