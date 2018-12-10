// Copyright (c) 2006-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using LanguageExplorer.SfmToXml;

namespace LanguageExplorer.Controls.LexText
{
	/// <summary>
	/// This container holds the import fields that were read in from the controlled
	/// xml file that is used for mapping the markers in the import data to the
	/// fields used in FieldWorks.  Properties show up in the Content Mappings page
	/// as well as the Modify mapping dlg.
	/// </summary>
	internal sealed class LexImportFields : ILexImportFields
	{
		/// <summary>
		/// key=class[string], value=fields[Dictionary[name, fields]]
		/// </summary>
		private Dictionary<string, Dictionary<string, ILexImportField>> m_classFields;
		/// <summary>
		/// key=[field.ID], value=[List of classes]
		/// </summary>
		private Dictionary<string, List<string>> m_allFields;
		/// <summary>
		/// key=class[string], value=partOf[string[]]
		/// </summary>
		private Dictionary<string, string> m_classPartOf;
		/// <summary>
		/// key=class[string], value=[field]
		/// </summary>
		private Dictionary<string, ILexImportField> m_autoFields;
		/// <summary>
		/// what signature values use the Abbr attribute
		/// </summary>
		private List<string> m_AbbrSignatures;
		private Dictionary<string, ILexImportCustomField> m_customFields;

		internal LexImportFields()
		{
			m_classFields = new Dictionary<string, Dictionary<string, ILexImportField>>();
			m_allFields = new Dictionary<string, List<string>>();
			m_classPartOf = new Dictionary<string, string>();
			m_autoFields = new Dictionary<string, ILexImportField>();
			m_AbbrSignatures = new List<string>();
			m_customFields = new Dictionary<string, ILexImportCustomField>();
		}

		internal ILexImportCustomField GetCustomField(LexImportCustomField cfIn)
		{
			return m_customFields.ContainsKey(cfIn.CustomKey) ? m_customFields[cfIn.CustomKey] : null;
		}

		internal ILexImportCustomField GetCustomField(ClsCustomFieldDescription cfIn)
		{
			return m_customFields.ContainsKey(cfIn.CustomKey) ? m_customFields[cfIn.CustomKey] : null;
		}

		public Dictionary<string, ILexImportField> GetAutoFields()
		{
			return m_autoFields;
		}

		public bool ContainsCustomField(string key)
		{
			return m_customFields.ContainsKey(key);
		}

		public ILexImportField GetAutoField(string className)
		{
			ILexImportField field;
			m_autoFields.TryGetValue(className, out field);
			return field;
		}

		/// <summary />
		/// <remarks>assumes that the fwDest is only in one class</remarks>
		public ILexImportField GetField(string fwDest, out string className)
		{
			List<string> classNames;
			if (!m_allFields.TryGetValue(fwDest, out classNames))
			{
				className = string.Empty;
				return null;
			}
			//Have to handle the case where there are more than one classes here - possible AddCustomField the class that
			//is stored in the ContentMarker as the DestinationClass and save it as part of the ILexImportField (allows dups)
			if (classNames.Count != 1)
			{
				className = string.Empty;
				return null;
			}
			className = classNames[0];
			return GetField(className, fwDest);
		}

		public ILexImportField GetField(string className, string fwDest)
		{
			Dictionary<string, ILexImportField> fields;
			if (!m_classFields.TryGetValue(className, out fields))
			{
				return null;
			}
			ILexImportField field;
			return !fields.TryGetValue(fwDest, out field) ? null : field;
		}

		/// <summary>
		/// add a field to the given class
		/// </summary>
		public bool AddField(string className, string partOf, ILexImportField field)
		{
			Dictionary<string, ILexImportField> fields;
			if (!m_classFields.TryGetValue(className, out fields))
			{
				fields = new Dictionary<string, ILexImportField>();
				m_classFields.Add(className, fields);
				m_classPartOf.Add(className, partOf);
			}
			if (fields.ContainsKey(field.ID))
			{
				return false;   // already present
			}
			fields.Add(field.ID, field);
			if (field.IsAutoField)
			{
				m_autoFields.Add(className, field);
			}
			return true;
		}

		public string GetCustomFieldClassFromClassID(int classID)
		{
			const int kclidLexEntry = 5002;
			const int kclidLexSense = 5016;
			const int kclidMoForm = 5035;
			const int kclidLexExampleSentence = 5004;

			// Currently custom fields can belong to only a limited number of known types, so
			// throw if it's of a different value - this code will have to change as more types
			// are supported.
			switch (classID)
			{
				case kclidLexEntry:
					return "Entry";
				case kclidLexSense:
					return "Sense";
				case kclidLexExampleSentence:
					return "Example";
				case kclidMoForm:
					return "Allomorph";
				default:
					throw new Exception("Custom field exists that belongs to an unknown class: modification needed here...");
			}
		}

		/// <summary>
		/// add a field to the given class
		/// </summary>
		public bool AddCustomField(int classID, ILexImportCustomField field)
		{
			var baseClassName = GetCustomFieldClassFromClassID(classID);
			// possibly have to add the 'Entry' field to the 'Subentry' and 'Variant' classes also
			var classNames = new List<string>
			{
				baseClassName
			};
			if (baseClassName == "Entry")
			{
				classNames.Add("Subentry");
				classNames.Add("Variant");
			}
			foreach (var className in classNames)
			{
				Dictionary<string, ILexImportField> fields;
				if (!m_classFields.TryGetValue(className, out fields))
				{
					fields = new Dictionary<string, ILexImportField>();
					m_classFields.Add(className, fields);
				}
				if (fields.ContainsKey(field.ID))
				{
					return false;
				}
				fields.Add(field.ID, field);
				if (!ContainsCustomField(field.CustomKey))
				{
					m_customFields.Add(field.CustomKey, field);
				}
				List<string> classnames;
				if (!m_allFields.TryGetValue(field.ID, out classnames))
				{
					m_allFields.Add(field.ID, new List<string>(new string[] { className }));
				}
				else
				{
					classnames.Add(className);
				}
			}
			return true;
		}

		/// <summary>
		/// Get a list of class values defined
		/// </summary>
		public ICollection Classes => m_classFields.Keys;

		public string HierarchyForClass(string className)
		{
			string partOf;
			return m_classPartOf.TryGetValue(className, out partOf) ? partOf : "***UnknownClassName***";
		}

		/// <summary>
		/// get the field names for a given class of fields
		/// </summary>
		public ICollection FieldsForClass(string className)
		{
			Dictionary<string, ILexImportField> fields;
			return m_classFields.TryGetValue(className, out fields) ? fields.Values : null;
		}

		public string GetUIDestForName(string fieldName)
		{
			foreach (var classObj in m_classFields)
			{
				if (classObj.Value.ContainsKey(fieldName))
				{
					return classObj.Value[fieldName].UIName;
				}
			}
			return null;
		}

		public bool GetDestinationForName(string name, out string className, out string fieldName)
		{
			foreach (var classObj in m_classFields)
			{
				if (classObj.Value.ContainsKey(name))
				{

					className = classObj.Key;
					fieldName = classObj.Value[name].UIName;
					return true;
				}
			}
			className = fieldName = string.Empty;
			return false;
		}

		/// <summary>
		/// read the passed in list of additional fields (should be custom ones)
		/// and add them to the list of fields.
		/// </summary>
		public bool AddCustomImportFields(ILexImportFields customFields)
		{
			return true;
		}

		/// <summary>
		/// read the passed in path and file and pull out the classes and fields
		/// </summary>
		public bool ReadLexImportFields(string xmlFileName)
		{
			bool success;
			try
			{
				var xmlMap = XDocument.Load(xmlFileName);
				var abbrSignatures = xmlMap.XPathSelectElement("ImportFields/AbbreviationSignatures");
				ReadSignatureNode(abbrSignatures);

				var classList = xmlMap.XPathSelectElements("ImportFields/Class");
				foreach (var classNode in classList)
				{
					ReadAClassNode(classNode);
				}
				success = Initialize();
			}
			catch (XmlException)
			{
				success = false;
			}
			return success;
		}


		/// <summary>
		/// Any additional initialization processing could be done here.  This is after
		/// the file has been read and before it's used.
		/// </summary>
		private static bool Initialize()
		{
			return true;
		}

		private void ReadSignatureNode(XElement element)
		{
			var nameAttr = element.Attribute("names");
			if (nameAttr == null)
			{
				return;
			}
			m_AbbrSignatures = new List<string>(SfmToXmlServices.SplitString(nameAttr.Value));
		}


		/// <summary>
		/// helper method to read a class element and store the fields
		/// </summary>
		private void ReadAClassNode(XElement element)
		{
			var nameAttr = element.Attribute("name");
			if (nameAttr == null)
			{
				return;
			}
			var className = nameAttr.Value;
			var partOfAttr = element.Attribute("partOf");
			if (partOfAttr == null)
			{
				return;
			}
			var partOf = partOfAttr.Value;
			foreach (var idElement in element.Elements("Field"))
			{
				ILexImportField field = new LexImportField();
				if (field.ReadElement(idElement))
				{
					// is a abbrv field
					field.IsAbbrField = m_AbbrSignatures.Contains(field.Signature);
					AddField(className, partOf, field);

					List<string> classnames;
					if (!m_allFields.TryGetValue(field.ID, out classnames))
					{
						m_allFields.Add(field.ID, new List<string>(new[] { className }));
					}
					else
					{
						// Review DanH (RandyR): Why add more than one, since only one is ever used?
						// Maybe it should not be a List of strings, but only one string.
						classnames.Add(className);
					}
				}
				else
				{
					// error case where the xml field wasn't able to be read
					return;
				}
			}
		}
	}
}