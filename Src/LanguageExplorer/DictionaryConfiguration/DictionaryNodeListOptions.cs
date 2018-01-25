// Copyright (c) 2014-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace LanguageExplorer.DictionaryConfiguration
{
	/// <summary>Options for selecting and ordering items in lists, including Custom Lists and WritingSystem lists</summary>
	public class DictionaryNodeListOptions : DictionaryNodeOptions
	{
		public DictionaryNodeListOptions()
		{
			Options = new List<DictionaryNodeOption>();
		}

		[XmlAttribute(AttributeName = "list")]
		public ListIds ListId { get; set; }

		/// <summary>ShouldSerialize___ is a magic method to prevent XmlSerializer from serializing ListId if there is none.</summary>
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

			((DictionaryNodeListOptions)target).Options = Options.Select(dno => new DictionaryNodeOption
			{
				Id = dno.Id, IsEnabled = dno.IsEnabled
			}).ToList();

			return target;
		}
	}
}