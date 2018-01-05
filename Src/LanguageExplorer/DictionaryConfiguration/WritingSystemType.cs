// Copyright (c) 2014-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Serialization;

namespace LanguageExplorer.DictionaryConfiguration
{
	/// <summary />
	public enum WritingSystemType
	{
		[XmlEnum("vernacular")]
		Vernacular,
		[XmlEnum("analysis")]
		Analysis,
		//both Analysis and Vernacular
		[XmlEnum("both")]
		Both,
		[XmlEnum("pronunciation")]
		Pronunciation,
		[XmlEnum("reversal")]
		Reversal
	}
}