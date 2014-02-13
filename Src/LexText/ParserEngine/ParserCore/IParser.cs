using System.Xml.Linq;

namespace SIL.FieldWorks.WordWorks.Parser
{
	public interface IParser
	{
		// Warning: This method is not thread-safe.
		//  The parser could end up using stale data.
		bool IsUpToDate();

		void Update();

		void Reset();

		ParseResult ParseWord(string word);

		XDocument ParseWordXml(string word);

		XDocument TraceWordXml(string word, string selectTraceMorphs);
	}
}
