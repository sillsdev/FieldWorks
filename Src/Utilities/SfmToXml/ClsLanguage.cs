using System;
using System.Collections;
using ECInterfaces;
using SilEncConverters31;

namespace Sfm2Xml
{
	/// <summary>
	/// Summary description for ClsLanguage.
	/// </summary>
	public class ClsLanguage
	{
		private string m_LangId;
		private string m_XmlLang;
		private string m_EncCvtrMap;
		private IEncConverter m_iconverter;

		public ClsLanguage()
		{
			m_LangId = "";
			m_XmlLang = "";
			m_EncCvtrMap = "";
		}

		public ClsLanguage(string lang, string xmlLang, string map)
		{
			m_LangId = lang;
			m_XmlLang = xmlLang;
			m_EncCvtrMap = map;
		}

		public override string ToString()
		{
			string langData = "id=\"" + m_LangId + "\" xml:lang=\"" + m_XmlLang + "\"";
			if (m_EncCvtrMap != null && m_EncCvtrMap.Length > 0)
			{
				langData += " map=\"" + m_EncCvtrMap + "\"";
			}
			return langData;
		}

		public string ToXmlString()
		{
			string result = ToString();	// get the data portion of the string
			result = result.Replace("&", "&amp;");
			result = result.Replace("<", "&lt;");
			result = result.Replace(">", "&gt;");

			// add langDef element
			result = "<langDef " + result + "/>";
			return result;
		}

//		public string Languagex
//		{
//			get { return m_LangId; }
//		}

		public string KEY
		{
			get { return m_LangId; }
		}

		public string XmlLang
		{
			get { return m_XmlLang; }
			set { m_XmlLang = value; }
		}

		public string EncCvtrMap
		{
			get { return m_EncCvtrMap; }
		}

		public IEncConverter EncCvtr
		{
			get { return m_iconverter; }
		}

		public void NoConvert()
		{
			m_EncCvtrMap = null;	// if the map is empty, this means the lang is already UTF8
			m_iconverter = null;	// also null
		}

		public void Convert(string xmlLang, string map)
		{
			m_XmlLang = xmlLang;
			m_EncCvtrMap = map;
		}

		public bool SetConverter(EncConverters converters)
		{
			if (m_EncCvtrMap == null)
				return true;	// if the map is empty, this means the lang is already UTF8

			m_iconverter = converters[m_EncCvtrMap];
			return (m_iconverter != null);
		}

		public bool ReadXmlNode(System.Xml.XmlNode langDef)
		{
			bool Success = true;

			foreach(System.Xml.XmlAttribute Attribute in langDef.Attributes)
			{
				switch (Attribute.Name)
				{
					case "id":
						if (Attribute.Value == "")
							Success = false;
						else
							m_LangId = Attribute.Value;
						break;
					case "xml:lang":
						if (Attribute.Value == "")
							Success = false;
						else
							m_XmlLang = Attribute.Value;
						break;
					case "map":
						if (Attribute.Value == "")
							Success = false;
						else
							m_EncCvtrMap = Attribute.Value;
						break;
					default:
						Converter.Log.AddWarning(String.Format(Sfm2XmlStrings.UnknownAttribute0InTheLanguages, Attribute.Name));
						break;
				}
			}

			if (m_LangId == null)
			{
				Converter.Log.AddError(Sfm2XmlStrings.IdNotDefinedInALanguage);
				Success = false;
			}
			if (m_XmlLang == null)
			{
				Converter.Log.AddError(String.Format(Sfm2XmlStrings.Language0LacksXmlLangAttribute + m_LangId));
				Success = false;
			}
//			if (m_EncCvtrMap == null)
//			{
//				Converter.Log.AddError("Error in Mapping File: language '" + m_LangId + "' is invalid because its 'map' attribute is not defined.");
//				Success = false;
//			}

			return Success;
		}
	}
}
