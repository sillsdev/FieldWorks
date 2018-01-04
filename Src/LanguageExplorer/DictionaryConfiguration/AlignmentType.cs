// Copyright (c) 2014-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Serialization;

namespace LanguageExplorer.DictionaryConfiguration
{
	/// <summary />
	public enum AlignmentType
	{
		// Since Right=0, it is the default selected if nothing is specified in the xml
		[XmlEnum("right")]
		Right = 0,
		[XmlEnum("left")]
		Left
		//todo: add options for above and below entry
	}
}