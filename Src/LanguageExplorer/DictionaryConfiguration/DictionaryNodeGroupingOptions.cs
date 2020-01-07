// Copyright (c) 2014-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Serialization;

namespace LanguageExplorer.DictionaryConfiguration
{
	/// <summary>Options for allowing the grouping of nodes which are not related in the model</summary>
	public class DictionaryNodeGroupingOptions : DictionaryNodeOptions, IParaOption
	{
		[XmlText]
		public string Description { get; set; }

		[XmlAttribute(AttributeName = "displayGroupInParagraph")]
		public bool DisplayEachInAParagraph { get; set; }

		public override DictionaryNodeOptions DeepClone()
		{
			return DeepCloneInto(new DictionaryNodeGroupingOptions());
		}
	}
}