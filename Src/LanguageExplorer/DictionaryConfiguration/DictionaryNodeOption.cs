// Copyright (c) 2014-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Serialization;

namespace LanguageExplorer.DictionaryConfiguration
{
	public class DictionaryNodeOption
	{
		[XmlAttribute(AttributeName = "id")]
		public string Id { get; set; }
		[XmlAttribute(AttributeName = "isEnabled")]
		public bool IsEnabled { get; set; }

		public override string ToString()
		{
			return $"[{(IsEnabled ? "X" : "_")}] {Id}";
		}
	}
}