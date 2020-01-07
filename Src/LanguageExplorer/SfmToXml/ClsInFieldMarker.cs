// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Text;
using System.Xml;

namespace LanguageExplorer.SfmToXml
{
	/// <summary />
	public class ClsInFieldMarker
	{
		private string m_xmlElementName;
		private ArrayList m_endList;
		private string m_xmlLang;       // actual ws for language
		private bool m_useXmlLangValue; // true if we should use the xmlLang value for language
		private char[] delim = { ' ' };

		public ClsInFieldMarker()
		{
			m_endList = new ArrayList();
			Language = m_xmlLang = Style = string.Empty;
			m_useXmlLangValue = false;
		}

		public ClsInFieldMarker(string begin, string endList, bool fEndWithWord, bool fEndWithField, string lang, string xmlLang, string style, bool ignore)
		{
			m_endList = new ArrayList();
			Begin = begin;
			// change from a delimited string to an arraylist
			SfmToXmlServices.SplitString(endList, delim, ref m_endList);
			EndWithWord = fEndWithWord;
			EndWithField = fEndWithField;
			Language = lang;
			m_xmlLang = xmlLang;    // string to use for the ws (writing System)
			Style = style;
			Ignore = ignore;
			m_useXmlLangValue = false;
		}

		public string EndListToString()
		{
			var result = string.Empty;
			if (m_endList.Count > 0)
			{
				foreach (string marker in m_endList)
				{
					if (result.Length > 0)
					{
						result += " ";
					}
					result += marker;
				}
			}
			else if (EndWithField)
			{
				result = "<End Of Field>";
			}
			else if (EndWithWord)
			{
				result = "<End Of Word>";
			}
			return result;
		}

		public string OptionsString()
		{
			var output = string.Empty;
			if (Language.Length > 0)
			{
				output += " lang=\"" + Language + "\"";
			}
			if (Style.Length > 0)
			{
				output += " style=\"" + Style + "\"";
			}
			if (Ignore)
			{
				output += " ignore=\"true\"";
			}
			return output;
		}

		/// <summary>
		/// Convert the XML element name to a valid element name as needed.
		/// </summary>
		public void GenerateElementName(Hashtable htMarkers)
		{
			var sbElementName = new StringBuilder(Begin + "_");
			if (m_endList.Count > 0)
			{
				sbElementName.Append(EndListToString());
			}
			else
			{
				sbElementName.Append(EndWithField ? "EndOfField" : "EndOfWord");
			}
			sbElementName = sbElementName.Replace("|", "Bar-");
			sbElementName = sbElementName.Replace(":", "Colon-");
			sbElementName = sbElementName.Replace("{", "-Open");
			sbElementName = sbElementName.Replace("}", "-Close");
			for (var ich = sbElementName.Length - 1; ich >= 0; --ich)
			{
				var ch = sbElementName[ich];   // sElementName[ich];
				if (char.IsLetterOrDigit(ch) || ch == '.' || ch == '-' || ch == '_')
				{
					continue;
				}
				// Replace a (possibly) invalid char with it's Hexadecimal code.
				var sb = sbElementName.Remove(ich, 1);
				// Note that Uri.HexEscape puts a '%' character at the beginning, which is
				// invalid for XML element names, and unnecessary for our purposes.
				sbElementName = sb.Insert(ich, Uri.HexEscape(ch).Substring(1));
			}
			// Ensure tag starts with a letter or an underscore.  (LT-5316)
			if (!char.IsLetter(sbElementName[0]) && sbElementName[0] != '_')
			{
				sbElementName = sbElementName.Insert(0, "_");
			}
			// Get the string representation
			var sElementName = sbElementName.ToString();
			// make sure it's unique among the other element names
			if (htMarkers != null)
			{
				while (htMarkers.Contains(sElementName) && htMarkers[sElementName] != this)
				{
					sElementName += "_";
				}
				htMarkers[sElementName] = this;
			}
			m_xmlElementName = sElementName;
		}

		/// <summary>
		/// Modified to put out a string representation of this class, building the string
		/// using a string builder object.
		/// </summary>
		public override string ToString()
		{
			if (m_xmlElementName == null)
			{
				GenerateElementName(null);
			}
			var sbOutput = new System.Text.StringBuilder();
			sbOutput.Append("element=\"");
			sbOutput.Append(m_xmlElementName);
			sbOutput.Append("\" begin=\"");
			// make sure the begin data is valid xml
			sbOutput.Append(Begin);
			sbOutput.Append("\"");
			if (m_endList.Count > 0)
			{
				// only one end marker, so just put it out
				if (m_endList.Count == 1)
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
					sbOutput.Append(m_endList[0] as string);
					sbOutput.Append("\"");

					sbOutput.Append(" endList=\"");
					sbOutput.Append(EndListToString());
					sbOutput.Append("\"");
				}
			}
			else if (EndWithField)
			{
				sbOutput.Append(" endWithField=\"");
				sbOutput.Append(EndWithField.ToString());
				sbOutput.Append("\"");
			}
			else
			{
				sbOutput.Append(" endWithWord=\"");
				sbOutput.Append(EndWithWord.ToString());
				sbOutput.Append("\"");
			}

			if (m_useXmlLangValue)
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
				if (Language.Length > 0)
				{
					sbOutput.Append(" lang=\"");
					sbOutput.Append(Language);
					sbOutput.Append("\"");
				}
			}
			// only put out if not empty
			if (Style.Length > 0)
			{
				sbOutput.Append(" style=\"");
				sbOutput.Append(Style);
				sbOutput.Append("\"");
			}
			// only put out if true
			if (Ignore)
			{
				sbOutput.Append(" ignore=\"true\"");
			}
			return sbOutput.ToString();
		}

		public string ElementAndAttributes()
		{
			var sbOutput = new StringBuilder("<");
			sbOutput.Append(m_xmlElementName);
			// only put out if not empty
			if (Language.Length > 0)
			{
				sbOutput.Append(" ws=\"");
				sbOutput.Append(Language);
				sbOutput.Append("\"");
			}
			// only put out if not empty
			if (Style.Length > 0)
			{
				sbOutput.Append(" namedStyle=\"");
				sbOutput.Append(Style);
				sbOutput.Append("\"");
			}
			// only put out if true
			if (Ignore)
			{
				sbOutput.Append(" ignore=\"true\"");
			}
			sbOutput.Append(">");
			return sbOutput.ToString();
		}

		public string ToXmlString()
		{
			var result = ToString(); // get the data portion of the string
			result = result.Replace("&", "&amp;");
			result = result.Replace("<", "&lt;");
			result = result.Replace(">", "&gt;");
			// add langDef element
			result = $"<ifm {result}/>";
			return result;
		}

		public bool HasStyle => Style.Length > 0;

		public string Style { get; private set; }

		public bool Ignore { get; private set; }

		public string ElementName
		{
			get
			{
				// This is not always generated explicitly elsewhere.  See LT-6329.
				if (m_xmlElementName == null)
				{
					GenerateElementName(null);
				}
				return m_xmlElementName;
			}
		}

		public string Begin { get; private set; }

		public ICollection End => m_endList;

		public bool ContainsEndMarker(string endMarker)
		{
			if (m_endList.Contains(endMarker))
			{
				return true;
			}
			if (EndWithWord && endMarker == " ")
			{
				return true;
			}
			return EndWithField && endMarker.Length == 0;
		}

		public bool EndWithWord { get; private set; }

		public bool EndWithField { get; private set; }

		public string KEY => Begin;

		public string Language { get; private set; }

		public bool ReadXmlNode(XmlNode node, Hashtable languages)
		{
			var success = true;
			EndWithField = false;
			EndWithWord = false;
			foreach (XmlAttribute attribute in node.Attributes)
			{
				// Create new attribute details, which may be altered later on:
				var newValue = attribute.Value;
				switch (attribute.Name)
				{
					case "element":
						m_xmlElementName = attribute.Value;
						break;
					case "begin":
						Begin = attribute.Value;
						break;
					case "end":
					case "endList":
						SfmToXmlServices.SplitString(newValue, delim, ref m_endList);
						break;
					case "endWithField":
						EndWithField = IsTrue(attribute.Value);
						break;
					case "endWithWord":
						EndWithWord = IsTrue(attribute.Value);
						break;
					case "lang":
						// Look up replacement language name:
						var language = languages[attribute.Value] as ClsLanguage;
						if (language != null)
						{
							Language = language.KEY;
							m_xmlLang = language.XmlLang;
						}
						else
						{
							Converter.Log.AddError(string.Format(SfmToXmlStrings.UnknownLangValue0InTheInFieldMarkers, attribute.Value));
						}
						break;
					case "style":
						Style = attribute.Value;
						break;
					case "ignore":
						Ignore = SfmToXmlServices.IsBoolString(attribute.Value, false);
						break;
					default:
						Converter.Log.AddWarning(string.Format(SfmToXmlStrings.UnknownAttribute0InTheInFieldMarkers, attribute.Name));
						success = false;
						break;
				}
			}
			if (Begin == null)
			{
				Converter.Log.AddError(SfmToXmlStrings.BeginNotDefinedInAnInFieldMarker);
				success = false;
			}
			if (m_endList.Count == 0 && !EndWithWord && !EndWithField)
			{
				Converter.Log.AddError(string.Format(SfmToXmlStrings.InFieldMarker0HasNoEndAttribute, Begin));
				success = false;
			}
			if (m_xmlElementName == null)
			{
				Converter.Log.AddError(string.Format(SfmToXmlStrings.InFieldMarker0HasNoElementNameAttribute, Begin));
				success = false;
			}
			return success;
		}

		private static bool IsTrue(string p)
		{
			return p == true.ToString() || p.ToLowerInvariant() == "true" || p.ToLowerInvariant() == "yes";
		}

		public bool OutputXml(XmlTextWriter xmlOutput, bool useXmlLangValue)
		{
			if (xmlOutput != null)
			{
				m_useXmlLangValue = useXmlLangValue;
				xmlOutput.WriteRaw(ToXmlString());
				xmlOutput.WriteRaw(Environment.NewLine);
				m_useXmlLangValue = false;
			}
			return true;
		}
	}
}