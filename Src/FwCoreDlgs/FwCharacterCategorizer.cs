// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// FwCharacterCategorizer categorizes characters based on the ICU and user overrides of
	/// ICU for a particular writing system.
	/// </summary>
	public class FwCharacterCategorizer : CharacterCategorizer
	{
		#region Member variables
		/// <summary>valid characters (used to determine word-forming characters)</summary>
		private readonly ValidCharacters m_validChars;
		#endregion

		/// <summary />
		/// <param name="validChars">The valid characters. If null, will fall back on the
		/// specified character property engine.</param>
		public FwCharacterCategorizer(ValidCharacters validChars)
		{
			m_validChars = validChars;
		}

		/// <summary>
		/// Determines whether the specified character is lower.
		/// </summary>
		/// <param name="ch">The given character.</param>
		/// <returns>
		/// 	<c>true</c> if the specified character is lower; otherwise, <c>false</c>.
		/// </returns>
		public override bool IsLower(char ch)
		{
			return Icu.Character.GetCharType(ch) == Icu.Character.UCharCategory.LOWERCASE_LETTER;
		}

		/// <summary>
		/// Determines whether the specified character is upper.
		/// </summary>
		/// <param name="ch">The given character.</param>
		/// <returns>
		/// 	<c>true</c> if the specified character is upper; otherwise, <c>false</c>.
		/// </returns>
		public override bool IsUpper(char ch)
		{
			return Icu.Character.GetCharType(ch) == Icu.Character.UCharCategory.UPPERCASE_LETTER;
		}

		/// <summary>
		/// Determines whether the specified cc is diacritic.
		/// </summary>
		/// <param name="cc">The cc.</param>
		/// <returns>
		/// 	<c>true</c> if the specified cc is diacritic; otherwise, <c>false</c>.
		/// </returns>
		public override bool IsDiacritic(char cc)
		{
			return Icu.Character.IsDiacritic(cc);
		}

		/// <summary>
		/// Determines whether the specified character is punctuation.
		/// </summary>
		/// <param name="cc">The specified character.</param>
		/// <returns>
		/// 	<c>true</c> if the specified character is punctuation; otherwise, <c>false</c>.
		/// </returns>
		public override bool IsPunctuation(char cc)
		{
			return Icu.Character.IsPunct(cc);
		}

		/// <summary>
		/// Determines whether the specified character is title.
		/// </summary>
		/// <param name="ch">The specified character.</param>
		/// <returns>
		/// 	<c>true</c> if the specified character is title; otherwise, <c>false</c>.
		/// </returns>
		public override bool IsTitle(char ch)
		{
			return Icu.Character.GetCharType(ch) == Icu.Character.UCharCategory.TITLECASE_LETTER;
		}

		/// <summary>
		/// Determines whether a character is a word forming character.
		/// </summary>
		/// <param name="cc">The specified character.</param>
		/// <returns>
		/// 	<c>true</c> if the character is a word forming character; otherwise, <c>false</c>.
		/// </returns>
		public override bool IsWordFormingCharacter(char cc)
		{
			return m_validChars != null ? m_validChars.IsWordForming(cc) : TsStringUtils.IsWordForming(cc);
		}

		/// <summary>
		/// Determines whether a character is word medial punctuation.
		/// </summary>
		/// <param name="cc">The character.</param>
		/// <returns>
		/// 	<c>true</c> if the character is a word medial punctuation character;
		/// 	otherwise, <c>false</c>.
		/// </returns>
		public override bool IsWordMedialPunctuation(char cc)
		{
			// Be careful to make sure that zwnj and zwj are included here for
			// indic scripts since they should not break words.
			return Icu.Character.GetCharType(cc) == Icu.Character.UCharCategory.CONNECTOR_PUNCTUATION;
		}

		/// <summary>
		/// Gets words and punctuation from text.
		/// </summary>
		public override List<WordAndPunct> WordAndPuncts(string text)
		{
			var waps = new List<WordAndPunct>();
			for (var i = 0; i < text.Length;)
			{
				var wap = new WordAndPunct();
				// Ignore any initial separator characters
				while (i < text.Length && Icu.Character.IsSeparator(text[i]))
				{
					i++;
				}
				if (i == text.Length)
				{
					return waps;
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
					if (Icu.Character.IsNumeric(cc))
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
					if (IsWordFormingCharacter(cc) || Icu.Character.IsNumeric(cc))
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
