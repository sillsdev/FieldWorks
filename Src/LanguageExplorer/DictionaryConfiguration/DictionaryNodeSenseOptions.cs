// Copyright (c) 2014-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Serialization;

namespace LanguageExplorer.DictionaryConfiguration
{
	/// <summary>Options for formatting Senses</summary>
	public class DictionaryNodeSenseOptions : DictionaryNodeOptions
	{
		// Character Style applied to Sense Numbers
		[XmlAttribute(AttributeName = "numberStyle")]
		public string NumberStyle { get; set; }

		[XmlAttribute(AttributeName = "numberBefore")]
		public string BeforeNumber { get; set; }

		// Example values: ""->none; %O->1.2.3; %d->1, 2, 3
		[XmlAttribute(AttributeName = "numberingStyle")]
		public string NumberingStyle { get; set; }

		// Example values: ""->none; %j->Joined; %.->Separated by dot
		[XmlAttribute(AttributeName = "parentSenseNumberingStyle")]
		public string ParentSenseNumberingStyle { get; set; }

		[XmlAttribute(AttributeName = "numberAfter")]
		public string AfterNumber { get; set; }

		[XmlAttribute(AttributeName = "numberSingleSense")]
		public bool NumberEvenASingleSense { get; set; }

		[XmlAttribute(AttributeName = "showSingleGramInfoFirst")]
		public bool ShowSharedGrammarInfoFirst { get; set; }

		[XmlAttribute(AttributeName = "displayEachSenseInParagraph")]
		public bool DisplayEachSenseInAParagraph { get; set; }

		[XmlAttribute(AttributeName = "displayFirstSenseInline")]
		public bool DisplayFirstSenseInline { get; set; }

		public override DictionaryNodeOptions DeepClone()
		{
			return DeepCloneInto(new DictionaryNodeSenseOptions());
		}
	}
}