using System;
using System.Collections;

namespace Sfm2Xml
{
	/// <summary>
	///
	/// </summary>
	public class ClsInFieldMarker
	{
		private string m_XmlElementName;
		private string m_Begin;
		private ArrayList m_EndList;
		private bool m_fEndWithWord = false;
		private bool m_fEndWithField = false;
		private string m_Language;		// language to apply or string.empty
		private string m_xmlLang;		// actual ws for language
		private string m_style;			// style to apply or string.empty
		private bool m_ignore;			// true if this should be ignored
		private bool m_UseXmlLangValue;	// true if we should use the xmlLang value for language

		private char[] delim = new char[] { ' ' };	// , ';', ',', '\t' };

		public ClsInFieldMarker()
		{
			m_EndList = new ArrayList();
			m_Language = m_xmlLang = m_style = string.Empty;
			m_UseXmlLangValue = false;
		}

		public ClsInFieldMarker(string begin, string endList,
			bool fEndWithWord, bool fEndWithField, string lang, string xmlLang, string style, bool ignore)
		{
			m_EndList = new ArrayList();
			m_Begin = begin;
			// change from a delimited string to an arraylist
			STATICS.SplitString(endList, delim, ref m_EndList);
			m_fEndWithWord = fEndWithWord;
			m_fEndWithField = fEndWithField;
			m_Language = lang;
			m_xmlLang = xmlLang;	// string to use for the ws (writing System)
			m_style = style;
			m_ignore = ignore;
			m_UseXmlLangValue = false;
		}

		public string EndListToString()
		{
			string result="";
			if (m_EndList.Count > 0)
			{
				foreach (string marker in m_EndList)
				{
					if (result.Length > 0)
						result += " ";
					result += marker;
				}
			}
			else if (m_fEndWithField)
			{
				result = "<End Of Field>";
			}
			else if (m_fEndWithWord)
			{
				result = "<End Of Word>";
			}
			return result;
		}

		public string OptionsString()
		{
			string output = "";
			if (m_Language.Length > 0)
				output += " lang=\"" + m_Language + "\"";

			if (m_style.Length > 0)
				output += " style=\"" + m_style + "\"";

			if (m_ignore)
				output += " ignore=\"true\"";

			return output;
		}

		/// <summary>
		/// Convert the XML element name to a valid element name as needed.
		/// </summary>
		public void GenerateElementName(Hashtable htMarkers)
		{
			System.Text.StringBuilder sbElementName = new System.Text.StringBuilder(m_Begin + "_");
			if (m_EndList.Count > 0)
			{
				sbElementName.Append(EndListToString());
			}
			else
			{
				if (m_fEndWithField)
					sbElementName.Append("EndOfField");
				else
					sbElementName.Append("EndOfWord");
			}
			sbElementName = sbElementName.Replace("|", "Bar-");
			sbElementName = sbElementName.Replace(":", "Colon-");
			sbElementName = sbElementName.Replace("{", "-Open");
			sbElementName = sbElementName.Replace("}", "-Close");
			for (int ich = sbElementName.Length - 1; ich >= 0; --ich)
			{
				char ch = sbElementName[ich];	// sElementName[ich];
				if (Char.IsLetterOrDigit(ch) || ch == '.' || ch == '-' || ch == '_')
					continue;
				// Replace a (possibly) invalid char with it's Hexadecimal code.
				System.Text.StringBuilder sb = sbElementName.Remove(ich, 1);
				// Note that Uri.HexEscape puts a '%' character at the beginning, which is
				// invalid for XML element names, and unnecessary for our purposes.
				sbElementName = sb.Insert(ich, Uri.HexEscape(ch).Substring(1));
			}
			// Ensure tag starts with a letter or an underscore.  (LT-5316)
			if (!Char.IsLetter(sbElementName[0]) && sbElementName[0] != '_')
				sbElementName = sbElementName.Insert(0, "_");
			string sElementName = sbElementName.ToString();		// Get the string representation
			// make sure it's unique among the other element names
			if (htMarkers != null)
			{
				while (htMarkers.Contains(sElementName) && htMarkers[sElementName] != this)
				{
					sElementName += "_";
				}
				htMarkers[sElementName] = this;
			}
			m_XmlElementName = sElementName;
		}

		/// <summary>
		/// Modified to put out a string representation of this class, building the string
		/// using a stringbuilder object.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			if (m_XmlElementName == null)
				GenerateElementName(null);

			System.Text.StringBuilder sbOutput = new System.Text.StringBuilder();
			sbOutput.Append("element=\"");
			sbOutput.Append(m_XmlElementName);
			sbOutput.Append("\" begin=\"");
			// make sure the begin data is valid xml
			sbOutput.Append(m_Begin);
			sbOutput.Append("\"");

			if (m_EndList.Count > 0)
			{
				// only one end marker, so just put it out
				if (m_EndList.Count == 1)
				{
					sbOutput.Append(" end=\"");
					// make sure the end data is valid xml
					sbOutput.Append(EndListToString());
					sbOutput.Append("\"");
				}
				else
				{
					// put out the first one with the 'end' attribute
					sbOutput.Append(" end=\"");
					sbOutput.Append(m_EndList[0] as string);
					sbOutput.Append("\"");

					sbOutput.Append(" endList=\"");
					sbOutput.Append(EndListToString());
					sbOutput.Append("\"");
				}
			}
			else if (m_fEndWithField)
			{
				sbOutput.Append(" endWithField=\"");
				sbOutput.Append(m_fEndWithField.ToString());
				sbOutput.Append("\"");
			}
			else
			{
				sbOutput.Append(" endWithWord=\"");
				sbOutput.Append(m_fEndWithWord.ToString());
				sbOutput.Append("\"");
			}

			if (m_UseXmlLangValue)
			{
				// only put out if not empty
				if (m_xmlLang.Length > 0)
				{
					sbOutput.Append(" xml:lang=\"");
					sbOutput.Append(m_xmlLang);
					sbOutput.Append("\"");
				}
			}
			else
			{
				// only put out if not empty
				if (m_Language.Length > 0)
				{
					sbOutput.Append(" lang=\"");
					sbOutput.Append(m_Language);
					sbOutput.Append("\"");
				}
			}

			// only put out if not empty
			if (m_style.Length > 0)
			{
				sbOutput.Append(" style=\"");
				sbOutput.Append(m_style);
				sbOutput.Append("\"");
			}
			// only put out if true
			if (m_ignore)
			{
				sbOutput.Append(" ignore=\"true\"");
			}
			return sbOutput.ToString();
		}

		public string ElementAndAttributes()
		{
			System.Text.StringBuilder sbOutput = new System.Text.StringBuilder("<");
			sbOutput.Append(m_XmlElementName);

			// only put out if not empty
			if (m_Language.Length > 0)
			{
				sbOutput.Append(" ws=\"");
				sbOutput.Append(m_Language);
				sbOutput.Append("\"");
			}
			// only put out if not empty
			if (m_style.Length > 0)
			{
				sbOutput.Append(" namedStyle=\"");
				sbOutput.Append(m_style);
				sbOutput.Append("\"");
			}
			// only put out if true
			if (m_ignore)
			{
				sbOutput.Append(" ignore=\"true\"");
			}
			sbOutput.Append(">");
			return sbOutput.ToString();
		}

		private string GetSafeXMLString(string xml)
		{
			System.Text.StringBuilder result = new System.Text.StringBuilder(xml);
			result = result.Replace("&", "&amp;");
			result = result.Replace("<", "&lt;");
			result = result.Replace(">", "&gt;");
			return result.ToString();
		}

		public string ToXmlString()
		{
			string result = ToString();	// get the data portion of the string
			result = result.Replace("&", "&amp;");
			result = result.Replace("<", "&lt;");
			result = result.Replace(">", "&gt;");

			// add langDef element
			result = "<ifm " + result + "/>";
			return result;
		}

		public bool HasStyle { get { return m_style.Length > 0; }}
		public string Style { get { return m_style; }}
		public bool Ignore { get { return m_ignore; }}

		public string ElementName
		{
			get
			{
				// This is not always generated explicitly elsewhere.  See LT-6329.
				if (m_XmlElementName == null)
					GenerateElementName(null);
				return m_XmlElementName;
			}
		}

		public string Begin
		{
			get { return m_Begin; }
		}

		public ICollection End
		{
			get { return m_EndList; }
		}

		public bool ContainsEndMarker(string endMarker)
		{
			if (m_EndList.Contains(endMarker))
				return true;
			else if (m_fEndWithWord && endMarker == " ")
				return true;
			else
				return m_fEndWithField && endMarker.Length == 0;
		}

		public bool EndWithWord
		{
			get { return m_fEndWithWord; }
		}

		public bool EndWithField
		{
			get { return m_fEndWithField; }
		}

		public string KEY
		{
			get { return Begin; }
		}

		public string Language
		{
			get { return m_Language; }
		}

		public bool ReadXmlNode(System.Xml.XmlNode node, Hashtable languages)
		{
			bool Success = true;
			m_fEndWithField = false;
			m_fEndWithWord = false;
			foreach(System.Xml.XmlAttribute Attribute in node.Attributes)
			{
				// Create new attribute details, which may be altered later on:
				string NewName = Attribute.Name;
				string NewValue = Attribute.Value;
				switch (Attribute.Name)
				{
					case "element":
						m_XmlElementName = Attribute.Value;
						break;
					case "begin":
						m_Begin = Attribute.Value;
						break;
					case "end": case "endList":
						STATICS.SplitString(NewValue, delim, ref m_EndList);
						break;
					case "endWithField":
						m_fEndWithField = IsTrue(Attribute.Value);
						break;
					case "endWithWord":
						m_fEndWithWord = IsTrue(Attribute.Value);
						break;
						/*
					case "lang":
						// Look up replacement language name:
						ClsLanguage language = languages[Attribute.Value] as ClsLanguage;
						if (language == null)
						{
							Converter.Log.AddError("Error in Mapping File: Unknown 'lang' value '" + Attribute.Value + "' in the 'fieldDescriptions' section.");
							m_xmlLanguage = NewValue;
						}
						else
							m_xmlLanguage = language.XmlLang;

						m_Language = NewValue;
						break;
						 * */
					case "lang":
						NewValue = null;
						// Look up replacement language name:
						ClsLanguage language = languages[Attribute.Value] as ClsLanguage;
						if (language != null)
						{
							NewValue = language.XmlLang;
							m_Language = language.KEY;
							m_xmlLang = language.XmlLang;
						}
						else
						{
							Converter.Log.AddError(String.Format(Sfm2XmlStrings.UnknownLangValue0InTheInFieldMarkers, Attribute.Value));
							NewValue = Attribute.Value;
						}
						break;
					case "style":
						m_style = Attribute.Value;
						break;
					case "ignore":
						m_ignore = STATICS.IsBoolString(Attribute.Value, false);
						break;
					default:
						Converter.Log.AddWarning(String.Format(Sfm2XmlStrings.UnknownAttribute0InTheInFieldMarkers, Attribute.Name));
						Success = false;
						break;
				}
			}

			if (m_Begin == null)
			{
				Converter.Log.AddError(Sfm2XmlStrings.BeginNotDefinedInAnInFieldMarker);
				Success = false;
			}
			if (m_EndList.Count == 0 && !m_fEndWithWord && !m_fEndWithField)
			{
				Converter.Log.AddError(String.Format(Sfm2XmlStrings.InFieldMarker0HasNoEndAttribute, m_Begin));
				Success = false;
			}
			if (m_XmlElementName == null)
			{
				Converter.Log.AddError(String.Format(Sfm2XmlStrings.InFieldMarker0HasNoElementNameAttribute, m_Begin));
				Success = false;
			}

			return Success;
		}

		private bool IsTrue(string p)
		{
			bool f = true;
			if (p == f.ToString())
				return true;
			if (p.ToLowerInvariant() == "true")
				return true;
			if (p.ToLowerInvariant() == "yes")
				return true;
			return false;
		}

		public bool OutputXml(System.Xml.XmlTextWriter xmlOutput, bool useXmlLangValue)
		{
			if (xmlOutput != null)
			{
				m_UseXmlLangValue = useXmlLangValue;
				xmlOutput.WriteRaw(ToXmlString());
				xmlOutput.WriteRaw(System.Environment.NewLine);
				m_UseXmlLangValue = false;
			}
			return true;
		}

#if false
		public static void SplitString(string xyz, ref ArrayList list)
		{
			if (xyz == null || xyz.Length == 0)
				return;
			char[] delim = new char[] { ' ' } ;	// , ';', ',', '\t' };
			string[] values = xyz.Split(delim);
			foreach (string item in values)
			{
				// Make sure we're not dealing with adjacent delimiters or repeated substrings:
				if (item.Length > 0 && !list.Contains(item))
					list.Add(item);
			}
		}
#endif
	}
}
