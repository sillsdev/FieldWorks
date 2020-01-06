// Copyright (c) 2012-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Globalization;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary />
	public class CharacterCategorizer
	{
		//! What should be done about PUA characters?

		private const string wordMedialCharacters = "-\u200c\u200d";
		// zwj and zwnj should not normally break words

		// Characters which are considered to be key forming in this
		// project but are not considered so in Unicode.
		// Do not include key medial punctuation in this list
		string extraWordFormingCharacters = string.Empty;

		// Characters which are considered to be diacritics in this
		// project but are not considered so in Unicode.
		string extraDiacriticCharacters = string.Empty;

		string wordFormingCharacters = string.Empty;

		//! should there be a list of digits?

		/// <summary>
		/// Use this constructor to default to Unicode character semantics.
		/// </summary>
		public CharacterCategorizer()
		{
		}

		/// <summary>
		/// This constructor allows overriding Unicode character semantics for use with hacked
		/// fonts.
		/// </summary>
		public CharacterCategorizer(string wordFormingCharacters, string diacriticCharacters)
		{
			this.wordFormingCharacters = wordFormingCharacters;
			DiacriticCharacters = diacriticCharacters;

			// Save any characters which have been defined to be key forming
			// for this project but are not considered so in Unicode.
			extraWordFormingCharacters = string.Empty;
			foreach (var cc in this.wordFormingCharacters)
			{
				if (char.IsWhiteSpace(cc))
				{
					continue;
				}
				var cat = char.GetUnicodeCategory(cc);
				if (cat <= UnicodeCategory.SpacingCombiningMark)
				{
					continue;
				}
				extraWordFormingCharacters += cc;
			}

			// Save any characters which are considered to be diacritics in this
			// project but are not considered so in Unicode.
			extraDiacriticCharacters = string.Empty;
			foreach (var cc in DiacriticCharacters)
			{
				if (char.IsWhiteSpace(cc))
				{
					continue;
				}
				var cat = char.GetUnicodeCategory(cc);
				if (cat == UnicodeCategory.SpacingCombiningMark || cat == UnicodeCategory.NonSpacingMark)
				{
					continue;
				}
				extraDiacriticCharacters += cc;
			}
		}

		// Characters in these ranges form a single character "key".
		// These are (primarily?) ideograms.
		public static char[] SingleCharacterWords = {
				'\u2e80', '\u2fd0',
				'\u3004', '\u3006',
				'\u3012', '\u3013',
				'\u3020', '\u302f',
				'\u3031', '\u303e',
				'\u3040', '\u31b7',
				'\u31f0', '\u9ff0',
				'\uf900', '\ufaf0',
				'\ufe30', '\ufe40',
				'\uffff', '\uffff' };

		/// <summary>
		/// Determines whether the specified character is a single character word.
		/// </summary>
		public virtual bool IsSingleCharacterWord(char cc)
		{
			for (var i = 0; cc >= SingleCharacterWords[i] && SingleCharacterWords[i] != '\uffff'; i += 2)
			{
				if (cc <= SingleCharacterWords[i + 1])
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Determines whether a character is a word forming character.
		/// </summary>
		public virtual bool IsWordFormingCharacter(char cc)
		{
			return char.GetUnicodeCategory(cc) <= UnicodeCategory.SpacingCombiningMark || extraWordFormingCharacters.IndexOf(cc) >= 0;
		}

		/// <summary>
		/// Determines whether [is word medial punctuation] [the specified cc].
		/// </summary>
		public virtual bool IsWordMedialPunctuation(char cc)
		{
			// Be careful to make sure that zwnj and zwj are included here for
			// indic scripts since they should not break words.
			return wordMedialCharacters.IndexOf(cc) >= 0;
		}

		/// <summary>
		/// Determines whether the specified cc is punctuation.
		/// </summary>
		public virtual bool IsPunctuation(char cc)
		{
			var cat = char.GetUnicodeCategory(cc);
			return cat >= UnicodeCategory.ConnectorPunctuation && cat <= UnicodeCategory.OtherSymbol;
		}

		/// <summary>
		/// Determines whether the specified cc is diacritic.
		/// </summary>
		public virtual bool IsDiacritic(char cc)
		{
			var cat = char.GetUnicodeCategory(cc);
			return cat == UnicodeCategory.SpacingCombiningMark || cat == UnicodeCategory.NonSpacingMark || extraDiacriticCharacters.IndexOf(cc) >= 0;
		}

		/// <summary>
		/// Diacritics always follow base characters in Unicode. In hacked fonts
		/// this may not be true.
		/// </summary>
		public virtual bool DiacriticsFollowBaseCharacters()
		{
			//! get from language data
			return true;
		}

		/// <summary>
		/// Determines whether the specified ch is upper.
		/// </summary>
		public virtual bool IsUpper(char ch)
		{
			return char.IsUpper(ch);
		}

		/// <summary>
		/// Determines whether the specified ch is lower.
		/// </summary>
		public virtual bool IsLower(char ch)
		{
			return char.IsLower(ch);
		}

		/// <summary>
		/// Determines whether the specified ch is title.
		/// </summary>
		public virtual bool IsTitle(char ch)
		{
			//! should there be a list of title case letters?
			//! should there be a list of no case letters? Non-roman letters do not have case.
			return char.GetUnicodeCategory(ch) == UnicodeCategory.TitlecaseLetter;
		}

		/// <summary>
		/// Toes the lower.
		/// </summary>
		public virtual string ToLower(string str)
		{
			return str.ToLower();
		}

		/// <summary>
		/// Toes the upper.
		/// </summary>
		public virtual string ToUpper(string str)
		{
			return str.ToUpper();
		}

		public virtual string WordFormingCharacters => wordFormingCharacters;

		public string DiacriticCharacters { get; } = string.Empty;

		public virtual List<WordAndPunct> WordAndPuncts(string text)
		{
			var waps = new List<WordAndPunct>();
			for (var i = 0; i < text.Length;)
			{
				var wap = new WordAndPunct();
				// Ignore any initial separator characters
				while (i < text.Length && char.IsSeparator(text[i]))
				{
					i++;
				}
				wap.Offset = i;
				var isFirstCharacterInWord = true;
				char cc;
				while (i < text.Length)
				{
					cc = text[i];
					if (IsSingleCharacterWord(cc))
					{
						if (isFirstCharacterInWord)
						{
							// Single Character key is the first character in the key.
							// It forms a key all by itself.
							i = i + 1;
						}
						else
						{
							// Single Character key is NOT the first character in the key.
							// It ends the key currently being formed.
							// 'i' is not incremented
						}
						break;
					}
					if (IsWordMedialPunctuation(cc))
					{
						// Word medial punctuation only counts if this is not the first
						// character in a key AND the next character is key forming.
						//! can we have multiple key medial punctuation?
						if (isFirstCharacterInWord)
						{
							break;
						}
						if (i + 1 >= text.Length || !IsWordFormingCharacter(text[i + 1]))
						{
							break;
						}
					}
					else if (char.IsDigit(cc))
					{
						// allow digits in words
					}
					else if (!IsWordFormingCharacter(cc))
					{
						break;
					}
					i = i + 1;
					isFirstCharacterInWord = false;
				}

				wap.Word = text.Substring(wap.Offset, i - wap.Offset);

				var punctOffset = i;

				while (i < text.Length)
				{
					cc = text[i];
					if (IsWordFormingCharacter(cc) || char.IsDigit(cc))
					{
						break;
					}
					i = i + 1;
				}
				wap.Punct = text.Substring(punctOffset, i - punctOffset);

				waps.Add(wap);
			}

			return waps;
		}
	}
}