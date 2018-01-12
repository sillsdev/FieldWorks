// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml;
using Sfm2Xml;

namespace LanguageExplorer.Controls.LexText
{
	/// <summary>
	/// Quick and dirty class for reading the Options section of the 'map' file.
	/// </summary>
	internal class OptionConverter : Converter
	{
		public Dictionary<string, bool> Options(string mapFile)
		{
			var xmlMap = new XmlDocument();
			try
			{
				xmlMap.Load(mapFile);
				ReadOptions(xmlMap);
				return base.GetOptions;
			}
			catch
			{
				var xmlFile = mapFile.Split('\\');
				MessageBox.Show(String.Format("Xml file {0} is invalid.", xmlFile[xmlFile.Length - 1]));
			}
			return null;
		}
	}
}