// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FWCharacterCategorizer.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.FDO.DomainServices
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// FwCharacterCategorizer categorizes characters based on the ICU and user overrides of
	/// ICU for a particular writing system.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FwCharacterCategorizer : CharacterCategorizer
	{
		#region Member variables

		/// <summary>character property engine</summary>
		private ILgCharacterPropertyEngine m_charPropEngine;
		/// <summary>valid characters (used to determine word-forming characters)</summary>
		private ValidCharacters m_validChars;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FwCharacterCategorizer"/> class.
		/// </summary>
		/// <param name="validChars">The valid characters. If null, will fall back on the
		/// specified character property engine.</param>
		/// <param name="charPropEngine">The character property engine.</param>
		/// ------------------------------------------------------------------------------------
		public FwCharacterCategorizer(ValidCharacters validChars,
			ILgCharacterPropertyEngine charPropEngine)
		{
			if (charPropEngine == null)
				throw new ArgumentNullException("charPropEngine");

			m_validChars = validChars;
			m_charPropEngine = charPropEngine;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified character is lower.
		/// </summary>
		/// <param name="ch">The given character.</param>
		/// <returns>
		/// 	<c>true</c> if the specified character is lower; otherwise, <c>false</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override bool IsLower(char ch)
		{
			return m_charPropEngine.get_IsLower(ch);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified character is upper.
		/// </summary>
		/// <param name="ch">The given character.</param>
		/// <returns>
		/// 	<c>true</c> if the specified character is upper; otherwise, <c>false</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override bool IsUpper(char ch)
		{
			return m_charPropEngine.get_IsUpper(ch);
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
		public override bool IsDiacritic(char cc)
		{
			LgGeneralCharCategory cat = m_charPropEngine.get_GeneralCategory(cc);
			return (cat == LgGeneralCharCategory.kccMc || cat == LgGeneralCharCategory.kccMn);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified character is punctuation.
		/// </summary>
		/// <param name="cc">The specified character.</param>
		/// <returns>
		/// 	<c>true</c> if the specified character is punctuation; otherwise, <c>false</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override bool IsPunctuation(char cc)
		{
			return m_charPropEngine.get_IsPunctuation(cc);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified character is title.
		/// </summary>
		/// <param name="ch">The specified character.</param>
		/// <returns>
		/// 	<c>true</c> if the specified character is title; otherwise, <c>false</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override bool IsTitle(char ch)
		{
			return m_charPropEngine.get_IsTitle(ch);
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
		public override bool IsWordFormingCharacter(char cc)
		{
			return m_validChars != null ? m_validChars.IsWordForming(cc) : m_charPropEngine.get_IsWordForming(cc);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether a character is word medial punctuation.
		/// </summary>
		/// <param name="cc">The character.</param>
		/// <returns>
		/// 	<c>true</c> if the character is a word medial punctuation character;
		/// 	otherwise, <c>false</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override bool IsWordMedialPunctuation(char cc)
		{
			// Be careful to make sure that zwnj and zwj are included here for
			// indic scripts since they should not break words.
			return m_charPropEngine.get_IsWordMedial(cc);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets words and punctuation from text.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <returns>a collection of words and punctuation</returns>
		/// ------------------------------------------------------------------------------------
		public override List<WordAndPunct> WordAndPuncts(string text)
		{
			char cc;
			int punctOffset;
			List<WordAndPunct> waps = new List<WordAndPunct>();

			for (int i = 0; i < text.Length; )
			{
				WordAndPunct wap = new WordAndPunct();

				// Ignore any initial separator characters
				while (i < text.Length && m_charPropEngine.get_IsSeparator(text[i]))
					i++;

				if (i == text.Length)
					return waps;

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
					else if (m_charPropEngine.get_IsNumber(cc))
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
					if (IsWordFormingCharacter(cc) || m_charPropEngine.get_IsNumber(cc))
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
