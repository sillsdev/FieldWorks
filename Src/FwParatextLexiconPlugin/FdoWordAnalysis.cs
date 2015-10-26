// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Paratext.LexicalContracts;

namespace SIL.FieldWorks.ParatextLexiconPlugin
{
	internal class FdoWordAnalysis : WordAnalysis
	{
		private readonly string m_word;
		private readonly Lexeme[] m_lexemes;

		public FdoWordAnalysis(string word, IEnumerable<Lexeme> lexemes)
		{
			if (!word.IsNormalized())
				throw new ArgumentException("The word is not normalized.", "word");
			m_word = word;
			m_lexemes = lexemes.ToArray();
		}

		public string Word
		{
			get { return m_word; }
		}

		public Lexeme this[int index]
		{
			get { return m_lexemes[index]; }
		}

		public int Length
		{
			get { return m_lexemes.Length; }
		}

		public override bool Equals(object obj)
		{
			var other = obj as FdoWordAnalysis;
			if (other == null || m_lexemes.Length != other.m_lexemes.Length)
				return false;

			for (int i = 0; i < m_lexemes.Length; i++)
			{
				if (!m_lexemes[i].Equals(other.m_lexemes[i]))
					return false;
			}
			return true;
		}

		public override int GetHashCode()
		{
			return m_lexemes.Aggregate(23, (code, lexeme) => code * 31 + lexeme.GetHashCode());
		}

		public IEnumerator<Lexeme> GetEnumerator()
		{
			return ((IEnumerable<Lexeme>)m_lexemes).GetEnumerator();
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "Don't need to dispose an IEnumerator.")]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
