// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: KeyTermMatch.cs
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.Utils;

namespace SILUBS.PhraseTranslationHelper
{
	public class KeyTermMatch : IPhrasePart
	{
		internal readonly List<Word> m_words;
		private readonly List<IKeyTerm> m_terms;
		private string m_bestTranslation = null;
		private readonly bool m_matchForRefOnly;
		private HashSet<int> m_occurrences;

		internal KeyTermMatch(IEnumerable<Word> words, IKeyTerm term, bool matchForRefOnly)
		{
			m_matchForRefOnly = matchForRefOnly;
			m_words = words.ToList();
			m_terms = new List<IKeyTerm>();
			m_terms.Add(term);
		}

		public IEnumerable<Word> Words
		{
			get { return m_words; }
		}

		public override bool Equals(object obj)
		{
			if (obj is KeyTermMatch)
			{
				return m_words.SequenceEqual(((KeyTermMatch)obj).m_words);
			}
			if (obj is IEnumerable<Word>)
			{
				return m_words.SequenceEqual((IEnumerable<Word>)obj);
			}
			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public bool AppliesTo(int startRef, int endRef)
		{
			if (!m_matchForRefOnly)
				return true;
			if (m_occurrences == null)
				m_occurrences = new HashSet<int>(m_terms.SelectMany(term => term.BcvOccurences));
			return m_occurrences.Any(o => startRef <= o && endRef >= o);
		}

		public void AddTerm(IKeyTerm keyTerm)
		{
			if (keyTerm == null)
				throw new ArgumentNullException("keyTerm");
			m_terms.Add(keyTerm);
		}

		public void AddWord(Word word)
		{
			if (word == null)
				throw new ArgumentNullException("word");
			m_words.Add(word);
		}

		public void AddWords(IEnumerable<Word> words)
		{
			m_words.AddRange(words);
		}

		public override string ToString()
		{
			return m_words.ToString(" ");
		}

		public string Translation
		{
			get
			{
				if (m_bestTranslation == null)
				{
					int max = 0;
					Dictionary<string, int> occurrences = new Dictionary<string, int>();
					foreach (string rendering in m_terms.Select(keyTerm => keyTerm.BestRendering).Where(rendering => rendering != null))
					{
						string normalizedRendering = rendering.Normalize(NormalizationForm.FormD);
						int num;
						occurrences.TryGetValue(normalizedRendering, out num);
						occurrences[normalizedRendering] = ++num;
						if (num > max)
						{
							m_bestTranslation = normalizedRendering;
							max = num;
						}
					}
					if (m_bestTranslation == null)
						m_bestTranslation = string.Empty;
				}
				return m_bestTranslation;
			}
		}

		public string DebugInfo
		{
			get { return "KT: " + Translation; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets all renderings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IEnumerable<string> AllRenderings
		{
			get { return m_terms.SelectMany(keyTerm => keyTerm.Renderings).Select(r => r.Normalize(NormalizationForm.FormD)); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets all the key terms for this match.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IEnumerable<IKeyTerm> AllTerms
		{
			get { return m_terms; }
		}
	}
}