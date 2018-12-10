// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

namespace LanguageExplorer.SfmToXml
{
	/// <summary>
	/// This class implements the data structure for the "fieldDescriptions" mapping element.
	/// This element contains all the "field" elements that can be in the input data file.
	/// Attributes of "field":
	/// - sfm : this is the text of the field / marker that is used in the input file.
	/// </summary>
	public class ClsFieldDescription
	{
		// this static variable is used to track all new sfms
		private static int g_nextNewSFM = 1;
		private static string NextNewSFM => $"Auto_SFM_{g_nextNewSFM++}";
		private const string DefaultTopAnalysisWS = "en";
		protected string m_AutoSfm;         // used if the sfm has invalid characters in it
		protected string m_xmlLanguage;     // this is the one for the passed in value that goes out with xml:lang
		protected string m_Meaning;         // used to maintain the meaning sub element
		protected string m_OtherAttributes; // used to keep all other field attributes
		protected bool m_Abbr;              // true if it is an abbreviation
		protected string m_RefFunc;         // value if there is a 'func' property also known as a 'ref' property
		protected string m_RefFuncWS;       // the ws for the above property
		protected string m_autoFieldClass;  // used for xml lang output of multiple auto fields
		protected Dictionary<string, string> m_autofieldInfo;    // key=string[className], value=string[fwDest]

		public ClsFieldDescription(string marker, string name, string datatype, string lang, bool abbr, string fwID)
		{
			Init();
			SFM = marker;
			Name = name;
			Type = datatype;
			Language = lang;
			if (abbr)
			{
				IsAbbr = abbr;  // need to set both member variables correctly - use the property
			}
			else
			{
				IsAbbrField = false;   // not a field with this element
				m_Abbr = true;          // default to true
			}
			MeaningApp = "fw.sil.org";
			MeaningId = MakeValidFwId(fwID);
			m_Meaning = $"<meaning app=\"{MeaningApp}\" id=\"{MeaningId}\"/>";
			if (fwID == string.Empty)
			{
				IsAutoImportField = true;
			}
			SetAutoSfm();
		}

		public ClsFieldDescription()
		{
			Init();
			SetAutoSfm();
		}

		public ClsFieldDescription(string sfm)
		{
			Init();
			SFM = sfm;
			SetAutoSfm();
		}

		private void Init()
		{
			MeaningApp = string.Empty;
			MeaningId = string.Empty;
			m_OtherAttributes = string.Empty;
			IsAbbrField = false;   // not a field with this element
			m_Abbr = true;          // default to true
			IsExcluded = false;     // not excluded by default
			IsAutoImportField = false;   // not auto import by default
			m_RefFunc = string.Empty;         // empty is same as none
			m_RefFuncWS = string.Empty;
			m_Meaning = "<meaning/>";
			m_xmlLanguage = string.Empty;
			m_autoFieldClass = string.Empty;
			m_autofieldInfo = new Dictionary<string, string>();
		}

		protected string MakeValidFwId(string value)
		{
			var result = value;
			switch (value)
			{
				case "subd":    // Subentry (Derivation)
					m_RefFunc = "Derivative";
					result = "sub"; // new value
					break;
				case "subc":    // Subentry (Compound)
					m_RefFunc = "Compound";
					result = "sub"; // new value
					break;
				case "subi":    // Subentry (Idiom)
					m_RefFunc = "Idiom";
					result = "sub"; // new value
					break;
				case "subk":    // Subentry (Keyterm Phrase)
					m_RefFunc = "";
					result = "sub"; // new value
					break;
				case "subpd":   // Subentry (Phrasal Verb)
					m_RefFunc = "Phrasal Verb";
					result = "sub"; // new value
					break;
				case "subs":    // Subentry (Saying)
					m_RefFunc = "Saying";
					result = "sub"; // new value
					break;
				case "vard":    // Variant (Dialectal)
					m_RefFunc = "Dialectal Variant";
					result = "var"; // new value
					break;
				case "varf":    // Variant (Free)
					m_RefFunc = "Free Variant";
					result = "var"; // new value
					break;
				case "vari":    // Variant (Inflectional)
					m_RefFunc = "Irregularly Inflected Form";
					result = "var"; // new value
					break;
				case "vars":    // Variant (Spelling)
					m_RefFunc = "Spelling Variant";
					result = "var"; // new value
					break;
				case "varc":    // Variant (Comment)
					break;
			}
			return result;
		}

		protected void SetAutoSfm()
		{
			if (ContainsInvalidSFMCharacters) // always have to use new sfm for element name
			{
				m_AutoSfm = NextNewSFM;
			}
			else if (IsValidSFMName)
			{
				m_AutoSfm = string.Empty;
			}
			// LT-3692 Handle markers that begin with a numeric value
			else if (SFM != null && char.IsDigit(SFM, 0))
			{
				m_AutoSfm = $"SFM_{SFM}";
			}
			// default auto sfm marker text
			else
			{
				m_AutoSfm = NextNewSFM;
			}
		}


		/// <summary>
		/// Test the sfm marker characters for validness in the xml elements.
		/// </summary>
		/// <returns>
		/// true if the sfm marker contains invalid characters to be used as
		/// part of the element name in the xml output.
		/// </returns>
		private bool ContainsInvalidSFMCharacters
		{
			get
			{
				var valid = true;
				if (SFM != null)
				{
					foreach (var c in SFM)
					{
						if (char.IsPunctuation(c) || char.IsControl(c))
						{
							valid = false;
							break;
						}
					}
				}
				return !valid;
			}
		}

		private bool IsValidSFMName
		{
			get
			{
				const string invalidChars = @"?()&\/";
				// LT-3692 Handle markers that begin with a numeric value
				// - special as they aren't valid xml element names by themselves
				return SFM == null || SFM.IndexOfAny(invalidChars.ToCharArray(), 0, SFM.Length) < 0 && !char.IsDigit(SFM, 0);
			}
		}

		public void AddAutoFieldInfo(string className, string fwDest)
		{
			if (!m_autofieldInfo.ContainsKey(className))
			{
				m_autofieldInfo.Add(className, fwDest);
			}
		}

		public string SFM { get; protected set; }

		public string SFMxmlSafe => m_AutoSfm.Length > 0 ? m_AutoSfm : SFM;

		public string KEY => SFM;

		public string Name { get; protected set; }

		public string Language { get; protected set; }

		public void UpdateLanguageValues(string longName, string shortName)
		{
			Language = longName;
			m_xmlLanguage = shortName;
		}

		public string Type { get; set; }

		public string MeaningId { get; set; }

		public bool IsAbbrField { get; protected set; }

		public bool IsAbbr
		{
			get
			{
				return IsAbbrField && m_Abbr;
			}
			set
			{
				IsAbbrField = true;
				m_Abbr = value;
			}
		}

		public bool IsExcluded { get; set; }

		public bool IsAutoImportField { get; set; }

		public bool IsRef => m_RefFunc.Length > 0;

		public void ClearRef()
		{
			m_RefFunc = m_RefFuncWS = string.Empty;
		}

		public string RefFunc
		{
			get
			{
				return m_RefFunc;
			}
			set
			{
				m_RefFunc = value;
				RebuildMeaningEntry(null, DefaultTopAnalysisWS);
			}
		}

		public string RefFuncWS
		{
			get
			{
				return m_RefFuncWS;
			}
			set
			{
				m_RefFuncWS = value;
				RebuildMeaningEntry(null, DefaultTopAnalysisWS);
			}
		}

		private void RebuildMeaningEntry(XmlTextWriter xmlOutput, string topAnalysisWS)
		{
			m_Meaning = $"<meaning app=\"{MeaningApp}\" id=\"{MeaningID}\"";
			if (IsRef || MeaningId == "funold")
			{
				m_Meaning += " funcWS=\"";
				if (m_RefFuncWS != string.Empty)
				{
					m_Meaning += m_RefFuncWS;
				}
				else
				{
					m_Meaning += topAnalysisWS;
				}
				if (IsRef)
				{
					m_Meaning += $"\" func=\"{m_RefFunc}\"";
				}
				else
				{
					m_Meaning += "\"";
				}
			}
			m_Meaning += "/>";
			if (xmlOutput != null)
			{
				xmlOutput.WriteStartElement("meaning");
				xmlOutput.WriteAttributeString("app", MeaningApp);
				xmlOutput.WriteAttributeString("id", MeaningId);
				if (IsRef || MeaningId == "funold")
				{
					xmlOutput.WriteAttributeString("funcWS", m_RefFuncWS != string.Empty ? m_RefFuncWS : topAnalysisWS);
					if (IsRef)
					{
						xmlOutput.WriteAttributeString("func", m_RefFunc);
					}
				}
				xmlOutput.WriteEndElement();
			}
		}

		public string MeaningApp { get; protected set; }

		public string MeaningID
		{
			get
			{
				return MeaningId;
			}
			set
			{
				MeaningId = MakeValidFwId(value);
				RebuildMeaningEntry(null, DefaultTopAnalysisWS);
			}
		}

		public override string ToString()
		{
			return SFM;
		}

		public string ToXmlString()
		{
			return ToXmlBaseString(false, null);
		}

		public string ToXmlLangString(XmlTextWriter xmlOutput)
		{
			if (m_autofieldInfo.Count == 0)
			{
				return ToXmlBaseString(true, xmlOutput);
			}
			// save the orig values
			var origMeaningId = MeaningID;
			var outputData = string.Empty;
			var count = m_autofieldInfo.Count;
			foreach (var classSfmKvp in m_autofieldInfo)
			{
				MeaningID = classSfmKvp.Value;
				m_autoFieldClass = classSfmKvp.Key;
				outputData += ToXmlBaseString(true, xmlOutput);
				if (count > 1)
				{
					outputData += Environment.NewLine;
				}
				count--;
			}
			m_autoFieldClass = string.Empty;
			MeaningID = origMeaningId;
			return outputData;
		}

		protected virtual string ToXmlBaseString(bool useXMLLang, XmlTextWriter xmlOutput)
		{
			var result = "<field ";
			result += $"sfm=\"{SFM}\" ";
			if (m_AutoSfm.Length > 0)
			{
				result += $"autoSfm=\"{m_AutoSfm}\" ";
			}
			result += $"name=\"{Name}\" ";
			result += $"type=\"{Type}\" ";
			if (xmlOutput != null)
			{
				xmlOutput.WriteStartElement("field");
				xmlOutput.WriteAttributeString("sfm", SFM);
				if (m_AutoSfm.Length > 0)
				{
					xmlOutput.WriteAttributeString("autoSfm", m_AutoSfm);
				}
				xmlOutput.WriteAttributeString("name", Name);
				xmlOutput.WriteAttributeString("type", Type);
			}

			if (useXMLLang)
			{
				result += $"xml:lang=\"{m_xmlLanguage}\" ";
				xmlOutput?.WriteAttributeString("xml:lang", m_xmlLanguage);
			}
			else
			{
				result += $"lang=\"{Language}\" ";
				xmlOutput?.WriteAttributeString("lang", Language);
			}

			if (IsAbbrField)    // only put out if it's a field that can have the abbr attribute
			{
				result += "abbr=\"";
				if (m_Abbr)
				{
					result += "True";
				}
				else
				{
					result += "False";
				}
				result += "\" ";
				xmlOutput?.WriteAttributeString("abbr", m_Abbr ? "True" : "False");
			}

			if (IsExcluded) // only put out if true
			{
				result += "exclude=\"True\" ";
				xmlOutput?.WriteAttributeString("exclude", "True");
			}

			if (IsAutoImportField)  // only put out if true
			{
				result += "autoImport=\"True\" ";
				xmlOutput?.WriteAttributeString("autoImport", "True");

				if (m_autoFieldClass.Length > 0)
				{
					result += $"autoImportClassName=\"{m_autoFieldClass}\" ";
					xmlOutput?.WriteAttributeString("autoImportClassName", m_autoFieldClass);
				}
			}

			result += m_OtherAttributes + ">" + System.Environment.NewLine;
			result += m_Meaning + Environment.NewLine;
			result += "</field>";
			if (xmlOutput != null)
			{
				if (m_OtherAttributes.Length > 0)
				{
					xmlOutput.WriteRaw(m_OtherAttributes);
				}
				RebuildMeaningEntry(xmlOutput, DefaultTopAnalysisWS);       // puts out the meaning element
				xmlOutput.WriteEndElement();        // end the field element
			}
			return result;
		}


		public bool Output(Hashtable languages, XmlTextWriter xmlOutput, ref Hashtable langsToIgnore, ref Hashtable fieldsToIgnore)
		{
			if (Language != null && langsToIgnore.ContainsKey(Language))
			{
				fieldsToIgnore.Add(SFM, null);
				Converter.Log.AddWarning(string.Format(SfmToXmlStrings.FieldDescWithSFMOf0IsBeingIGNORED, SFM));
				return false;   // no output for this field
			}
			// just add it to the list of fields to ignore and allow everything else to process the same
			if (IsExcluded)
			{
				fieldsToIgnore.Add(SFM, null);
			}
			if (xmlOutput != null)
			{
				ToXmlLangString(xmlOutput);
			}
			return true;
		}


		public bool ReadAndOutputXmlNode(XmlNode fieldNode, Hashtable languages, string topAnalysisWS, XmlTextWriter xmlOutput, ref Hashtable langsToIgnore, ref Hashtable fieldsToIgnore)
		{
			var rval = false;
			if (ReadXmlNode(fieldNode, languages, topAnalysisWS))
			{
				rval = Output(languages, xmlOutput, ref langsToIgnore, ref fieldsToIgnore);
			}
			return rval;
		}


		public virtual bool ReadXmlNode(XmlNode fieldNode, Hashtable languages, string topAnalysisWS)
		{
			m_OtherAttributes = string.Empty;
			foreach (XmlAttribute attribute in fieldNode.Attributes)
			{
				// Create new attribute details, which may be altered later on:
				var newName = attribute.Name;
				var newValue = attribute.Value;
				switch (attribute.Name)
				{
					case "sfm":
						SFM = attribute.Value;
						break;
					case "name":
						Name = attribute.Value;
						break;
					case "type":
						Type = attribute.Value;
						break;
					case "lang":
						// Look up replacement language name:
						var language = languages[attribute.Value] as ClsLanguage;
						if (language == null)
						{
							Converter.Log.AddError(string.Format(SfmToXmlStrings.UnknownLangValue0InFieldDescs, attribute.Value));
							m_xmlLanguage = newValue;
						}
						else
						{
							m_xmlLanguage = language.XmlLang;
						}
						Language = newValue;
						break;
					case "abbr":
						IsAbbr = SfmToXmlServices.IsBoolString(attribute.Value, true);
						break;
					case "exclude":
						IsExcluded = SfmToXmlServices.IsBoolString(attribute.Value, false);
						break;
					case "autoImport":
						IsAutoImportField = SfmToXmlServices.IsBoolString(attribute.Value, false);
						break;
					case "autoSfm": // just ignore for now and re-assign
						break;
					default:
						m_OtherAttributes += " " + newName + "=\"" + newValue + "\"";
						break;
				}
			}

			// Iterate through all the attributes of the "meaning" sub-element of this field:
			var meaning = fieldNode.SelectSingleNode("meaning");
			if (meaning == null)
			{
				Converter.Log.AddError(string.Format(SfmToXmlStrings.MissingMeaningElementInField0InFieldDescs, SFM));
			}
			else
			{
				m_Meaning = "<meaning ";

				foreach (XmlAttribute attribute in meaning.Attributes)
				{
					m_Meaning += attribute.Name + "=\"" + attribute.Value + "\" ";
					switch (attribute.Name)
					{
						case "app":
							MeaningApp = attribute.Value;
							break;
						case "id":
							MeaningId = MakeValidFwId(attribute.Value);
							break;
						case "func":
							m_RefFunc = attribute.Value;
							break;
						case "funcWS":
							m_RefFuncWS = attribute.Value;
							break;
					}
				}
				m_Meaning += " />";

				RebuildMeaningEntry(null, topAnalysisWS);
			}

			SetAutoSfm();
			if (SFM == null)
			{
				Converter.Log.AddError(SfmToXmlStrings.FieldDefinedWithNoSfmAttributeInTheFieldDescs);
			}
			else if (Language == null)
			{
				Converter.Log.AddError(string.Format(SfmToXmlStrings.FieldDescWithSFMOf0HasNoLangAttribute, SFM));
			}
			if (Type == null)
			{
				Converter.Log.AddWarning(string.Format(SfmToXmlStrings.FieldDescWithSFMOf0HasNoTypeAttribute, SFM));
			}
			else
			{
				return true;
			}
			return false;
		}
	}
}