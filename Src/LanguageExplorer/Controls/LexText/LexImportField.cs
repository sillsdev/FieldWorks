// Copyright (c) 2006-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections;
using System.Xml.Linq;
using LanguageExplorer.SfmToXml;

namespace LanguageExplorer.Controls.LexText
{
	/// <summary>
	/// This class is used to contain the field element in the FW import fields xml file
	/// </summary>
	internal class LexImportField : ILexImportField
	{
		private Hashtable m_mdfMarkers; // 0 - n markers

		public LexImportField()
		{
			ID = string.Empty;
			UIName = string.Empty;
			DataType = string.Empty;
			Property = string.Empty;
			Signature = string.Empty;
			IsList = false;
			IsMulti = false;
			IsRef = false;
			IsAutoField = false;
			m_mdfMarkers = new Hashtable();
			IsAbbrField = false;
			IsUnique = false;
		}

		public LexImportField(string name, string uiDest, string prop, string sig, bool list, bool multi, bool unique, string mdf)
		{
			ID = name;
			UIName = uiDest;
			Property = prop;
			DataType = "string";
			Signature = sig;
			IsList = list;
			IsMulti = multi;
			IsUnique = unique;
			m_mdfMarkers = new Hashtable();
			SfmToXmlServices.SplitString(mdf, ref m_mdfMarkers);
		}

		public ClsFieldDescription ClsFieldDescriptionWith(ClsFieldDescription fieldIn)
		{
			if (this is ILexImportCustomField && fieldIn != null)
			{
				var licf = this as ILexImportCustomField;
				// custom field case
				return new ClsCustomFieldDescription(licf.Class, licf.UIClass, licf.FLID, licf.Big,
					licf.WsSelector, fieldIn.SFM, licf.UIName, licf.Signature, fieldIn.Language, false, fieldIn.MeaningID);
			}
			// regular field case
			return null;
		}


		public ClsFieldDescription ClsFieldDescription
		{
			get
			{
				if (this is ILexImportCustomField)
				{
					var licf = this as ILexImportCustomField;
					// custom field case
					return new ClsCustomFieldDescription(licf.Class, licf.UIClass, licf.FLID, licf.Big, licf.WsSelector,
						"marker", licf.UIName, licf.DataType, "LANG IS STILL REQUIRED", false, "MEANING ID STILL REQUIRED");
				}
				// regular field case
				return null;
			}
		}

		public string ID { get; private set; }
		public string Property { get; private set; }
		public string UIName { get; private set; }
		public string Signature { get; private set; }
		public bool IsList { get; private set; }
		public bool IsMulti { get; private set; }
		public bool IsRef { get; private set; }
		public bool IsAutoField { get; private set; }
		public bool IsUnique { get; private set; }
		public XElement Element { get; private set; }
		public ICollection Markers => m_mdfMarkers.Keys;

		public string DataType { get; set; }

		public bool IsAbbrField { get; set; }

		// Given a string and default boolean value return the determined
		// value for the passed in string
		private static bool ReadBoolValue(string boolValue, bool defaultValue)
		{
			if (!defaultValue)
			{
				// look for all possible 'true' values
				return boolValue.ToLowerInvariant() == "yes" || boolValue.ToLowerInvariant() == "y" || boolValue.ToLowerInvariant() == "true"
				       || boolValue.ToLowerInvariant() == "t" || boolValue == "1";
			}
			// look for all possible 'false' values
			return boolValue.ToLowerInvariant() != "no" && boolValue.ToLowerInvariant() != "n" && boolValue.ToLowerInvariant() != "false"
			       && boolValue.ToLowerInvariant() != "f" && boolValue != "0";
		}

		/// <summary>
		/// This method will set exists to true if there is data in the
		/// passed in string and will return that data, otherwise it will return null
		/// and exists will be false.
		/// </summary>
		/// <param name="stringValue">data to return if exists</param>
		/// <param name="exists">set based on passed in string</param>
		/// <returns></returns>
		private static string ReadRequiredString(string stringValue, ref bool exists)
		{
			if (string.IsNullOrEmpty(stringValue))
			{
				exists = false;
				return null;
			}
			exists = true;
			return stringValue;
		}

		/// <summary>
		/// Read a 'Field' node from the controlled xml file that contains the ImportFields
		/// </summary>
		public bool ReadElement(XElement element)
		{
			var success = true;
			Element = element;
			foreach (var attribute in element.Attributes())
			{
				switch (attribute.Name.LocalName)
				{
					case "id":
						ID = ReadRequiredString(attribute.Value, ref success);
						break;
					case "uiname":
						UIName = ReadRequiredString(attribute.Value, ref success);
						break;
					case "property":
						Property = ReadRequiredString(attribute.Value, ref success);
						break;
					case "signature":
						Signature = ReadRequiredString(attribute.Value, ref success);
						break;
					case "list":
						IsList = ReadBoolValue(attribute.Value, false);
						break;
					case "multi":
						IsMulti = ReadBoolValue(attribute.Value, false);
						break;
					case "ref":
						IsRef = ReadBoolValue(attribute.Value, false);
						break;
					case "autofield":
						IsAutoField = ReadBoolValue(attribute.Value, false);
						break;
					case "type":
						DataType = ReadRequiredString(attribute.Value, ref success);
						break;
					case "unique":
						IsUnique = ReadBoolValue(attribute.Value, false);
						break;
					case "MDF":
						SfmToXmlServices.SplitString(attribute.Value, ref m_mdfMarkers);
						break;
				}
			}
			return success;
		}
	}
}