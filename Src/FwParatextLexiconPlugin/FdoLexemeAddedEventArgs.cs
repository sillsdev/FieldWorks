// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

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
