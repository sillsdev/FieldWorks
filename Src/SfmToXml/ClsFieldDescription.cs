// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;

namespace Sfm2Xml
{
	/// <summary>
	/// This class implements the data structure for the "fieldDescriptions" mapping element.
	/// This element contains all the "field" elements that can be in the input data file.
	/// Attributes of "field":
	/// - sfm : this is the text of the field / marker that is used in the input file.
	///
	/// </summary>
	public class ClsFieldDescription
	{
		// this static variable is used to track all new sfms
		static private int g_nextNewSFM = 1;
		static public string NextNewSFM()
		{
			int id = g_nextNewSFM++;
			return "Auto_SFM_" + id.ToString();
		}
		static private string DefaultTopAnalysisWS = "en";

		protected string m_Sfm;
		protected string m_AutoSfm;			// used if the sfm has invalid characters in it
		protected string m_Name;
		protected string m_Language;
		protected string m_xmlLanguage;		// this is the one for the passed in value that goes out with xml:lang
		protected string m_Type;
//		private string m_FieldAttributes;	// info on the field attributes
		protected string m_Meaning;			// used to maintain the meaning sub element
		protected string m_OtherAttributes;	// used to keep all other field attributes
		protected bool m_AbbrOrName;			// true if this field uses the 'abbr' attribute whose value is stored in m_Abbr
		protected bool m_Abbr;				// true if it is an abbreviation
		protected bool m_Excluded;			// true if it is excluded from import
		protected bool m_AutoImport;			// true if this field is to be auto imported
		protected string m_RefFunc;			// value if there is a 'func' property also known as a 'ref' property
		protected string m_RefFuncWS;		// the ws for the above property
		protected string m_meaningApp;		// app attribute of meaning element
		protected string m_meaningId;			// id attribute of meaning element
		protected string m_autoFieldClass;	// used for xml lang output of multiple auto fields
		protected Hashtable m_autofieldInfo;	// key=string[className], value=string[fwDest]

		protected ClsFieldDescription(string marker, string name, string datatype, string lang, bool abbr, string fwID)
		{
			Init();
			m_Sfm = marker;
			m_Name = name;
			m_Type = datatype;
			m_Language = lang;
			// m_Abbr = abbr;
			if (abbr)
				IsAbbr = abbr;	// need to set both member variables correctly - use the property
			else
			{
				m_AbbrOrName = false;	// not a field with this element
				m_Abbr = true;			// default to true
			}
			m_meaningApp = "fw.sil.org";
			m_meaningId = MakeValidFwId(fwID);
			m_Meaning = "<meaning app=\"" + m_meaningApp + "\" id=\"" + m_meaningId + "\"/>";
			if (fwID == "")
				IsAutoImportField = true;
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
			m_Sfm = sfm;
			SetAutoSfm();
		}

		private void Init()
		{
			m_meaningApp = "";
			m_meaningId = "";
			m_OtherAttributes = "";
			m_AbbrOrName = false;	// not a field with this element
			m_Abbr = true;			// default to true
			m_Excluded = false;		// not excluded by default
			m_AutoImport = false;	// not auto import by default
			m_RefFunc = "";			// empty is same as none
			m_RefFuncWS = "";
			m_Meaning = "<meaning/>";
			m_xmlLanguage = "";
			m_autoFieldClass = "";
			m_autofieldInfo = new Hashtable();
		}

		protected string MakeValidFwId(string value)
		{
			string result = value;
			switch (value)
			{
				//case "lxrel":	// Lexical relation
				//case "cref":	// Cross reference fields
				//    if (data.RefFuncWS == string.Empty)
				//        data.RefFuncWS = topAnalysis;	// dont let it be blank for old map files
				//    break;

				case "subd":	// Subentry (Derivation)
					m_RefFunc = "Derivative";
					result = "sub";	// new value
					break;
				case "subc":	// Subentry (Compound)
					m_RefFunc = "Compound";
					result = "sub";	// new value
					break;
				case "subi":	// Subentry (Idiom)
					m_RefFunc = "Idiom";
					result = "sub";	// new value
					break;
				case "subk":	// Subentry (Keyterm Phrase)
					m_RefFunc = "";
					result = "sub";	// new value
					break;
				case "subpd":	// Subentry (Phrasal Verb)
					m_RefFunc = "Phrasal Verb";
					result = "sub";	// new value
					break;
				case "subs":	// Subentry (Saying)
					m_RefFunc = "Saying";
					result = "sub";	// new value
					break;
					//data.RefFuncWS = topAnalysis;

				case "vard":	// Variant (Dialectal)
					m_RefFunc = "Dialectal Variant";
					result = "var";	// new value
					break;
				case "varf":	// Variant (Free)
					m_RefFunc = "Free Variant";
					result = "var";	// new value
					break;
				case "vari":	// Variant (Inflectional)
					m_RefFunc = "Irregularly Inflected Form";
					result = "var";	// new value
					break;
				case "vars":	// Variant (Spelling)
					m_RefFunc = "Spelling Variant";
					result = "var";	// new value
					break;
				case "varc":	// Variant (Comment)
					//data.RefFuncWS = topAnalysis;
					break;
			}
			return result;
		}


		protected void SetAutoSfm()
		{
			if (ContainsInvalidSFMCharacters())	// always have to use new sfm for element name
				m_AutoSfm = NextNewSFM();
			else if (IsValidSFMName())
				m_AutoSfm = "";
			// LT-3692 Handle markers that begin with a numeric value
			else if (m_Sfm != null && char.IsDigit(m_Sfm, 0))
				m_AutoSfm = "SFM_" + m_Sfm;
			// default auto sfm marker text
			else
				m_AutoSfm = NextNewSFM();
		}

		/// <summary>
		/// Test the sfm marker characters for validness in the xml elements.
		/// </summary>
		/// <returns>
		/// true if the sfm marker contains invalid characters to be used as
		/// part of the element name in the xml output.
		/// </returns>
		private bool ContainsInvalidSFMCharacters()
		{
			bool valid = true;
			if (m_Sfm != null)
			{
				foreach (char c in m_Sfm)
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

		private bool IsValidSFMName()
		{
			string invalidChars = @"?()&\/";
			char[] anyOf = invalidChars.ToCharArray();

			// LT-3692 Handle markers that begin with a numeric value
			// - special as they aren't valid xml element names by themselves
			if (m_Sfm != null && (m_Sfm.IndexOfAny(anyOf, 0, m_Sfm.Length) >= 0 || char.IsDigit(m_Sfm, 0)))
				return false;
			return true;
		}


		public void AddAutoFieldInfo(string className, string fwDest)
		{
			if (!m_autofieldInfo.ContainsKey(className))
				m_autofieldInfo.Add(className, fwDest);
		}

		public string SFM
		{
			get { return m_Sfm; }
		}

		public string SFMxmlSafe
		{
			get
			{
				if (m_AutoSfm.Length > 0)
					return m_AutoSfm;
				return SFM;
			}
		}

		public string KEY
		{
			get { return SFM; }
		}

		public string Name
		{
			get { return m_Name; }
		}

		public string Language
		{
			get { return m_Language; }
		}

		public void UpdateLanguageValues(string longName, string shortName)
		{
			m_Language = longName;
			m_xmlLanguage = shortName;
		}

		public string Type
		{
			get { return m_Type; }
			set { m_Type = value; }
		}

		public string MeaningId
		{
			get { return m_meaningId; }
			set { m_meaningId = value; }
		}

		public bool IsAbbrField
		{
			get { return m_AbbrOrName;}
		}
		public bool IsAbbr
		{
			get { if (m_AbbrOrName) return m_Abbr; return false;}
			set { m_AbbrOrName = true; m_Abbr = value; }
		}
		public bool IsExcluded
		{
			get { return m_Excluded; }
			set { m_Excluded = value; }
		}
		public bool IsAutoImportField
		{
			get { return m_AutoImport; }
			set { m_AutoImport = value; }
		}
		public bool IsRef
		{
			get { return m_RefFunc.Length > 0; }
		}
		public void ClearRef()
		{
			m_RefFunc = m_RefFuncWS = "";
		}
		public string RefFunc
		{
			get { return m_RefFunc; }
			set
			{
				m_RefFunc = value;
				RebuildMeaningEntry(null, DefaultTopAnalysisWS);
			}
		}
		public string RefFuncWS
		{
			get { return m_RefFuncWS; }
			set
			{
				m_RefFuncWS = value;
				RebuildMeaningEntry(null, DefaultTopAnalysisWS);
			}
		}

		private void RebuildMeaningEntry(System.Xml.XmlTextWriter xmlOutput, string topAnalysisWS)
		{
			m_Meaning = "<meaning app=\"" + m_meaningApp + "\" id=\"" + m_meaningId + "\"";
			if (IsRef || MeaningId == "funold")
			{
				m_Meaning += " funcWS=\"";
				if (m_RefFuncWS != string.Empty)
					m_Meaning += m_RefFuncWS;
				else
					m_Meaning += topAnalysisWS;
				if (IsRef)
					m_Meaning += "\" func=\"" + m_RefFunc + "\"";
				else
					m_Meaning += "\"";
			}
			m_Meaning += "/>";
			if (xmlOutput != null)
			{
				xmlOutput.WriteStartElement("meaning");
				xmlOutput.WriteAttributeString("app", m_meaningApp);
				xmlOutput.WriteAttributeString("id", m_meaningId);
				if (IsRef || MeaningId == "funold")
				{
					if (m_RefFuncWS != string.Empty)
						xmlOutput.WriteAttributeString("funcWS", m_RefFuncWS);
					else
						xmlOutput.WriteAttributeString("funcWS", topAnalysisWS);

					if (IsRef)
						xmlOutput.WriteAttributeString("func", m_RefFunc);
				}
				xmlOutput.WriteEndElement();
			}
		}

		public string MeaningApp { get { return m_meaningApp; }}// wont ever be null
		public string MeaningID
		{
			get { return m_meaningId; }
			set
			{
				m_meaningId = MakeValidFwId(value);
				RebuildMeaningEntry(null, DefaultTopAnalysisWS);
			}
		}

		public override string ToString()
		{
			return m_Sfm;
		}

		public string ToXmlString() { return ToXmlBaseString(false, null); }
		public string ToXmlLangString(System.Xml.XmlTextWriter xmlOutput)
		{
			if (m_autofieldInfo.Count == 0)
				return ToXmlBaseString(true, xmlOutput);

			// save the orig values
			string origMeaningId = MeaningID;
			string outputData = "";
			int count = m_autofieldInfo.Count;
			foreach (DictionaryEntry classsfm in m_autofieldInfo)
			{
				MeaningID = classsfm.Value as string;
				m_autoFieldClass = classsfm.Key as string;

				outputData += ToXmlBaseString(true, xmlOutput);
				if (count > 1)
					outputData += Environment.NewLine;
				count--;
			}
			m_autoFieldClass = "";
			MeaningID = origMeaningId;
			return outputData;
		}

		protected string ToXmlBaseString(bool useXMLLang, System.Xml.XmlTextWriter xmlOutput)
		{
			string result = "<field ";
			result += "sfm=\"" + m_Sfm + "\" ";

			if (m_AutoSfm.Length > 0)
				result += "autoSfm=\"" + m_AutoSfm + "\" ";

			result += "name=\"" + m_Name + "\" ";
			result += "type=\"" + m_Type + "\" ";
			if (xmlOutput != null)
			{
				xmlOutput.WriteStartElement("field");
				xmlOutput.WriteAttributeString("sfm", m_Sfm);
				if (m_AutoSfm.Length > 0)
					xmlOutput.WriteAttributeString("autoSfm", m_AutoSfm);
				xmlOutput.WriteAttributeString("name", m_Name);
				xmlOutput.WriteAttributeString("type", m_Type);
			}

			if (useXMLLang)
			{
				result += "xml:lang=\"" + m_xmlLanguage + "\" ";
				if (xmlOutput != null)
					xmlOutput.WriteAttributeString("xml:lang", m_xmlLanguage);
			}
			else
			{
				result += "lang=\"" + m_Language + "\" ";
				if (xmlOutput != null)
					xmlOutput.WriteAttributeString("lang", m_Language);
			}

			if (IsAbbrField)	// only put out if it's a field that can have the abbr attribute
			{
				result += "abbr=\"";
				if (m_Abbr)
					result += "True";
				else
					result += "False";
				result += "\" ";

				if (xmlOutput != null)
					xmlOutput.WriteAttributeString("abbr", m_Abbr?"True":"False");
			}

			if (IsExcluded)	// only put out if true
			{
				result += "exclude=\"True\" ";
				if (xmlOutput != null)
					xmlOutput.WriteAttributeString("exclude", "True");
			}

			if (IsAutoImportField)	// only put out if true
			{
				result += "autoImport=\"True\" ";
				if (xmlOutput != null)
					xmlOutput.WriteAttributeString("autoImport", "True");

				if (m_autoFieldClass.Length > 0)
				{
					result += "autoImportClassName=\"" + m_autoFieldClass + "\" ";
					if (xmlOutput != null)
						xmlOutput.WriteAttributeString("autoImportClassName", m_autoFieldClass);
				}
			}

			result += m_OtherAttributes + ">" + System.Environment.NewLine;
			result += m_Meaning + System.Environment.NewLine;
			result += "</field>";
			if (xmlOutput != null)
			{
				if (m_OtherAttributes.Length > 0)
					xmlOutput.WriteRaw(m_OtherAttributes);
				RebuildMeaningEntry(xmlOutput, DefaultTopAnalysisWS);		// puts out the meaning element
				xmlOutput.WriteEndElement();		// end the field element
			}
			return result;
		}


		public bool Output(Hashtable languages, System.Xml.XmlTextWriter xmlOutput, ref Hashtable langsToIgnore,
			ref Hashtable fieldsToIgnore)
		{
			if (m_Language != null && langsToIgnore.ContainsKey(m_Language))
			{
				fieldsToIgnore.Add(m_Sfm, null);
				Converter.Log.AddWarning(String.Format(Sfm2XmlStrings.FieldDescWithSFMOf0IsBeingIGNORED, m_Sfm));
				return false;	// no output for this field
			}
			// just add it to the list of fields to ignore and allow everything else to process the same
			if (IsExcluded)
				fieldsToIgnore.Add(m_Sfm, null);

			if (xmlOutput != null)
			{
//				xmlOutput.WriteRaw(ToXmlLangString());
				ToXmlLangString(xmlOutput);
			}
			return true;
		}


		public bool ReadAndOutputXmlNode(System.Xml.XmlNode fieldNode, Hashtable languages, string topAnalysisWS, System.Xml.XmlTextWriter xmlOutput,
			ref Hashtable langsToIgnore, ref Hashtable fieldsToIgnore)
		{
			bool rval = false;
			if (ReadXmlNode(fieldNode, languages, topAnalysisWS))
			{
				rval = Output(languages, xmlOutput, ref langsToIgnore, ref fieldsToIgnore);
			}
			return rval;
		}


		public bool ReadXmlNode(System.Xml.XmlNode fieldNode, Hashtable languages, string topAnalysisWS)
		{
			m_OtherAttributes = "";
////			string allAttributes = "<field ";

			foreach(System.Xml.XmlAttribute Attribute in fieldNode.Attributes)
			{
				// Create new attribute details, which may be altered later on:
				string NewName = Attribute.Name;
				string NewValue = Attribute.Value;
				switch (Attribute.Name)
				{
					case "sfm":
						m_Sfm = Attribute.Value;
						break;
					case "name":
						m_Name = Attribute.Value;
						break;
					case "type":
						m_Type = Attribute.Value;
						break;
					case "lang":
////						NewName = "xml:lang";
////						NewValue = Attribute.Value;
						// Look up replacement language name:
						ClsLanguage language = languages[Attribute.Value] as ClsLanguage;
						if (language == null)
						{
							Converter.Log.AddError(String.Format(Sfm2XmlStrings.UnknownLangValue0InFieldDescs, Attribute.Value));
							m_xmlLanguage = NewValue;
						}
						else
							m_xmlLanguage = language.XmlLang;

						m_Language = NewValue;
						break;
					case "abbr":
						IsAbbr = STATICS.IsBoolString(Attribute.Value, true);
						break;
					case "exclude":
						IsExcluded = STATICS.IsBoolString(Attribute.Value, false);
						break;
					case "autoImport":
						IsAutoImportField = STATICS.IsBoolString(Attribute.Value, false);
						break;
					case "autoSfm":	// just ignore for now and re-assign
						break;
					default:
						m_OtherAttributes += " " + NewName + "=\"" + NewValue + "\"";
						break;
				}
////				allAttributes += " " + NewName + "=\"" + NewValue + "\"";
			}

////			allAttributes += ">";

			// Iterate through all the attributes of the "meaning" sub-element of this field:
			System.Xml.XmlNode Meaning = fieldNode.SelectSingleNode("meaning");
			if (Meaning == null)
			{
				Converter.Log.AddError(String.Format(Sfm2XmlStrings.MissingMeaningElementInField0InFieldDescs, m_Sfm));
			}
			else
			{
				m_Meaning = "<meaning ";

				foreach (System.Xml.XmlAttribute Attribute in Meaning.Attributes)
				{
					m_Meaning += Attribute.Name + "=\"" + Attribute.Value + "\" ";
					if (Attribute.Name == "app")
						m_meaningApp = Attribute.Value;
					else if (Attribute.Name == "id")
						m_meaningId = MakeValidFwId(Attribute.Value);
					else if (Attribute.Name == "func")
						m_RefFunc = Attribute.Value;
					else if (Attribute.Name == "funcWS")
						m_RefFuncWS = Attribute.Value;
				}
				m_Meaning += " />";

				RebuildMeaningEntry(null, topAnalysisWS);
			}

//			xmlOutput.WriteRaw(System.Environment.NewLine + "</field>" + System.Environment.NewLine);

			SetAutoSfm();
			if (m_Sfm == null)
				Converter.Log.AddError(Sfm2XmlStrings.FieldDefinedWithNoSfmAttributeInTheFieldDescs);
			else if (m_Language == null)
				Converter.Log.AddError(String.Format(Sfm2XmlStrings.FieldDescWithSFMOf0HasNoLangAttribute, m_Sfm));
			if (m_Type == null)
				Converter.Log.AddWarning(String.Format(Sfm2XmlStrings.FieldDescWithSFMOf0HasNoTypeAttribute, m_Sfm));
			else
				return true;

			return false;
		}

	}

	/// <summary>
	/// This class serves as a wrapper for the ClsFieldDescription class so that properties can be set
	/// at construction time.
	/// </summary>
	public class ClsFieldDescriptionWrapper : ClsFieldDescription
	{
		public ClsFieldDescriptionWrapper(string marker, string name, string datatype, string lang, bool abbr, string fwID)
			:
			base(marker, name, datatype, lang, abbr, fwID)
		{
		}
	}


	public class ClsCustomFieldDescription : ClsFieldDescription
	{
		public ClsCustomFieldDescription()
			: base()
		{
			m_class = string.Empty;
			m_uiClass = string.Empty;
			m_flid = 0;
			m_wsSelector = 0;
			m_big = false;
		}
		public ClsCustomFieldDescription(string fdClass, string uiClass, int flid, bool big, int wsSelector,	// custom specific values
			ClsFieldDescription baseFD)
			: base(baseFD.SFM, baseFD.Name, baseFD.Type, baseFD.Language, baseFD.IsAbbr, baseFD.MeaningID)
		{
			m_class = fdClass;
			m_uiClass = uiClass;
			m_flid = flid;
			m_big = big;
			m_wsSelector = wsSelector;
		}

		public ClsCustomFieldDescription(string fdClass, string uiClass, int flid, bool big, int wsSelector,	// custom specific values
			string marker, string name, string datatype, string lang, bool abbr, string fwID) :
			base(marker, name, datatype, lang, abbr, fwID)
		{
			m_class = fdClass;
			m_uiClass = uiClass;
			m_flid = flid;
			m_big = big;
			m_wsSelector = wsSelector;
		}
		private string m_class;			// LexEntry or LexSense
		private string m_uiClass;		// "Entry", "Subentry", "Variant", "Sense" - used in the UI
		private int m_flid;				// flid from the db
		private int m_wsSelector;		// wiritng system selector
		private bool m_big;				// is big

		public string ClassNameUI { get { return m_uiClass; } }	// m_class.Substring(3); } }
		new public bool ReadAndOutputXmlNode(System.Xml.XmlNode fieldNode, Hashtable languages, string topAnalysisWS, System.Xml.XmlTextWriter xmlOutput,
	ref Hashtable langsToIgnore, ref Hashtable fieldsToIgnore)
		{
			bool rval = false;
			if (ReadXmlNode(fieldNode, languages, topAnalysisWS))
			{
				rval = Output(languages, xmlOutput, ref langsToIgnore, ref fieldsToIgnore);
			}
			return rval;
		}
		public string CustomKey
		{
			get
			{
				string result = "_n:"+Name + "_c:" + m_class + "_t:" + m_Type;
				return result;	// .ToLower();
			}
		}


		new public bool ReadXmlNode(System.Xml.XmlNode customfieldNode, Hashtable languages, string topAnalysisWS)
		{
			// Iterate through all the attributes of the "field" sub-element of this custom field:
			System.Xml.XmlNode fieldNode = customfieldNode.SelectSingleNode("field");
			if (fieldNode == null)
			{
				Converter.Log.AddError(String.Format(Sfm2XmlStrings.NoFieldNodeInTheCustomField, m_Sfm));
				return false;
			}
			else
			{
				if (!base.ReadXmlNode(fieldNode, languages, "en"))
					return false;
			}

			foreach(System.Xml.XmlAttribute Attribute in customfieldNode.Attributes)
			{
				// Create new attribute details, which may be altered later on:
				string newValue = Attribute.Value;
				switch (Attribute.Name)
				{
//					case "guid":
//						m_guid = Attribute.Value;
//						break;
					case "wsSelector":
						m_wsSelector = Convert.ToInt32(Attribute.Value);
						break;
					case "big":
						m_big = STATICS.IsBoolString(Attribute.Value, false);
						break;
					case "flid":
						m_flid = Convert.ToInt32(Attribute.Value);
						break;
					case "class":
						m_class = newValue;
						break;
					case "uiclass":
						m_uiClass = newValue;
						break;
					default:
						throw new Exception("Invalid attribute on Custom Field Element.");

				}
			}

			return true;
		}
		new public string ToXmlString() { return ToXmlBaseString(false, null); }
		new private string ToXmlBaseString(bool useXMLLang, System.Xml.XmlTextWriter xmlOutput)
		{
			if (xmlOutput == null)
			{
				string baseElement = base.ToXmlString();
				string element = "<CustomField ";
//				element += "guid=\"" + m_guid + "\" ";
				element += "wsSelector=\"" + m_wsSelector + "\" ";
				element += "big=\"" + m_big + "\" ";
				element += "flid=\"" + m_flid + "\" ";
				element += "class=\"" + m_class + "\" ";
				element += "uiclass=\"" + m_uiClass + "\" ";
				element += ">" + System.Environment.NewLine;
				element += baseElement + System.Environment.NewLine;
				element += "</CustomField>";
				return element;
			}
			if (xmlOutput != null)
			{
				xmlOutput.WriteStartElement("CustomField");
//				xmlOutput.WriteAttributeString("guid", m_guid);
				xmlOutput.WriteAttributeString("wsSelector", m_wsSelector.ToString());
				xmlOutput.WriteAttributeString("big", m_big.ToString());
				xmlOutput.WriteAttributeString("flid", m_flid.ToString());
				xmlOutput.WriteAttributeString("class", m_class);
				xmlOutput.WriteAttributeString("uiclass", m_uiClass);
				base.ToXmlBaseString(useXMLLang, xmlOutput);
				xmlOutput.WriteEndElement();
			}
			return "";
		}

		new public string ToXmlLangString(System.Xml.XmlTextWriter xmlOutput)
		{
			if (m_autofieldInfo.Count == 0)
				return ToXmlBaseString(true, xmlOutput);

			// save the orig values
			string origMeaningId = MeaningID;
			string outputData = "";
			int count = m_autofieldInfo.Count;
			foreach (DictionaryEntry classsfm in m_autofieldInfo)
			{
				MeaningID = classsfm.Value as string;
				m_autoFieldClass = classsfm.Key as string;

				outputData += ToXmlBaseString(true, xmlOutput);
				if (count > 1)
					outputData += Environment.NewLine;
				count--;
			}
			m_autoFieldClass = "";
			MeaningID = origMeaningId;
			return outputData;
		}

	}
}
