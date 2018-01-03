// Copyright (c) 2014-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Serialization;

namespace LanguageExplorer.Works
{
	public enum StyleTypes
	{
		[XmlEnum("default")]
		Default = 0,
		[XmlEnum("character")]
		Character,
		[XmlEnum("paragraph")]
		Paragraph
	}
}