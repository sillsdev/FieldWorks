// Copyright (c) 2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;

namespace LanguageExplorer
{
	internal interface IPersistAsXmlFactoryStrategy
	{
		FactoryNames Name { get; }
		IPersistAsXml Create(IPersistAsXmlFactory persistAsXmlFactory, XElement element);
	}
}