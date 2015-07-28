// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using Paratext.LexicalContracts;

namespace SIL.FieldWorks.ParatextLexiconPlugin
{
	internal class FdoLexiconGlossAddedEventArgs : EventArgs, LexiconGlossAddedEventArgs
	{
		private readonly Lexeme m_lexeme;
		private readonly LexiconSense m_sense;
		private readonly LanguageText m_gloss;

		public FdoLexiconGlossAddedEventArgs(Lexeme lexeme, LexiconSense sense, LanguageText gloss)
		{
			m_lexeme = lexeme;
			m_sense = sense;
			m_gloss = gloss;
		}

		public Lexeme Lexeme
		{
			get { return m_lexeme; }
		}

		public LexiconSense Sense
		{
			get { return m_sense; }
		}

		public LanguageText Gloss
		{
			get { return m_gloss; }
		}
	}
}
