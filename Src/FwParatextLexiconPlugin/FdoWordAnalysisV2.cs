// // Copyright (c) $year$ SIL International
// // This software is licensed under the LGPL, version 2.1 or later
// // (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Paratext.LexicalContracts;
using SIL.LCModel;
using SIL.Linq;

namespace SIL.FieldWorks.ParatextLexiconPlugin
{
	/// <summary>
	/// Class to handle the WordAnalysis objects in Paratext.LexiconPluginV2
	/// </summary>
	internal class FdoWordAnalysisV2 : WordAnalysisV2
	{
		private Lexeme[] _lexemes;

		public FdoWordAnalysisV2(Guid id, string word, IEnumerable<Lexeme> lexemes, IEnumerable<IWfiGloss> glosses = null)
		{
			Id = id.ToString();
			Word = word;
			_lexemes = lexemes.ToArray();
			Glosses = new List<LanguageText>();
			glosses?.ForEach(g =>
				((List<LanguageText>)Glosses).Add(new FdoLanguageText(
					g.Cache.LangProject.DefaultAnalysisWritingSystem.Id,
					g.Form.AnalysisDefaultWritingSystem.Text)));
		}

		public FdoWordAnalysisV2(string word, IEnumerable<Lexeme> lexemes) : this(Guid.NewGuid(), word, lexemes)
		{
		}

		public IEnumerator<Lexeme> GetEnumerator()
		{
			return ((IEnumerable<Lexeme>)_lexemes).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public string Word { get; }

		public Lexeme this[int index] => _lexemes[index];

		public int Length => _lexemes.Length;
		public string Id { get; }
		public IEnumerable<LanguageText> Glosses { get; }
	}
}