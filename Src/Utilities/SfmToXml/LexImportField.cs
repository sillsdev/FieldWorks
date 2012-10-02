using System;
using System.Text;
using System.Collections;
using System.Xml;	// XmlNode

namespace Sfm2Xml
{
	/// <summary>
	/// This interface defines a way of interacting with the import fields that
	/// were read in from the controled xml file that is used for mapping the
	/// markers in the import data to the fields used in FieldWorks.
	/// </summary>
	public interface ILexImportField
	{
		string ID { get;}		// this is the FW field id
		string Property { get;}	// this is the user readable name
		string UIName { get;}	// this is the UI Name
		string Signature { get;}	// FW data type 'like' property
		bool IsList { get;}		// true if it's a list
		bool IsMulti { get;}		// true if it's a multi
		bool IsRef { get;}		// true if it's a ref: 'lxrel' and 'cref'
		bool IsAutoField { get;}	// true if it's an auto
		bool IsUnique { get;}	// only can have on unique per object(overrides begin marker logic)
		XmlNode Node { get;}		// original XML

		ICollection Markers { get;}		// mdf markers normally associated with this field
		string DataType { get; set;}	// type of data [integer, date, string, ...]
		bool IsAbbrField { get; set;}	// true if this field allows the abbr field to be editied

		/// Read a 'Field' node from the controled xml file that contains the ImportFields
		bool ReadNode(System.Xml.XmlNode node);
		////bool IsCustomField { get; set;}
		////Guid CustomFieldID { get; set;}
		ClsFieldDescription ClsFieldDescription { get;}	// return an equivelant Cls object
		ClsFieldDescription ClsFieldDescriptionWith(ClsFieldDescription fieldIn);

	}

	public interface ILexImportCustomField : ILexImportField
	{
		// Guid CustomFieldID { get; /*set;*/}
		int WsSelector { get; }
		bool Big { get;  }
		int FLID { get; }
		string Class { get; }
		string UIClass { get; set; }
		string CustomKey { get; }
		uint CRC { get; }
	}

	public class LexImportCustomField : LexImportField, ILexImportCustomField
	{
		protected LexImportCustomField() : base()
		{
		}
		// This is a TERRIBLE constructor with so many parameters ... but, so be it for now, other wise
		//  it would require to many set accessors
		public LexImportCustomField(int fdClass, string uiClass, /*Guid customID,*/ int flid, bool big, int wsSelector,	// custom specific values
			string name, string uiDest, string prop, string sig, bool list, bool multi, bool unique, string mdf)
			: base(name, uiDest, prop, sig, list, multi, unique, mdf)
		{
			const int kclidLexEntry = 5002;
			const int kclidLexSense = 5016;

			m_class = (fdClass == kclidLexEntry?"LexEntry":(fdClass== kclidLexSense?"LexSense":"UnknownClass"));
			m_uiClass = uiClass;
			//m_customFieldID = customID;
			m_flid = flid;
			m_big = big;
			m_wsSelector = wsSelector;
		}
		//private Guid m_customFieldID = Guid.Empty;
		private int m_wsSelector = 0;
		private bool m_big = false;
		private int m_flid;
		private uint m_crc=0;
		private string m_class = "";	// lexEntry or LexSense
		private string m_uiClass = "";	// "Entry", "Subentry", "Variant", "Sense" - used in the UI
		//public Guid CustomFieldID
		//{
		//    get { return m_customFieldID; }
		//    //set { m_customFieldID = value; }
		//}
		public string CustomKey
		{
			get
			{
				string result = "_n:" + UIName + "_c:" + Class + "_t:" + Signature;
				return result;	//.ToLowerInvariant();
			}
		}
		public int WsSelector { get { return m_wsSelector; } }
		public bool Big { get { return m_big; } }
		public int FLID { get { return m_flid; } }
		public string Class { get { return m_class; } }
		public string UIClass { get { return m_uiClass; } set { m_uiClass = value; } }
		public uint CRC	// intent is to use this value to compare to others to see if they are the same or different
		{
			get
			{
				if (m_crc == 0)
				{
					StringBuilder data = new StringBuilder();
					data.Append(CustomKey);
//					data.Append('0');
//					data.Append(m_customFieldID.ToString());
					data.Append('1');
					data.Append(m_wsSelector);
					data.Append('2');
					data.Append(m_big);
					data.Append('3');
					data.Append(m_flid);
					data.Append('4');
					data.Append(m_class);
					data.Append('5');
					data.Append(m_uiClass);

					ASCIIEncoding AE = new ASCIIEncoding();
					byte[] byteData = AE.GetBytes(data.ToString());

					CRC crc = new CRC();
					m_crc = crc.CalculateCRC(byteData, byteData.Length);
				}
				return m_crc;
			}
		}
	}

	/// <summary>
	/// This class is used to contain the field element in the FW import fields xml file
	/// </summary>
	public class LexImportField : ILexImportField
	{
		private string m_name;	// this is the FW field id
		private string m_uiName;	// UI Name
		private string m_dataType;	// type of data [integer, date, string, ...]
		private string m_property;	// user readable name
		private string m_signature;	// FW data type 'like' property
		private bool m_isList;	// defaults to false
		private bool m_isMulti;	// defaults to false
		private bool m_isRef;	// defaults to false, ref fields like 'lxrel' and 'cref'
		private bool m_isAutoField;	// defaults to false
		private bool m_isUnique;	// defaults to false; only can have one unique per object(overrides begin marker logic)
		private XmlNode m_node;  // the original XML itself (used by help)

		private Hashtable m_mdfMarkers;	// 0 - n markers
		private bool m_isAbbrField;	// true if this field allows the abbr field to be editied

		public LexImportField()
		{
			m_name = "";		// required field
			m_uiName = "";		// required field
			m_dataType = "";	// required field
			m_property = "";	// required field
			m_signature = "";	// required field
			m_isList = false;	// default to false
			m_isMulti = false;	// default to false
			m_isRef = false;	// default to false
			m_isAutoField = false;
			m_mdfMarkers = new Hashtable();
			m_isAbbrField = false;
			m_isUnique = false;
		}

		public LexImportField(string name, string uiDest, string prop, string sig, bool list, bool multi, bool unique, string mdf)
		{
			m_name = name;
			m_uiName = uiDest;
			m_property = prop;
			m_dataType = "string";	// default if not given
			m_signature = sig;
			m_isList = list;
			m_isMulti = multi;
			m_isUnique = unique;
			m_mdfMarkers = new Hashtable();
			STATICS.SplitString(mdf, ref m_mdfMarkers);
		}

		public ClsFieldDescription ClsFieldDescriptionWith(ClsFieldDescription fieldIn)
		{
			if (this is ILexImportCustomField && fieldIn != null)
			{
				LexImportCustomField licf = this as LexImportCustomField;
				// custom field case
				ClsCustomFieldDescription rvalc = new ClsCustomFieldDescription(
					licf.Class,
					licf.UIClass,
					//licf.CustomFieldID,
					licf.FLID,
					licf.Big,
					licf.WsSelector,

					fieldIn.SFM,
					licf.UIName,
					licf.Signature,	// NOT SURE !!!! ????  DataType,
					fieldIn.Language,	//"LANG IS STILL REQUIRED",//licf.Language,
					false,
					fieldIn.MeaningID	// "MEANING ID STILL REQUIRED"	// licf.MeaningID
					);
				return rvalc;
			}
			else
			{
				// regular field case
				return null;
			}
		}


		public ClsFieldDescription ClsFieldDescription
		{
			get
			{
				if (this is ILexImportCustomField)
				{
					LexImportCustomField licf = this as LexImportCustomField;
					// custom field case
					ClsCustomFieldDescription rvalc = new ClsCustomFieldDescription(
						licf.Class,
						licf.UIClass,
						//licf.CustomFieldID,
						licf.FLID,
						licf.Big,
						licf.WsSelector,

						"marker",
						licf.UIName,
						licf.DataType,
						"LANG IS STILL REQUIRED",//licf.Language,
						false,
						"MEANING ID STILL REQUIRED"	// licf.MeaningID
						);
					return rvalc;
				}
				else
				{
					// regular field case
					return null;
				}
			}
		}


		//public attributes / properties of this class
		public string ID { get { return m_name; } }
		public string Property { get { return m_property; } }
		public string UIName { get { return m_uiName; } }
		public string Signature { get { return m_signature; } }
		public bool IsList { get { return m_isList; } }
		public bool IsMulti { get { return m_isMulti; } }
		public bool IsRef { get { return m_isRef; } }
		public bool IsAutoField { get { return m_isAutoField; } }
		public bool IsUnique { get { return m_isUnique; } }
		public XmlNode Node { get { return m_node; } }

		public ICollection Markers { get { return m_mdfMarkers.Keys; } }

		public string DataType
		{
			get { return m_dataType; }
			set { m_dataType = value; }
		}

		/*
		private bool m_isCustomField = false; // are now working with custom fields at times so defaut to false but provide for it if true
		private Guid m_customFieldID = Guid.Empty;
		public bool IsCustomField
		{
			get { return m_isCustomField; }
			set { m_isCustomField = value; }
		}
		public Guid CustomFieldID
		{
			get { return m_customFieldID; }
			set { m_customFieldID = value; }
		}
		*/
		public bool IsAbbrField
		{	// default is false
			get { return m_isAbbrField; }
			set { m_isAbbrField = value; }
		}

		// Given a string and default boolean value return the determined
		// value for the passed in string
		private bool ReadBoolValue(string boolValue, bool defaultValue)
		{
			if (defaultValue == false)
			{
				// look for all possible 'true' values
				if (boolValue.ToLowerInvariant() == "yes" || boolValue.ToLowerInvariant() == "y" ||
					boolValue.ToLowerInvariant() == "true" || boolValue.ToLowerInvariant() == "t" ||
					boolValue == "1")
					return true;
			}
			else
			{
				// look for all possible 'false' values
				if (boolValue.ToLowerInvariant() == "no" || boolValue.ToLowerInvariant() == "n" ||
					boolValue.ToLowerInvariant() == "false" || boolValue.ToLowerInvariant() == "f" ||
					boolValue == "0")
					return false;
			}
			return defaultValue;
		}

		/// <summary>
		/// This method will set exists to true if there is data in the
		/// passed in string and will return that data, otherwise it will return null
		/// and exists will be false.
		/// </summary>
		/// <param name="stringValue">data to return if exists</param>
		/// <param name="exists">set based on passed in string</param>
		/// <returns></returns>
		private string ReadRequiredString(string stringValue, ref bool exists)
		{
			if (stringValue == null || stringValue.Length == 0)
			{
				exists = false;
				return null;
			}
			exists = true;
			return stringValue;
		}

		/// <summary>
		/// Read a 'Field' node from the controled xml file that contains the ImportFields
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public bool ReadNode(System.Xml.XmlNode node)
		{
			bool success = true;
			m_node = node;
			foreach (System.Xml.XmlAttribute Attribute in node.Attributes)
			{
				switch (Attribute.Name)
				{
					case "id":
						m_name = ReadRequiredString(Attribute.Value, ref success);
						break;
					case "uiname":
						m_uiName = ReadRequiredString(Attribute.Value, ref success);
						break;
					case "property":
						m_property = ReadRequiredString(Attribute.Value, ref success);
						break;
					case "signature":
						m_signature = ReadRequiredString(Attribute.Value, ref success);
						break;
					case "list":
						m_isList = ReadBoolValue(Attribute.Value, false);
						break;
					case "multi":
						m_isMulti = ReadBoolValue(Attribute.Value, false);
						break;
					case "ref":
						m_isRef = ReadBoolValue(Attribute.Value, false);
						break;
					case "autofield":
						m_isAutoField = ReadBoolValue(Attribute.Value, false);
						break;
					case "type":
						m_dataType = ReadRequiredString(Attribute.Value, ref success);
						break;
					case "unique":
						m_isUnique = ReadBoolValue(Attribute.Value, false);
						break;
					case "MDF":
						STATICS.SplitString(Attribute.Value, ref m_mdfMarkers);
						break;
					default:
						break;
				}
			}
			return success;
		}
	}
}
