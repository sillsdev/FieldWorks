// Copyright (c) 2014-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Serialization;

namespace LanguageExplorer.DictionaryConfiguration
{
	/// <summary />
	public enum ListIds
	{
		// Since None=0, it is the default selected if nothing is specified in the xml
		[XmlEnum("none")]
		None = 0,
		/// <summary>Minor Entry could be a Complex or Variant Form</summary>
		[XmlEnum("minor")]
		Minor,
		[XmlEnum("complex")]
		Complex,
		[XmlEnum("variant")]
		Variant,
		/// <summary>Lexical Relations, including Reverses, having to do with Sense</summary>
		[XmlEnum("sense")]
		Sense,
		/// <summary>Lexical Relations, including Reverses, having to do with Entry</summary>
		[XmlEnum("entry")]
		Entry,
		/// <summary>Extended Note Types</summary>
		[XmlEnum("note")]
		Note
	}
}