// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Xml.Serialization;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Base class for ConfigurableDictionaryNode options
	/// <note>This would be an interface, but the XMLSerialization doesn't like those</note>
	/// </summary>
	public abstract class DictionaryNodeOptions {}

	/// <summary>Options for formatting Senses</summary>
	public class DictionaryNodeSenseOptions : DictionaryNodeOptions
	{
		[XmlAttribute(AttributeName = "numberBefore")]
		public string BeforeNumber { get; set; }

		// Valid values: ""->none; %O->1.2.3; %z->1, b, iii
		[XmlAttribute(AttributeName = "numberMark")]
		public string NumberMark { get; set; }

		[XmlAttribute(AttributeName = "numberAfter")]
		public string AfterNumber { get; set; }

		// currently represents bold and italic
		[XmlAttribute(AttributeName = "numberStyle")]
		public string NumberStyle { get; set; }

		[XmlAttribute(AttributeName = "numberFont")]
		public string NumberFont { get; set; }

		[XmlAttribute(AttributeName = "numberSingleSense")]
		public bool NumberEvenASingleSense { get; set; }

		[XmlAttribute(AttributeName = "showSingleGramInfoFirst")]
		public bool ShowSharedGrammarInfoFirst { get; set; }

		[XmlAttribute(AttributeName = "displayEachSenseInParagraph")]
		public bool DisplayEachSenseInAParagraph { get; set; }
	}

	/// <summary>Options for selecting and ordering items in lists, including Custom Lists and WritingSystem lists</summary>
	public class DictionaryNodeListOptions : DictionaryNodeOptions
	{
		public class DictionaryNodeOption
		{
			[XmlIgnore]
			public string Label { get; set; }
			[XmlAttribute(AttributeName = "id")]
			public string Id { get; set; }
			[XmlAttribute(AttributeName = "isEnabled")]
			public bool IsEnabled { get; set; }
		}

		[XmlElement(ElementName = "Options")]
		public List<DictionaryNodeOption> Options { get; set; }
	}

	/// <summary>Options for Referenced Complex Forms</summary>
	public class DictionaryNodeComplexFormOptions : DictionaryNodeListOptions
	{
		[XmlAttribute(AttributeName = "displayEachComplexFormInParagraph")]
		public bool DisplayEachComplexFormInAParagraph { get; set; }
	}

	/// <summary>Options for selecting and ordering WritingSystems</summary>
	public class DictionaryNodeWritingSystemOptions : DictionaryNodeListOptions
	{
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
			Pronunciation
		}

		// REVIEW (Hasso) 2014.02: since this never changes, we may not need it in the schema; however,
		// we will somehow need to determine which writing systems are available for each instance
		[XmlAttribute(AttributeName = "writingSystemType")]
		public WritingSystemType WsType { get; set; }

		[XmlAttribute(AttributeName = "displayWSAbreviation")]
		public bool DisplayWritingSystemAbbreviations { get; set; }
	}
}
