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
	[XmlInclude(typeof(DictionaryNodeSenseOptions))]
	[XmlInclude(typeof(DictionaryNodeListOptions))]
	[XmlInclude(typeof(DictionaryNodeWritingSystemOptions))]
	[XmlInclude(typeof(DictionaryNodeComplexFormOptions))]
	public abstract class DictionaryNodeOptions {}

	/// <summary>Options for formatting Senses</summary>
	public class DictionaryNodeSenseOptions : DictionaryNodeOptions
	{
		[XmlAttribute(AttributeName = "numberBefore")]
		public string BeforeNumber { get; set; }

		// Example values: ""->none; %O->1.2.3; %z->1, b, iii
		[XmlAttribute(AttributeName = "numberMark")]
		public string NumberingStyle { get; set; }

		[XmlAttribute(AttributeName = "numberAfter")]
		public string AfterNumber { get; set; }

		// Whether the sense number should be bold and/or italic.
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
		public enum ListIds
		{
			// REVIEW (Hasso) 2014.04: One instance of Complex Forms doesn't display the list in the old dialog. This may not be needed.
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
			Entry
		}

		public class DictionaryNodeOption
		{
			[XmlAttribute(AttributeName = "id")]
			public string Id { get; set; }
			[XmlAttribute(AttributeName = "isEnabled")]
			public bool IsEnabled { get; set; } // REVIEW pH 2014.03: do we need this?  isn't everything here enabled by merit of being here?
		}

		[XmlAttribute(AttributeName = "list")]
		public ListIds ListId { get; set; }

		/// <summary>ShouldSerialize___ is a magic method to prevent XmlSerializer from serializing ListId if there is none.</summary>
		/// <note>Per https://bugzilla.xamarin.com/show_bug.cgi?id=1852, this does not work until Mono 3.3.0</note>
		public bool ShouldSerializeListId()
		{
			return ListId != ListIds.None;
		}

		[XmlElement(ElementName = "Option")]
		public List<DictionaryNodeOption> Options { get; set; }
	}

	/// <summary>Options for Referenced Complex Forms</summary>
	public class DictionaryNodeComplexFormOptions : DictionaryNodeListOptions
	{
		[XmlAttribute(AttributeName = "displayEachComplexFormInParagraph")]
		public bool DisplayEachComplexFormInAParagraph { get; set; }
	}

	/// <summary>Options for selecting and ordering WritingSystems</summary>
	public class DictionaryNodeWritingSystemOptions : DictionaryNodeOptions
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

		[XmlElement(ElementName = "Option")]
		public List<DictionaryNodeListOptions.DictionaryNodeOption> Options { get; set; }
	}
}
