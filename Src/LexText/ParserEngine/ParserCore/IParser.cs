using System.Collections;

namespace SIL.FieldWorks.WordWorks.Parser
{
	interface IParser
	{
		void Initialize();

		ParseResult ParseWord(string word);

		string TraceWord(string word);
	}
}
