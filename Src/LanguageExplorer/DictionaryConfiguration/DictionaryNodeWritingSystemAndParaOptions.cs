// Copyright (c) 2014-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Serialization;

namespace LanguageExplorer.DictionaryConfiguration
{
	public class DictionaryNodeWritingSystemAndParaOptions : DictionaryNodeWritingSystemOptions, IParaOption
	{
		[XmlAttribute(AttributeName = "displayInParagraph")]
		public bool DisplayEachInAParagraph { get; set; }

		public override DictionaryNodeOptions DeepClone()
		{
			return DeepCloneInto(new DictionaryNodeWritingSystemAndParaOptions());
		}
	}
}