// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections;
using System.Xml;
using Sfm2Xml;

namespace LanguageExplorer.Controls.LexText
{
	/// <summary>
	/// Quick and dirty class for reading the InFieldMarkers section of the 'map' file.
	/// </summary>
	internal class IFMReader : Converter
	{
		public Hashtable IFMS(string mapFile, Hashtable languages)
		{
			var xmlMap = new XmlDocument();
			try
			{
				// pull out the clsLanguage objects and put in local hash for them
				foreach(DictionaryEntry entry in languages)
				{
					var lang = entry.Value as LanguageInfoUI;
					m_Languages.Add(lang.Key, lang.ClsLanguage);
				}
				xmlMap.Load(mapFile);
				ReadInFieldMarkers(xmlMap);
				return InFieldMarkerHashTable;
			}
			catch
			{
			}
			return null;
		}
	}
}