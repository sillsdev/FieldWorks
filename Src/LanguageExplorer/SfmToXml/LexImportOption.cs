// Copyright (c) 2016-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml;

namespace LanguageExplorer.SfmToXml
{
	/// <summary>
	/// This class is used to read/write the Options section of the .map xml file that remembers
	/// the settings used in the import process in FieldWorks. Currently the only type of option
	/// that is supported is whether or not a checkbox is checked.
	/// </summary>
	public class LexImportOption : ILexImportOption
	{
		public string Id { get; private set; }

		public string Type { get; private set; }

		public bool IsChecked { get; private set; }

		public LexImportOption()
		{
			Id = string.Empty;
			Type = string.Empty;
			IsChecked = false;
		}

		public LexImportOption(string id, string type, bool isChecked)
		{
			Id = id;
			Type = type;
			IsChecked = isChecked;
		}

		public string ToXmlString()
		{
			return $"<option id=\"{Id}\" type=\"{Type}\" checked=\"{IsChecked}\"/>";
		}

		public bool ReadXmlNode(XmlNode optDef)
		{
			var success = true;
			foreach (XmlAttribute attribute in optDef.Attributes)
			{
				switch (attribute.Name)
				{
					case "id":
						if (attribute.Value == string.Empty)
						{
							success = false;
						}
						else
						{
							Id = attribute.Value;
						}
						break;
					case "type":
						if (attribute.Value == string.Empty)
						{
							success = false;
						}
						else
						{
							Type = attribute.Value;
						}
						break;
					case "checked":
						if (attribute.Value == string.Empty)
						{
							success = false;
						}
						else
						{
							IsChecked = SfmToXmlServices.IsBoolString(attribute.Value, false);
						}
						break;
					default:
						Converter.Log.AddWarning(string.Format(SfmToXmlStrings.UnknownAttribute0InTheOptions, attribute.Name));
						break;
				}
			}
			if (!string.IsNullOrEmpty(Id))
			{
				return success;
			}
			Converter.Log.AddError(SfmToXmlStrings.IdNotDefinedInAnOption);
			return false;
		}
	}
}