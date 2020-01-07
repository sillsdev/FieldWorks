// Copyright (c) 2016-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml;

namespace LanguageExplorer.SfmToXml
{
	/// <summary>
	/// This interface defines a way of interacting with the import options checkboxes
	/// that were read in from the controlled xml file that is used for remembering the
	/// settings in the import process used in FieldWorks.
	/// </summary>
	public interface ILexImportOption
	{
		string Id { get; }      // this is the key to a checkbox in the Import Wizard
		string Type { get; }    // currently supported: "Checkbox"
		bool IsChecked { get; } // this is the persistable checkbox state if option is of Type "Checkbox"
		string ToXmlString();
		bool ReadXmlNode(XmlNode optDef);
	}
}