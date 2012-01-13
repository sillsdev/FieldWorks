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
// File: Word.cs
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SILUBS.PhraseTranslationHelper
{
	public sealed class Word
	{
		#region Data Members
		private readonly string m_sText;
		private static readonly Dictionary<string, Word> s_words = new Dictionary<string, Word>(1000);
		private static readonly Dictionary<Word, HashSet<Word>> s_inflectedWords = new Dictionary<Word, HashSet<Word>>();
		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="Word"/> class.
		/// </summary>
		/// <param name="text">The text of the word.</param>
		/// ------------------------------------------------------------------------------------
		private Word(string text)
		{
			Debug.Assert(!string.IsNullOrEmpty(text));
			m_sText = text;
			s_words[text] = this;
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the text of the word.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Text
		{
			get { return m_sText; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Enumerates all "known" words (i.e., those found in the English questions).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal static IEnumerable<string> AllWords
		{
			get { return s_words.Keys; }
		}
		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the given alternate (inflected) form of this word to the collection of words
		/// that will be considered as equivalent words.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void AddAlternateForm(Word inflectedForm)
		{
			HashSet<Word> inflectedForms;
			if (!s_inflectedWords.TryGetValue(this, out inflectedForms))
				s_inflectedWords[this] = inflectedForms = new HashSet<Word>();
			inflectedForms.Add(inflectedForm);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified other word is equivalent to this word (either the
		/// same word or an inflected form of it).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsEquivalent(Word otherWord)
		{
			if (this == otherWord)
				return true;
			HashSet<Word> inflectedForms;
			return (s_inflectedWords.TryGetValue(this, out inflectedForms) && inflectedForms.Contains(otherWord));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns whether or not a Word for the specified text exists.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool Exists(string word)
		{
			return s_words.ContainsKey(word);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The text of this word.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			 return m_sText;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs an implicit conversion from <see cref="SILUBS.PhraseTranslationHelper.Word"/>
		/// to <see cref="System.String"/>.
		/// </summary>
		/// <param name="word">The word.</param>
		/// <returns>The result of the conversion.</returns>
		/// ------------------------------------------------------------------------------------
		public static implicit operator string(Word word)
		{
			return word == null ? null : word.m_sText;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs an implicit conversion from <see cref="SILUBS.PhraseTranslationHelper.Word"/>
		/// to <see cref="System.String"/>.
		/// </summary>
		/// <param name="text">The word.</param>
		/// <returns>The result of the conversion.</returns>
		/// ------------------------------------------------------------------------------------
		public static implicit operator Word(string text)
		{
			if (string.IsNullOrEmpty(text))
				return null;
			Word word;
			if (s_words.TryGetValue(text, out word))
				return word;
			return new Word(text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the Word representing the last (space-delimited) word in the text. Text is
		/// assumed to be a trimmed, punctuation-free string (and probably lowercase).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Word LastWord(string text)
		{
			int ich;
			for (ich = text.Length - 1; ich > 0; ich--)
			{
				if (text[ich] == ' ')
				{
					ich++;
					break;
				}
			}
			return text.Substring(ich);
		}
		#endregion
	}
}
