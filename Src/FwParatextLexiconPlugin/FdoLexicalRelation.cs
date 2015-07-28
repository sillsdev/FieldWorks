// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

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
