// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections;
using System.Xml;
using Sfm2Xml;

namespace LanguageExplorer.Controls.LexText
{
	/// <summary>
	/// Quick and dirty class for reading the Languages section of the 'map' file.
	/// </summary>
	internal class LangConverter : Converter
	{
		public Hashtable Languages(string mapFile)
		{
			var xmlMap = new XmlDocument();
			try
			{
				xmlMap.Load(mapFile);
				ReadLanguages(xmlMap);
				return base.GetLanguages;
			}
			catch
			{
			}
			return null;
		}
	}
}