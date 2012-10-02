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
// File: PhraseParser.cs
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SIL.Utils;

namespace SILUBS.PhraseTranslationHelper
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Method class for parsing a translatable phrase into translatable parts and key terms.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class PhraseParser
	{
		private readonly Dictionary<Word, List<KeyTermMatch>> m_keyTermsTable;
		private readonly TranslatablePhrase m_phrase;
		private readonly Func<IEnumerable<Word>, TranslatablePhrase, int, Part> YieldPart;
		private readonly List<Word> m_words;
		/// <summary>The index of the current (potential) match</summary>
		private int m_iStartMatch;
		private int m_iNextWord;
		private List<KeyTermMatch> m_matches;
		private static PorterStemmer s_stemmer = new PorterStemmer();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="PhraseParser"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal PhraseParser(Dictionary<Word, List<KeyTermMatch>> keyTermsTable,
			Dictionary<Regex, string> substituteStrings, TranslatablePhrase phrase,
			Func<IEnumerable<Word>, TranslatablePhrase, int, Part> yieldPart)
		{
			m_keyTermsTable = keyTermsTable;
			YieldPart = yieldPart;
			m_phrase = phrase;

			string phraseToParse = m_phrase.OriginalPhrase;
			foreach (KeyValuePair<Regex, string> substituteString in substituteStrings)
				phraseToParse = substituteString.Key.Replace(phraseToParse, substituteString.Value);

			m_words = GetWordsInString(phraseToParse.Trim().ToLowerInvariant(), true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a list of all of the words in the specified string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal static List<Word> GetWordsInString(string phrase, bool createNewWords)
		{
			List<Word> words = new List<Word>();
			StringBuilder wordBldr = new StringBuilder();
			string word;
			foreach (char ch in phrase)
			{
				if (Char.IsLetter(ch) || ch == '\'' || ch == '-')
					wordBldr.Append(ch);
				else if (wordBldr.Length > 0)
				{
					word = wordBldr.ToString();
					if (createNewWords || Word.Exists(word))
						words.Add(wordBldr.ToString());
					wordBldr.Length = 0;
				}
			}
			word = wordBldr.ToString();
			if (word.Length > 0 && (createNewWords || Word.Exists(word)))
				words.Add(word);
			return words;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Cleans up the given source-language phrase (makes it all lowercase and removes
		/// punctuation and extraneous whitespace) and returns a collection of phrase parts,
		/// broken up by key terms.
		/// </summary>
		/// <returns>Collection of phrase parts</returns>
		/// ------------------------------------------------------------------------------------
		internal IEnumerable<IPhrasePart> Parse()
		{
			KeyTermMatch bestKeyTerm = null;
			int minUnhandled = 0;

			for (m_iStartMatch = m_iNextWord = 0; m_iNextWord < m_words.Count; )
			{
				bestKeyTerm = FindBestKeyTerm();
				if (bestKeyTerm == null)
					m_iNextWord++;
				else
				{
					// We've found the best key term we're going to find.
					int keyTermWordCount = bestKeyTerm.Words.Count();
					if (m_iStartMatch > minUnhandled)
						yield return YieldPart(m_words.Skip(minUnhandled).Take(m_iStartMatch - minUnhandled), m_phrase, 0);
					yield return bestKeyTerm;
					m_iStartMatch = m_iNextWord = minUnhandled = m_iStartMatch + keyTermWordCount;
				}
			}

			if (minUnhandled < m_words.Count)
			{
				yield return YieldPart(m_words.Skip(minUnhandled), m_phrase, 0);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the best key term in the list of words starting at m_iStartMatch and including
		/// up to m_iNextWord. As new words are considered, the list possible matches
		/// (m_matches) is reduced by any that no longer match until there is exactly one match
		/// that exactly equals the words in the key term or the list is empty.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private KeyTermMatch FindBestKeyTerm()
		{
			Word nextWord = m_words[m_iNextWord];

			if (m_iStartMatch == m_iNextWord)
			{
				List<KeyTermMatch> matches;
				if (!m_keyTermsTable.TryGetValue(nextWord, out matches))
				{
					Word stem = s_stemmer.stemTerm(nextWord);
					if (m_keyTermsTable.TryGetValue(stem, out matches))
						stem.AddAlternateForm(nextWord);
					else
					{
						m_iStartMatch++;
						return null;
					}
				}

				m_matches = new List<KeyTermMatch>(matches.Where(m => m.AppliesTo(m_phrase.StartRef, m_phrase.EndRef)));

				// If we found a one-word exact match and there are no other key terms that start
				// with that word, then we return it. The code below would handle this, but it's such
				// a common case, we want it to be fast. If there are one or more multi-word key
				// terms that start with this word, we need to keep looking.
				if (m_matches.Count == 1 && m_matches[0].Words.Count() == 1)
					return m_matches[0];
			}

			int cMatchingWordsInTermSoFar = m_iNextWord - m_iStartMatch + 1;
			int lengthOfBestMatch = 0;
			KeyTermMatch longestMatch = null;

			// Remove from the possible matches any that don't match so far
			for (int iTerm = 0; iTerm < m_matches.Count; iTerm++)
			{
				KeyTermMatch term = m_matches[iTerm];
				if (!PhraseEqualsKeyTermSoFar(term, cMatchingWordsInTermSoFar) ||
					(AtEndOfPhrase && term.m_words.Count > cMatchingWordsInTermSoFar))
					m_matches.RemoveAt(iTerm--);
				else if (term.m_words.Count > lengthOfBestMatch)
				{
					lengthOfBestMatch = term.m_words.Count;
					longestMatch = term;
				}
			}

			if (m_matches.Count == 0)
			{
				// The only matches we had were multi-word matches, and the addition of the current
				// word made it so that none of them matched. Therefore, we don't have a key term
				// starting at iStartMatch.
				m_iNextWord = m_iStartMatch; // The for loop in Parse will increment this.
				m_iStartMatch++;
				return null;
			}

			if ((m_matches.Count == 1 && lengthOfBestMatch < cMatchingWordsInTermSoFar) || (lengthOfBestMatch == cMatchingWordsInTermSoFar))
				return longestMatch;

			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether we're working on the last word in the phrase.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool AtEndOfPhrase
		{
			get { return m_iNextWord == m_words.Count - 1; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the part of the phrase we're considering matches the key term so
		/// far.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool PhraseEqualsKeyTermSoFar(KeyTermMatch term, int cMatchingWordsInTermSoFar)
		{
			int cCompare = Math.Min(term.m_words.Count, cMatchingWordsInTermSoFar);
			for (int iWord = m_iStartMatch; iWord < cCompare + m_iStartMatch; iWord++)
			{
				if (!term.m_words[iWord - m_iStartMatch].IsEquivalent(m_words[iWord]))
					return false;
			}
			return true;
		}
	}
}
