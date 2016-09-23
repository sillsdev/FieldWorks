using System;
using System.Xml;

namespace Sfm2Xml
{
	/// <summary>
	/// This interface defines a way of interacting with the import options checkboxes
	/// that were read in from the controlled xml file that is used for remembering the
	/// settings in the import process used in FieldWorks.
	/// </summary>
	public interface ILexImportOption
	{
		string Id { get; }		// this is the key to a checkbox in the Import Wizard
		string Type { get; }	// currently supported: "Checkbox"
		bool IsChecked { get; }	// this is the persistable checkbox state if option is of Type "Checkbox"
		string ToXmlString();
		bool ReadXmlNode(XmlNode optDef);
	}

	/// <summary>
	/// This class is used to read/write the Options section of the .map xml file that remembers
	/// the settings used in the import process in FieldWorks. Currently the only type of option
	/// that is supported is whether or not a checkbox is checked.
	/// </summary>
	public class LexImportOption : ILexImportOption
	{
		private string m_id;
		private string m_type;
		private bool m_isChecked;

		public string Id { get { return m_id; } }
		public string Type { get { return m_type; } }
		public bool IsChecked { get { return m_isChecked; } }

		public LexImportOption()
		{
			m_id = string.Empty;
			m_type = string.Empty;
			m_isChecked = false;
		}

		public LexImportOption(string id, string type, bool isChecked)
		{
			m_id = id;
			m_type = type;
			m_isChecked = isChecked;
		}

		public string ToXmlString()
		{
			return "<option id=\"" + Id + "\" type=\"" + Type + "\" checked=\"" + IsChecked + "\"/>";
		}

		public bool ReadXmlNode(XmlNode optDef)
		{
			bool success = true;

			foreach (XmlAttribute Attribute in optDef.Attributes)
			{
				switch (Attribute.Name)
				{
					case "id":
						if (Attribute.Value == "")
							success = false;
						else
							m_id = Attribute.Value;
						break;
					case "type":
						if (Attribute.Value == "")
							success = false;
						else
							m_type = Attribute.Value;
						break;
					case "checked":
						if (Attribute.Value == "")
							success = false;
						else
							m_isChecked = STATICS.IsBoolString(Attribute.Value, false);
						break;
					default:
						Converter.Log.AddWarning(String.Format(Sfm2XmlStrings.UnknownAttribute0InTheOptions, Attribute.Name));
						break;
				}
			}

			if (!string.IsNullOrEmpty(Id))
				return success;

			Converter.Log.AddError(Sfm2XmlStrings.IdNotDefinedInAnOption);
			return false;
		}
	}
}
