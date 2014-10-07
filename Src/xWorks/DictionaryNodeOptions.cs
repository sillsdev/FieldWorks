// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
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
	[XmlInclude(typeof(DictionaryNodePictureOptions))]
	public abstract class DictionaryNodeOptions
	{
		/// <summary>
		/// Deeply clones all members of this <see cref="DictionaryNodeOptions"/>
		/// </summary>
		/// <returns>an identical but independent instance of this <see cref="DictionaryNodeOptions"/></returns>
		public abstract DictionaryNodeOptions DeepClone();

		/// <summary>
		/// Clones all writable properties, importantly handling strings and primitives
		/// </summary>
		/// <param name="target"></param>
		/// <returns><see cref="target"/>, as a convenience</returns>
		protected virtual DictionaryNodeOptions DeepCloneInto(DictionaryNodeOptions target)
		{
			var properties = GetType().GetProperties();
			foreach (var property in properties.Where(prop => prop.CanWrite)) // Skip any read-only properties
			{
				var originalValue = property.GetValue(this, null);
				property.SetValue(target, originalValue, null);
			}
			return target;
		}
	}

	/// <summary>Options for formatting Senses</summary>
	public class DictionaryNodeSenseOptions : DictionaryNodeOptions
	{
		// Character Style applied to Sense Numbers
		[XmlAttribute(AttributeName = "numberStyle")]
		public string NumberStyle { get; set; }

		[XmlAttribute(AttributeName = "numberBefore")]
		public string BeforeNumber { get; set; }

		// Example values: ""->none; %O->1.2.3; %z->1, b, iii
		[XmlAttribute(AttributeName = "numberingStyle")]
		public string NumberingStyle { get; set; }

		[XmlAttribute(AttributeName = "numberAfter")]
		public string AfterNumber { get; set; }

		[XmlAttribute(AttributeName = "numberSingleSense")]
		public bool NumberEvenASingleSense { get; set; }

		[XmlAttribute(AttributeName = "showSingleGramInfoFirst")]
		public bool ShowSharedGrammarInfoFirst { get; set; }

		[XmlAttribute(AttributeName = "displayEachSenseInParagraph")]
		public bool DisplayEachSenseInAParagraph { get; set; }

		public override DictionaryNodeOptions DeepClone()
		{
			return DeepCloneInto(new DictionaryNodeSenseOptions());
		}
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

		public override DictionaryNodeOptions DeepClone()
		{
			return DeepCloneInto(new DictionaryNodeListOptions());
		}

		protected override DictionaryNodeOptions DeepCloneInto(DictionaryNodeOptions target)
		{
			base.DeepCloneInto(target);

			if (Options != null)
				((DictionaryNodeListOptions)target).Options = Options.Select(dno => new DictionaryNodeOption
				{
					Id = dno.Id, IsEnabled = dno.IsEnabled
				}).ToList();

			return target;
		}
	}

	/// <summary>Options for Referenced Complex Forms</summary>
	public class DictionaryNodeComplexFormOptions : DictionaryNodeListOptions
	{
		[XmlAttribute(AttributeName = "displayEachComplexFormInParagraph")]
		public bool DisplayEachComplexFormInAParagraph { get; set; }

		public override DictionaryNodeOptions DeepClone()
		{
			return DeepCloneInto(new DictionaryNodeComplexFormOptions());
		}
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

		public override DictionaryNodeOptions DeepClone()
		{
			return DeepCloneInto(new DictionaryNodeWritingSystemOptions());
		}

		protected override DictionaryNodeOptions DeepCloneInto(DictionaryNodeOptions target)
		{
			base.DeepCloneInto(target);

			if (Options != null)
				((DictionaryNodeWritingSystemOptions)target).Options = Options.Select(dno => new DictionaryNodeListOptions.DictionaryNodeOption
				{
					Id = dno.Id, IsEnabled = dno.IsEnabled
				}).ToList();

			return target;
		}
	}

	/// <summary>Options for formatting Pictures</summary>
	public class DictionaryNodePictureOptions : DictionaryNodeOptions
	{
		public enum AlignmentType
		{
			// Since Right=0, it is the default selected if nothing is specified in the xml
			[XmlEnum("right")]
			Right = 0,
			[XmlEnum("left")]
			Left
			//todo: add options for above and below entry
		}

		[XmlAttribute(AttributeName = "minimumHeight")]
		public float MinimumHeight { get; set; }

		[XmlAttribute(AttributeName = "minimumWidth")]
		public float MinimumWidth { get; set; }

		[XmlAttribute(AttributeName = "maximumHeight")]
		public float MaximumHeight { get; set; }

		[XmlAttribute(AttributeName = "maximumWidth")]
		public float MaximumWidth { get; set; }

		[XmlAttribute(AttributeName = "pictureLocation")]
		public AlignmentType PictureLocation { get; set; }

		[XmlAttribute(AttributeName = "stackPictures")]
		public bool StackMultiplePictures { get; set; }

		public override DictionaryNodeOptions DeepClone()
		{
			return DeepCloneInto(new DictionaryNodePictureOptions());
		}
	}
}
