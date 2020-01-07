// Copyright (c) 2014-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Serialization;

namespace LanguageExplorer.DictionaryConfiguration
{
	/// <remarks>deprecated; needed for migration</remarks>
	public class DictionaryNodeReferringSenseOptions : DictionaryNodeOptions
	{
		[XmlElement(ElementName = "WritingSystemOptions")]
		public DictionaryNodeWritingSystemOptions WritingSystemOptions { get; set; }

		public override DictionaryNodeOptions DeepClone()
		{
			return WritingSystemOptions.DeepClone(); // this is what migration should do anyway
		}
	}
}