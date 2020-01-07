// Copyright (c) 2014-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Serialization;

namespace LanguageExplorer.DictionaryConfiguration
{
	/// <summary>Options for items that may be displayed in paragraphs</summary>
	public class DictionaryNodeListAndParaOptions : DictionaryNodeListOptions, IParaOption
	{
		[XmlAttribute(AttributeName = "displayEachComplexFormInParagraph")]
		public bool DisplayEachInAParagraph { get; set; }

		public override DictionaryNodeOptions DeepClone()
		{
			return DeepCloneInto(new DictionaryNodeListAndParaOptions());
		}
	}
}