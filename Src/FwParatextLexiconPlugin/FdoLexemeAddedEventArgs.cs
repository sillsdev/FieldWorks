using System;
using Paratext.LexicalContracts;

namespace SIL.FieldWorks.ParatextLexiconPlugin
{
	internal class FdoLexemeAddedEventArgs : EventArgs, LexemeAddedEventArgs
	{
		private readonly Lexeme m_lexeme;

		public FdoLexemeAddedEventArgs(Lexeme lexeme)
		{
			m_lexeme = lexeme;
		}

		public Lexeme Lexeme
		{
			get { return m_lexeme; }
		}
	}
}
