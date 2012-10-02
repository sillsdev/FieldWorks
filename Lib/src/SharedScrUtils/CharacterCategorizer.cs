using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.Diagnostics;

namespace SILUBS.SharedScrUtils
{
	/// <summary>
	/// (optional) key string followed by (optional) punctuation/whitespace string.
	/// </summary>
	public struct WordAndPunct
	{
		public string Word;
		public string Punct;
		public int Offset;

		public override string ToString()
		{
			return Word + "/" + Punct + "/" + Offset.ToString();
		}
	}

	/// <summary>
	///
	/// </summary>
	public class CharacterCategorizer
	{
		//! What should be done about PUA characters?

		string wordMedialCharacters = "-\u200c\u200d";
		// zwj and zwnj should not normally break words

		// Characters which are considered to be key forming in this
		// project but are not considered so in Unicode.
		// Do not include key medial punctuation in this list
		string extraWordFormingCharacters = "";

		// Characters which are considered to be diacritics in this
		// project but are not considered so in Unicode.
		string extraDiacriticCharacters = "";

		string wordFormingCharacters = "";
		//string punctuationCharacters = "";
		string diacriticCharacters = "";
		string upperCaseCharacters = "";
		string lowerCaseCharacters = "";
		//! should there be a list of digits?

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Use this constructor to default to Unicode character semantics.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public CharacterCategorizer()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This constructor allows overriding Unicode character semantics for use with hacked
		/// fonts.
		/// </summary>
		/// <param name="_wordFormingCharacters">The _word forming characters.</param>
		/// <param name="wordMedialPunct">The word medial punct.</param>
		/// <param name="_diacriticCharacters">The _diacritic characters.</param>
		/// <param name="_upperCaseCharacters">The _upper case characters.</param>
		/// <param name="_lowerCaseCharacters">The _lower case characters.</param>
		/// ------------------------------------------------------------------------------------
		public CharacterCategorizer(string _wordFormingCharacters, string wordMedialPunct,
			string _diacriticCharacters, string _upperCaseCharacters, string _lowerCaseCharacters)
		{
			wordFormingCharacters = _wordFormingCharacters;
			diacriticCharacters = _diacriticCharacters;
			//punctuationCharacters = _punctuationCharacters;
			upperCaseCharacters = _upperCaseCharacters;
			lowerCaseCharacters = _lowerCaseCharacters;

			// Save any characters which have been defined to be key forming
			// for this project but are not considered so in Unicode.
			extraWordFormingCharacters = "";
			foreach (char cc in wordFormingCharacters)
			{
				if (char.IsWhiteSpace(cc))
					continue;
				UnicodeCategory cat = char.GetUnicodeCategory(cc);
				if (cat <= UnicodeCategory.SpacingCombiningMark)
					continue;

				extraWordFormingCharacters += cc;
			}

			// Save any characters which are considered to be diacritics in this
			// project but are not considered so in Unicode.
			extraDiacriticCharacters = "";
			foreach (char cc in diacriticCharacters)
			{
				if (char.IsWhiteSpace(cc))
					continue;
				UnicodeCategory cat = char.GetUnicodeCategory(cc);
				if (cat == UnicodeCategory.SpacingCombiningMark ||
					cat == UnicodeCategory.NonSpacingMark)
					continue;

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


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified character is a single character word.
		/// </summary>
		/// <param name="cc">The specified character.</param>
		/// <returns>
		/// 	<c>true</c> if the character is single character word; otherwise, <c>false</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool IsSingleCharacterWord(char cc)
		{
			for (int i = 0; cc >= SingleCharacterWords[i] && SingleCharacterWords[i] != '\uffff'; i += 2)
			{
				if (cc <= SingleCharacterWords[i + 1])
					return true;
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether a character is a word forming character.
		/// </summary>
		/// <param name="cc">The specified character.</param>
		/// <returns>
		/// 	<c>true</c> if the character is a word forming character; otherwise, <c>false</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool IsWordFormingCharacter(char cc)
		{
			UnicodeCategory cat = char.GetUnicodeCategory(cc);
			if (cat <= UnicodeCategory.SpacingCombiningMark)
				return true;

			if (extraWordFormingCharacters.IndexOf(cc) >= 0)
				return true;

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether [is word medial punctuation] [the specified cc].
		/// </summary>
		/// <param name="cc">The cc.</param>
		/// <returns>
		/// 	<c>true</c> if [is word medial punctuation] [the specified cc]; otherwise, <c>false</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool IsWordMedialPunctuation(char cc)
		{
			// Be careful to make sure that zwnj and zwj are included here for
			// indic scripts since they should not break words.
			if (wordMedialCharacters.IndexOf(cc) >= 0)
				return true;

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified cc is punctuation.
		/// </summary>
		/// <param name="cc">The cc.</param>
		/// <returns>
		/// 	<c>true</c> if the specified cc is punctuation; otherwise, <c>false</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool IsPunctuation(char cc)
		{
			UnicodeCategory cat = char.GetUnicodeCategory(cc);

			if (cat >= UnicodeCategory.ConnectorPunctuation &&
				cat <= UnicodeCategory.OtherSymbol)
				return true;

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified cc is diacritic.
		/// </summary>
		/// <param name="cc">The cc.</param>
		/// <returns>
		/// 	<c>true</c> if the specified cc is diacritic; otherwise, <c>false</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool IsDiacritic(char cc)
		{
			UnicodeCategory cat = char.GetUnicodeCategory(cc);

			if (cat == UnicodeCategory.SpacingCombiningMark ||
					cat == UnicodeCategory.NonSpacingMark)
				return true;

			if (extraDiacriticCharacters.IndexOf(cc) >= 0)
				return true;

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Diacritics always follow base characters in Unicode. In hacked fonts
		/// this may not be true.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool DiacriticsFollowBaseCharacters()
		{
			//! get from language data
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the upper case characters.
		/// </summary>
		/// <value>The upper case characters.</value>
		/// ------------------------------------------------------------------------------------
		public virtual string UpperCaseCharacters { get { return upperCaseCharacters; } }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the lower case characters.
		/// </summary>
		/// <value>The lower case characters.</value>
		/// ------------------------------------------------------------------------------------
		public virtual string LowerCaseCharacters { get { return lowerCaseCharacters; } }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified ch is upper.
		/// </summary>
		/// <param name="ch">The ch.</param>
		/// <returns>
		/// 	<c>true</c> if the specified ch is upper; otherwise, <c>false</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool IsUpper(char ch)
		{
			foreach (char x in upperCaseCharacters)
				if (ch == x)
					return true;

			bool result = char.IsUpper(ch);
			return result;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified ch is lower.
		/// </summary>
		/// <param name="ch">The ch.</param>
		/// <returns>
		/// 	<c>true</c> if the specified ch is lower; otherwise, <c>false</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool IsLower(char ch)
		{
			foreach (char x in lowerCaseCharacters)
				if (ch == x)
					return true;

			return char.IsLower(ch);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified ch is title.
		/// </summary>
		/// <param name="ch">The ch.</param>
		/// <returns>
		/// 	<c>true</c> if the specified ch is title; otherwise, <c>false</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool IsTitle(char ch)
		{
			//! should there be a list of title case letters?
			//! should there be a list of no case letters? Non-roman letters do not have case.
			UnicodeCategory cat = char.GetUnicodeCategory(ch);
			return cat == UnicodeCategory.TitlecaseLetter;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Toes the lower.
		/// </summary>
		/// <param name="str">The STR.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual string ToLower(string str)
		{
			char[] parts = str.ToCharArray();

			bool foundUpper = false;

			for (int i = 0; i <= parts.GetUpperBound(0); ++i)
				if (IsUpper(parts[i]))
				{
					foundUpper = true;
					parts[i] = ToLower(parts[i]);
				}

			if (foundUpper)
				return new string(parts);

			return str;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Toes the upper.
		/// </summary>
		/// <param name="str">The STR.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual string ToUpper(string str)
		{
			char[] parts = str.ToCharArray();

			bool foundLower = false;

			for (int i = 0; i < parts.GetUpperBound(0); ++i)
				if (IsLower(parts[i]))
				{
					foundLower = true;
					parts[i] = ToUpper(parts[i]);
				}

			if (foundLower)
				return new string(parts);

			return str;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Toes the lower.
		/// </summary>
		/// <param name="ch">The ch.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual char ToLower(char ch)
		{
			for (int i = 0; i < upperCaseCharacters.Length; ++i)
			{
				if (ch == upperCaseCharacters[i])
					return lowerCaseCharacters[i];
			}

			return char.ToLower(ch);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Toes the upper.
		/// </summary>
		/// <param name="ch">The ch.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual char ToUpper(char ch)
		{
			for (int i = 0; i < lowerCaseCharacters.Length; ++i)
			{
				if (ch == lowerCaseCharacters[i])
					return upperCaseCharacters[i];
			}

			return char.ToUpper(ch);
		}

		public virtual string WordFormingCharacters { get { return wordFormingCharacters; } }

		public string DiacriticCharacters { get { return diacriticCharacters; } }

		//public string PunctuationCharacters { get { return punctuationCharacters; } }

		public virtual List<WordAndPunct> WordAndPuncts(string text)
		{
			char cc;
			int punctOffset;
			List<WordAndPunct> waps = new List<WordAndPunct>();

			for (int i = 0; i < text.Length; )
			{
				WordAndPunct wap = new WordAndPunct();

				// Ignore any initial separator characters
				while (i < text.Length && char.IsSeparator(text[i]))
					i++;
				wap.Offset = i;
				bool isFirstCharacterInWord = true;

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
					else if (IsWordMedialPunctuation(cc))
					{
						// Word medial punctuation only counts if this is not the first
						// character in a key AND the next character is key forming.
						//! can we have multiple key medial punctuation?
						if (isFirstCharacterInWord)
							break;
						if (i + 1 >= text.Length || !IsWordFormingCharacter(text[i + 1]))
							break;
					}
					else if (char.IsDigit(cc))
					{
						// allow digits in words
					}
					else if (!IsWordFormingCharacter(cc))
						break;

					i = i + 1;
					isFirstCharacterInWord = false;
				}

				wap.Word = text.Substring(wap.Offset, i - wap.Offset);

				punctOffset = i;

				while (i < text.Length)
				{
					cc = text[i];
					if (IsWordFormingCharacter(cc) || char.IsDigit(cc))
						break;
					i = i + 1;
				}
				wap.Punct = text.Substring(punctOffset, i - punctOffset);

				waps.Add(wap);
			}

			return waps;
		}
	}
}
