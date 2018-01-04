using System.Xml.Serialization;

namespace LanguageExplorer.DictionaryConfiguration
{
	/// <summary />
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
		Pronunciation,
		[XmlEnum("reversal")]
		Reversal
	}
}