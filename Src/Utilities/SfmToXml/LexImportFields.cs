using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Xml;	// XmlNode

namespace Sfm2Xml
{
	/// <summary>
	/// This interface exists to serve as a contract between the import process and the object.
	/// </summary>
	public interface ILexImportFields
	{
		Dictionary<string, ILexImportField> GetAutoFields();
		ILexImportField GetAutoField(string className);
		ILexImportField GetField(string className, string fwDest);
		ILexImportField GetField(string fwDest, out string className);	// assumes that the fwDest is only in one class

		bool AddField(string className, string partOf, ILexImportField field);
		bool AddCustomField(int classID, /*string className,*/ ILexImportCustomField field);
		bool ContainsCustomField(string key);
		ICollection Classes { get;}
		string HierarchForClass(string className);
		ICollection FieldsForClass(string className);
		string GetUIDestForName(string fieldName);
		bool GetDestinationForName(string name, out string className, out string fieldName);
		bool ReadLexImportFields(string xmlFileName);
		bool AddCustomImportFields(Sfm2Xml.ILexImportFields customFields);
		string GetCustomFieldClassFromClassID(int classID);
		//bool SameCustomField(string customKey, System.Guid customFieldID);
	}

	/// <summary>
	/// This container holds the import fields that were read in from the controled
	/// xml file that is used for mapping the markers in the import data to the
	/// fields used in FieldWorks.  Properties show up in the Content Mappings page
	/// as well as the Modify mapping dlg.
	/// </summary>
	public class LexImportFields : ILexImportFields
	{
		private Dictionary<string, Dictionary<string, ILexImportField>> m_classFields; // key=class[string], value=fields[Dictionary[name, fields]]
		private Dictionary<string, List<string>> m_allFields;// key=[field.ID], value=[List of classes]
		// private Set<string> m_dupFields; // Not used anywhere, other than adding stuff to it.
		// private Set<string> m_uniqueFields; // Not used anywhere, other than adding stuff to it.
		private Dictionary<string, string> m_classPartOf;	// key=class[string], value=partOf[string[]]
		private Dictionary<string, ILexImportField> m_autoFields; // key=class[string], value=[field]
		private List<string> m_AbbrSignatures;// what signature values use the Abbr attribute
		private Dictionary<string, ILexImportCustomField> m_customFields;
		//private Dictionary<string, string> m_customFieldsByGUID;
		//private Dictionary<uint, string> m_customFieldsByCRC;	// key = crc, value = m_customFields.key

//		private string GetPropertyForName(string fieldName)
//		{
//			string propertyname = null;
//			foreach (Dictionary<string, ILexImportField> fields in m_classFields.Values)
//			{
//				ILexImportField field = null;
//				if (fields.TryGetValue(fieldName, out field))
//				{
//					propertyname = field.Property;
//					break;
//				}
//			}
//			return propertyname;
//		}

		public LexImportFields()
		{
			m_classFields = new Dictionary<string, Dictionary<string, ILexImportField>>();
			m_allFields = new Dictionary<string, List<string>>();
			// m_dupFields = new Set<string>(); // Not used anywhere, other than adding stuff to it.
			// m_uniqueFields = new Set<string>(); // Not used anywhere, other than adding stuff to it.
			m_classPartOf = new Dictionary<string, string>();
			m_autoFields = new Dictionary<string, ILexImportField>();
			m_AbbrSignatures = new List<string>();
			m_customFields = new Dictionary<string, ILexImportCustomField>();
//			m_customFieldsByGUID = new Dictionary<string, string>();
//			m_customFieldsByCRC = new Dictionary<uint, string>();
		}

		public Dictionary<string, ILexImportField> GetAutoFields()
		{
			return m_autoFields;
		}

		public ILexImportCustomField GetCustomField(LexImportCustomField cfIn)
		{
			if (m_customFields.ContainsKey(cfIn.CustomKey))
			{
//				if (m_customFieldsByGUID.ContainsKey(cfIn.CustomFieldID.ToString()))
					return m_customFields[cfIn.CustomKey];
//				throw new Exception("not sure here .... ???? ");
			}
			// if we find a match on the key, return that one,
			//  if we find a match on the guid, return that one,
			//   return null
			return null;
		}

		public ILexImportCustomField GetCustomField(ClsCustomFieldDescription cfIn)
		{
			if (m_customFields.ContainsKey(cfIn.CustomKey))
			{
				//if (m_customFieldsByGUID.ContainsKey(cfIn.CustomGuid))
					return m_customFields[cfIn.CustomKey];
				//throw new Exception("not sure here .... ???? ");
			}
			// if we find a match on the key, return that one,
			//  if we find a match on the guid, return that one,
			//   return null
	//		return m_customFields["_n:11 22 xyz yyy new custom field to remove_c:lexentry_t:string"];
			return null;
		}

		//public bool ContainsCustomFieldGuid(string key)
		//{
		//    return m_customFieldsByGUID.ContainsKey(key);
		//}

		public bool ContainsCustomField(string key)
		{
			return m_customFields.ContainsKey(key);
		}

		public ILexImportField GetAutoField(string className)
		{
			ILexImportField field = null;
			m_autoFields.TryGetValue(className, out field);
			return field;
		}

		public ILexImportField GetField(string fwDest, out string className)	// assumes that the fwDest is only in one class
		{
			className = "";
			List<string> classNames = null;
			if (!m_allFields.TryGetValue(fwDest, out classNames))
				return null;

			//Have to handle the case where there are more than one classes here - possible AddCustomField the class that
			//is stored in the ContentMarker as the DestinationClass and save it as part of the ILexImportField (allows dups)

			if (classNames.Count != 1)
				return null;

			className = classNames[0];
			return GetField(className, fwDest);
		}


		public ILexImportField GetField(string className, string fwDest)
		{
			Dictionary<string, ILexImportField> fields = null;
			if (!m_classFields.TryGetValue(className, out fields))
				return null;

			ILexImportField field = null;
			if (!fields.TryGetValue(fwDest, out field))
				return null;

			return field;
		}

		/// <summary>
		/// add a field to the given class
		/// </summary>
		/// <param name="className"></param>
		/// <param name="fieldName"></param>
		/// <returns></returns>
		public bool AddField(string className, string partOf, ILexImportField field)
		{
			Dictionary<string, ILexImportField> fields = null;
			if (!m_classFields.TryGetValue(className, out fields))
			{
				fields = new Dictionary<string, ILexImportField>();
				m_classFields.Add(className, fields);
				m_classPartOf.Add(className, partOf);
			}

			if (fields.ContainsKey(field.ID))
				return false;	// already present

			fields.Add(field.ID, field);

			if (field.IsAutoField)
				m_autoFields.Add(className, field);

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
		/// <param name="className"></param>
		/// <param name="fieldName"></param>
		/// <returns></returns>
		public bool AddCustomField(int classID, ILexImportCustomField field)
		{
			string baseClassName = GetCustomFieldClassFromClassID(classID);

			// possibly have to add the 'Entry' field to the 'Subentry' and 'Variant' classes also
			List<string> classNames = new List<string>();
			classNames.Add(baseClassName);
			if (baseClassName == "Entry")
			{
				classNames.Add("Subentry");
				classNames.Add("Variant");
			}
			foreach (string className in classNames)
			{
				Dictionary<string, ILexImportField> fields = null;
				if (!m_classFields.TryGetValue(className, out fields))
				{
					fields = new Dictionary<string, ILexImportField>();
					m_classFields.Add(className, fields);
				}

				if (fields.ContainsKey(field.ID))
					return false;	// already present

				fields.Add(field.ID, field);
				if (!ContainsCustomField(field.CustomKey))
				{
					m_customFields.Add(field.CustomKey, field);
					//m_customFieldsByGUID.Add(field.CustomFieldID.ToString(), field.CustomKey);
					//m_customFieldsByCRC.Add(field.CRC, field.CustomKey);
				}
				List<string> classnames = null;
				if (!m_allFields.TryGetValue(field.ID, out classnames))
					m_allFields.Add(field.ID, new List<string>(new string[] { className }));
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
		public ICollection Classes
		{
			get { return m_classFields.Keys; }
		}

		public string HierarchForClass(string className)
		{
			string partOf = null;
			if (m_classPartOf.TryGetValue(className, out partOf))
				return partOf;
			return "***UnknownClassName***"; // testing;
		}

		/// <summary>
		/// get the field names for a given class of fields
		/// </summary>
		/// <param name="className"></param>
		/// <returns></returns>
		public ICollection FieldsForClass(string className)
		{
			Dictionary<string, ILexImportField> fields = null;
			if (m_classFields.TryGetValue(className, out fields))
				return fields.Values;

			return null;
		}

		public string GetUIDestForName(string fieldName)
		{
			foreach (KeyValuePair<string, Dictionary<string, ILexImportField>> classObj in m_classFields)
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
			foreach (KeyValuePair<string, Dictionary<string, ILexImportField>> classObj in m_classFields)
			{
				if (classObj.Value.ContainsKey(name))
				{

					className = classObj.Key as string;
					fieldName = classObj.Value[name].UIName;	// .Property;
					return true;
				}
			}
			className = fieldName = "";
			return false;
		}

		/// <summary>
		/// read the passed in list of additional fields (should be custom ones)
		/// and add them to the list of fields.
		/// </summary>
		/// <param name="customFields"></param>
		/// <returns></returns>
		public bool AddCustomImportFields(Sfm2Xml.ILexImportFields customFields)
		{
			//foreach (LexImportField field in customFields)
			//{

			//}
			return true;
		}

		//public bool SameCustomField(string customKey, System.Guid customFieldID)
		//{
		//    if (m_customFieldsByGUID.ContainsKey(customFieldID.ToString()))
		//    {
		//        if (m_customFieldsByGUID[customFieldID.ToString()] == customKey)
		//            return true;
		//    }
		//    return false;
		//}

		/// <summary>
		/// read the passed in path and file and pull out the classes and fields
		/// </summary>
		/// <param name="xmlFileName"></param>
		/// <returns>true if successfull</returns>
		public bool ReadLexImportFields(string xmlFileName)
		{
			bool success = true;
			System.Xml.XmlDocument xmlMap = new System.Xml.XmlDocument();
			try
			{
				xmlMap.Load(xmlFileName);
				System.Xml.XmlNode abbrSignatures = xmlMap.SelectSingleNode("ImportFields/AbbreviationSignatures");
				ReadSignatureNode(abbrSignatures);

				System.Xml.XmlNodeList classList = xmlMap.SelectNodes("ImportFields/Class");
				foreach (System.Xml.XmlNode classNode in classList)
				{
					if (!ReadAClassNode(classNode))
						success = false;
				}
				success = Initialize();

				// 6/2/08  Now add in any custom fields that are currently defined in the database

			}
			catch (System.Xml.XmlException)
			{
//				string ErrMsg = "Error: invalid mapping file '" + xmlFileName + "' : " + e.Message;
				success = false;
			}
			return success;
		}


		/// <summary>
		/// Any additional initialization processing could be done here.  This is after
		/// the file has been read and before it's used.
		/// </summary>
		/// <returns>success</returns>
		private bool Initialize()
		{
			return true;
		}

		private bool ReadSignatureNode(System.Xml.XmlNode node)
		{
			System.Xml.XmlAttribute nameAttr = node.Attributes["names"];
			if (nameAttr == null)
				return false;
			string sigNames = nameAttr.Value;
			string [] sigs = STATICS.SplitString(sigNames);
			m_AbbrSignatures = new List<string>(sigs);
			return true;
		}


		/// <summary>
		/// helper method to read a class node and store the fields
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		private bool ReadAClassNode(System.Xml.XmlNode node)
		{
			bool success = true;
			System.Xml.XmlAttribute nameAttr = node.Attributes["name"];
			if (nameAttr == null)
				return false;
			string className = nameAttr.Value;
			System.Xml.XmlAttribute partOfAttr = node.Attributes["partOf"];
			if (partOfAttr == null)
				return false;
			string partOf = partOfAttr.Value;
			System.Xml.XmlNodeList idList = node.SelectNodes("Field");
			foreach (System.Xml.XmlNode idNode in idList)
			{
				ILexImportField field = new LexImportField();
				if (field.ReadNode(idNode))
				{
					// is a abbrv field
					field.IsAbbrField = m_AbbrSignatures.Contains(field.Signature);
					AddField(className, partOf, field);

					List<string> classnames = null;
					if (!m_allFields.TryGetValue(field.ID, out classnames))
						m_allFields.Add(field.ID, new List<string>(new string[] { className }));
					else
					{
						// Review DanH (RandyR): Why add more than one, since only one is ever used?
						// Maybe it should not be a List of strings, but only one string.
						classnames.Add(className);
						// Not used anywhere, other than adding stuff to it.
						// m_dupFields.Add(field.ID); // Set's won't add them more than once.
					}
					// Not used anywhere, other than adding stuff to it.
					// is a unique field
//					if (field.IsUnique)
//						m_uniqueFields.Add(field.ID);
				}
				else
				{
					// error case where the xml field wasn't able to be read
					success = false;	// error
				}
			}
			return success;
		}
	}
}
