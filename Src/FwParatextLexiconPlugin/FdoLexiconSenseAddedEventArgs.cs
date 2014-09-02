using System;
using Paratext.LexicalContracts;

namespace SIL.FieldWorks.ParatextLexiconPlugin
{
	internal class FdoLexiconSenseAddedEventArgs : EventArgs, LexiconSenseAddedEventArgs
	{
		private readonly Lexeme m_lexeme;
		private readonly LexiconSense m_sense;

		public FdoLexiconSenseAddedEventArgs(Lexeme lexeme, LexiconSense sense)
		{
			m_lexeme = lexeme;
			m_sense = sense;
		}

		public Lexeme Lexeme
		{
			get { return m_lexeme; }
		}

		public LexiconSense Sense
		{
			get { return m_sense; }
		}
	}
}
