using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Paratext.LexicalContracts;

namespace SIL.FieldWorks.ParatextLexiconPlugin
{
	internal class FdoWordAnalysis : WordAnalysis
	{
		private readonly Lexeme[] m_lexemes;

		public FdoWordAnalysis(IEnumerable<Lexeme> lexemes)
		{
			m_lexemes = lexemes.ToArray();
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
