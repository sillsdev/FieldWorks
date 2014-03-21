using Paratext.LexicalContracts;

namespace SIL.FieldWorks.ParatextLexiconPlugin
{
	internal class FdoLexicalRelation : LexicalRelation
	{
		private readonly Lexeme m_lexeme;
		private readonly string m_name;

		public FdoLexicalRelation(Lexeme lexeme, string name)
		{
			m_lexeme = lexeme;
			m_name = name;
		}

		public Lexeme Lexeme
		{
			get { return m_lexeme; }
		}

		public string Name
		{
			get { return m_name; }
		}
	}
}
