// ---------------------------------------------------------------------------------------------
// Copyright (c) 2011-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: KeyTermMatchBuilder.cs
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace SILUBS.PhraseTranslationHelper
{
	public class KeyTermMatchBuilder
	{
		private readonly List<KeyTermMatch> m_list = new List<KeyTermMatch>();
		private List<Word> m_optionalPhraseWords;
		private bool m_fInOptionalPhrase;
		private bool m_fMatchForRefOnly;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="KeyTermMatchBuilder"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public KeyTermMatchBuilder(IKeyTerm keyTerm) : this(keyTerm, null)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="KeyTermMatchBuilder"/> class.
		/// </summary>
		/// <param name="keyTerm">The key term.</param>
		/// <param name="rules">Optional dictionary of (English) key terms to rules indicating
		/// special handling neeeded.</param>
		/// ------------------------------------------------------------------------------------
		public KeyTermMatchBuilder(IKeyTerm keyTerm, Dictionary<string, KeyTermRule> rules)
		{
			string normalizedLcTerm = keyTerm.Term.ToLowerInvariant().Normalize(NormalizationForm.FormD);
			KeyTermRule ktRule;
			if (rules != null && rules.TryGetValue(normalizedLcTerm, out ktRule))
			{
				bool fExcludeMainTerm = false;
				if (ktRule.Rule != null)
				{
					switch (ktRule.Rule)
					{
						case KeyTermRule.RuleType.Exclude: fExcludeMainTerm = true; break;
						case KeyTermRule.RuleType.MatchForRefOnly: m_fMatchForRefOnly = true; break;
					}
				}
				if (ktRule.Alternates != null)
				{
					foreach (string phrase in ktRule.Alternates.Select(a => a.Name))
						ProcessKeyTermPhrase(keyTerm, phrase);
				}
				if (fExcludeMainTerm)
					return;
			}
			foreach (string phrase in normalizedLcTerm.Split(new[] { ", or ", ",", "=" }, StringSplitOptions.RemoveEmptyEntries))
				ProcessKeyTermPhrase(keyTerm, phrase);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes a single alternative word or phrase for a key term. Most key terms have a
		/// simple "source" (actually English) rendering that consists of a single word or
		/// phrase. But some have multiple alternative words or phrases; hence, this method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ProcessKeyTermPhrase(IKeyTerm keyTerm, string phrase)
		{
			int startOfListForPhrase = m_list.Count;
			string[] orParts = phrase.Split(new [] {" or "}, 2, StringSplitOptions.RemoveEmptyEntries);
			if (orParts.Length == 2)
			{
				int ichEndOfPreOrPhrase = orParts[0].Length;
				int ichStartOfPostOrPhrase = 0;
				int ichPre, ichPost;
				do
				{
					ichPre = orParts[0].LastIndexOf(' ', ichEndOfPreOrPhrase - 1);
					ichPost = orParts[1].IndexOf(' ', ichStartOfPostOrPhrase + 1);
					ichEndOfPreOrPhrase = (ichPre >= 0) ? ichPre : 0;
					ichStartOfPostOrPhrase = (ichPost >= 0) ? ichPost : orParts[1].Length;
				} while (ichEndOfPreOrPhrase > 0 && ichPost >= 0);

				if (ichEndOfPreOrPhrase > 0)
					ichEndOfPreOrPhrase++;
				ProcessKeyTermPhrase(keyTerm, orParts[0] + orParts[1].Substring(ichStartOfPostOrPhrase));
				ProcessKeyTermPhrase(keyTerm, orParts[0].Substring(0, ichEndOfPreOrPhrase) + orParts[1]);
				return;
			}

			// Initially, we add one empty list
			m_list.Add(new KeyTermMatch(new Word[0], keyTerm, m_fMatchForRefOnly));
			bool firstWordOfPhrase = true;
			foreach (Word metaWord in phrase.Split(new[]{' '}, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim('\'')))
			{
				List<Word> allWords = AllWords(metaWord, firstWordOfPhrase);
				if (allWords.Count > 0)
					AddWordsToMatches(keyTerm, allWords, startOfListForPhrase);
				firstWordOfPhrase = false;
			}

			if (m_fInOptionalPhrase)
				AddWordsToMatches(keyTerm, m_optionalPhraseWords, startOfListForPhrase);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the words to matches. If adding more than one word, then this represents an
		/// optional word/phrase, which results in doubling the number of matches for the
		/// current phrase.
		/// </summary>
		/// <param name="keyTerm">The key term.</param>
		/// <param name="words">The words to append to the matches' word lists.</param>
		/// <param name="startOfListForPhrase">The index of the position in m_list that
		/// corresponds to the start of the matches relevant to the current phrase.</param>
		/// ------------------------------------------------------------------------------------
		private void AddWordsToMatches(IKeyTerm keyTerm, List<Word> words, int startOfListForPhrase)
		{
			int originalCount = m_list.Count;
			if (words.Count > 1)
			{
				// Spawn a new copy of each matching phrase for this metaword.
				m_list.AddRange(m_list.Skip(startOfListForPhrase).Select(k => new KeyTermMatch(k.Words, keyTerm, m_fMatchForRefOnly)).ToList());
			}

			Word word = words[0];
			for (int index = (word == null || m_fInOptionalPhrase) ? originalCount : startOfListForPhrase; index < m_list.Count; index++)
			{
				if (m_fInOptionalPhrase)
					m_list[index].AddWords(words);
				else
				{
					if (index == originalCount)
						word = words[1];
					m_list[index].AddWord(word);
				}
			}
			m_fInOptionalPhrase = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the matches.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IEnumerable<KeyTermMatch> Matches
		{
			get { return m_list; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets all the possible surface forms represented by this metaword (which could have
		/// an optional part, indicated by parentheses). If this is a completely optional word,
		/// this will include a null. If it is part of an optional phrase, it will return an
		/// empty list until it gets to the last word in the phrase, at which point it returns
		/// a list representing the whole phrase.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private List<Word> AllWords(Word metaWord, bool firstWordOfPhrase)
		{
			List<Word> list = new List<Word>();
			int iOpenParen = (m_fInOptionalPhrase) ? 0 : metaWord.Text.IndexOf('(');
			if (iOpenParen >= 0)
			{
				int iCloseParen = metaWord.Text.IndexOf(')', iOpenParen);
				if (iCloseParen > iOpenParen)
				{
					if (m_fInOptionalPhrase)
					{
						list = m_optionalPhraseWords;
						list.Add(metaWord.Text.Remove(iCloseParen));
						m_optionalPhraseWords = null;
					}
					else
					{
						string opt = metaWord.Text.Remove(iOpenParen, iCloseParen - iOpenParen + 1);
						list.Add(opt == string.Empty ? null : opt);
						list.Add(metaWord.Text.Remove(iCloseParen, 1).Remove(iOpenParen, 1));
					}
				}
				else if (m_fInOptionalPhrase)
				{
					m_optionalPhraseWords.Add(metaWord);
				}
				else if (iOpenParen == 0)
				{
					m_optionalPhraseWords = new List<Word>();
					m_optionalPhraseWords.Add(metaWord.Text.Remove(0, 1));
					m_fInOptionalPhrase = true;
				}
				else
				{
					Debug.Fail("Found opening parenthesis with no closer");
				}
			}
			else
			{
				if (firstWordOfPhrase && metaWord == "to")
					list.Add(null);
				list.Add(metaWord);
			}

			return list;
		}
	}
}
