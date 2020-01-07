// Copyright (c) 2014-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace LanguageExplorer.DictionaryConfiguration
{
	/// <summary>Options for selecting and ordering WritingSystems</summary>
	public class DictionaryNodeWritingSystemOptions : DictionaryNodeOptions
	{
		public DictionaryNodeWritingSystemOptions()
		{
			Options = new List<DictionaryNodeOption>();
		}

		// REVIEW (Hasso) 2014.02: since this never changes, we may not need it in the schema; however,
		// we will somehow need to determine which writing systems are available for each instance
		[XmlAttribute(AttributeName = "writingSystemType")]
		public WritingSystemType WsType { get; set; }

		[XmlAttribute(AttributeName = "displayWSAbreviation")]
		public bool DisplayWritingSystemAbbreviations { get; set; }

		[XmlElement(ElementName = "Option")]
		public List<DictionaryNodeOption> Options { get; set; }

		public override DictionaryNodeOptions DeepClone()
		{
			return DeepCloneInto(new DictionaryNodeWritingSystemOptions());
		}

		protected override DictionaryNodeOptions DeepCloneInto(DictionaryNodeOptions target)
		{
			base.DeepCloneInto(target);
			((DictionaryNodeWritingSystemOptions)target).Options = Options.Select(dno => new DictionaryNodeOption
			{
				Id = dno.Id,
				IsEnabled = dno.IsEnabled
			}).ToList();
			return target;
		}
	}
}