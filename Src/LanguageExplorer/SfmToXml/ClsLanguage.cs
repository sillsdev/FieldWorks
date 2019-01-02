// Copyright (c) 2005-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml;
using ECInterfaces;
using SilEncConverters40;

namespace LanguageExplorer.SfmToXml
{
	/// <summary />
	public class ClsLanguage
	{
		public ClsLanguage()
		{
			KEY = string.Empty;
			XmlLang = string.Empty;
			EncCvtrMap = string.Empty;
		}

		public ClsLanguage(string lang, string xmlLang, string map)
		{
			KEY = lang;
			XmlLang = xmlLang;
			EncCvtrMap = map;
		}

		public override string ToString()
		{
			var langData = $"id=\"{KEY}\" xml:lang=\"{XmlLang}\"";
			if (!string.IsNullOrEmpty(EncCvtrMap))
			{
				langData += $" map=\"{EncCvtrMap}\"";
			}
			return langData;
		}

		public string ToXmlString()
		{
			var result = ToString(); // get the data portion of the string
			result = result.Replace("&", "&amp;");
			result = result.Replace("<", "&lt;");
			result = result.Replace(">", "&gt;");
			// add langDef element
			result = $"<langDef {result}/>";
			return result;
		}

		public string KEY { get; private set; }

		public string XmlLang { get; set; }

		public string EncCvtrMap { get; private set; }

		public IEncConverter EncCvtr { get; private set; }

		public void NoConvert()
		{
			EncCvtrMap = null;    // if the map is empty, this means the lang is already UTF8
			EncCvtr = null;    // also null
		}

		public void Convert(string xmlLang, string map)
		{
			XmlLang = xmlLang;
			EncCvtrMap = map;
		}

		public bool SetConverter(EncConverters converters)
		{
			if (EncCvtrMap == null)
			{
				return true;    // if the map is empty, this means the lang is already UTF8
			}
			EncCvtr = converters[EncCvtrMap];
			return EncCvtr != null;
		}

		public bool ReadXmlNode(XmlNode langDef)
		{
			var success = true;
			foreach (XmlAttribute attribute in langDef.Attributes)
			{
				switch (attribute.Name)
				{
					case "id":
						if (attribute.Value == string.Empty)
						{
							success = false;
						}
						else
						{
							KEY = attribute.Value;
						}
						break;
					case "xml:lang":
						if (attribute.Value == string.Empty)
						{
							success = false;
						}
						else
						{
							XmlLang = attribute.Value;
						}
						break;
					case "map":
						if (attribute.Value == string.Empty)
						{
							success = false;
						}
						else
						{
							EncCvtrMap = attribute.Value;
						}
						break;
					default:
						Converter.Log.AddWarning(string.Format(SfmToXmlStrings.UnknownAttribute0InTheLanguages, attribute.Name));
						break;
				}
			}
			if (KEY == null)
			{
				Converter.Log.AddError(SfmToXmlStrings.IdNotDefinedInALanguage);
				success = false;
			}
			if (XmlLang == null)
			{
				Converter.Log.AddError(string.Format(SfmToXmlStrings.Language0LacksXmlLangAttribute + KEY));
				success = false;
			}
			return success;
		}
	}
}