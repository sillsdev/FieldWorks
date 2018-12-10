// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Xml;

namespace LanguageExplorer.SfmToXml
{
	public class ClsCustomFieldDescription : ClsFieldDescription
	{
		/// <summary>
		/// LexEntry or LexSense
		/// </summary>
		private string m_class;
		/// <summary>
		/// flid from the db
		/// </summary>
		private int m_flid;
		/// <summary>
		/// writing system selector
		/// </summary>
		private int m_wsSelector;
		/// <summary>
		/// is big
		/// </summary>
		private bool m_big;

		public ClsCustomFieldDescription()
		{
			m_class = string.Empty;
			ClassNameUI = string.Empty;
			m_flid = 0;
			m_wsSelector = 0;
			m_big = false;
		}
		public ClsCustomFieldDescription(string fdClass, string uiClass, int flid, bool big, int wsSelector, ClsFieldDescription baseFD)
			: base(baseFD.SFM, baseFD.Name, baseFD.Type, baseFD.Language, baseFD.IsAbbr, baseFD.MeaningID)
		{
			m_class = fdClass;
			ClassNameUI = uiClass;
			m_flid = flid;
			m_big = big;
			m_wsSelector = wsSelector;
		}

		public ClsCustomFieldDescription(string fdClass, string uiClass, int flid, bool big, int wsSelector, string marker, string name, string datatype, string lang, bool abbr, string fwID)
			: base(marker, name, datatype, lang, abbr, fwID)
		{
			m_class = fdClass;
			ClassNameUI = uiClass;
			m_flid = flid;
			m_big = big;
			m_wsSelector = wsSelector;
		}

		public string ClassNameUI { get; private set; }

		public string CustomKey => $"_n:{Name}_c:{m_class}_t:{Type}";

		public override bool ReadXmlNode(XmlNode customfieldNode, Hashtable languages, string topAnalysisWS)
		{
			// Iterate through all the attributes of the "field" sub-element of this custom field:
			var fieldNode = customfieldNode.SelectSingleNode("field");
			if (fieldNode == null)
			{
				Converter.Log.AddError(string.Format(SfmToXmlStrings.NoFieldNodeInTheCustomField, SFM));
				return false;
			}
			if (!base.ReadXmlNode(fieldNode, languages, "en"))
			{
				return false;
			}
			foreach (XmlAttribute attribute in customfieldNode.Attributes)
			{
				// Create new attribute details, which may be altered later on:
				var newValue = attribute.Value;
				switch (attribute.Name)
				{
					case "wsSelector":
						m_wsSelector = Convert.ToInt32(attribute.Value);
						break;
					case "big":
						m_big = SfmToXmlServices.IsBoolString(attribute.Value, false);
						break;
					case "flid":
						m_flid = Convert.ToInt32(attribute.Value);
						break;
					case "class":
						m_class = newValue;
						break;
					case "uiclass":
						ClassNameUI = newValue;
						break;
					default:
						throw new Exception("Invalid attribute on Custom Field Element.");

				}
			}

			return true;
		}

		protected override string ToXmlBaseString(bool useXMLLang, XmlTextWriter xmlOutput)
		{
			if (xmlOutput == null)
			{
				var element = $"<CustomField wsSelector=\"{m_wsSelector}\" big=\"{m_big}\" flid=\"{m_flid}\" class=\"{m_class}\" uiclass=\"{ClassNameUI}\" ";
				element += $">{Environment.NewLine}{base.ToXmlString()}{Environment.NewLine}</CustomField>";
				return element;
			}
			xmlOutput.WriteStartElement("CustomField");
			xmlOutput.WriteAttributeString("wsSelector", m_wsSelector.ToString());
			xmlOutput.WriteAttributeString("big", m_big.ToString());
			xmlOutput.WriteAttributeString("flid", m_flid.ToString());
			xmlOutput.WriteAttributeString("class", m_class);
			xmlOutput.WriteAttributeString("uiclass", ClassNameUI);

			base.ToXmlBaseString(useXMLLang, xmlOutput);

			xmlOutput.WriteEndElement();
			return string.Empty;
		}
	}
}