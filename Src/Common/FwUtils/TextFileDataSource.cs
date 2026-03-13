// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Globalization;
using SIL.LCModel.Core.Scripture;

namespace SIL.FieldWorks.Common.FwUtils
{
	#region class TextFileDataSource
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// A class representing a file that can be parsed to find characters
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TextFileDataSource
	{
		private CharacterCategorizer m_characterCategorizer;
		private List<ITextToken> m_tftList;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TextFileDataSource"/> class.
		/// </summary>
		/// <param name="fileData">An array of strings with the lines of data from the file.
		/// </param>
		/// <param name="scrRefFormatString">Format string used to format scripture references.
		/// </param>
		/// <param name="categorizer">The character categorizer.</param>
		/// ------------------------------------------------------------------------------------
		public TextFileDataSource(string[] fileData, string scrRefFormatString,
			CharacterCategorizer categorizer)
		{
			m_characterCategorizer = categorizer ?? new CharacterCategorizer();
			m_tftList = new List<ITextToken>();
			int i = 1;
			foreach (string line in fileData)
				m_tftList.Add(new TextFileToken(line, i++, scrRefFormatString));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets character sequence references from all tokens.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<TextTokenSubstring> GetReferences()
		{
			var results = new List<TextTokenSubstring>();
			foreach (ITextToken tok in m_tftList)
			{
				if (tok.Text == null)
					continue;
				int offset = 0;
				foreach (string key in ParseCharacterSequences(tok.Text, m_characterCategorizer))
				{
					results.Add(new TextTokenSubstring(tok, offset, key.Length));
					offset += key.Length;
				}
			}
			return results;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Parses a string into character sequences, grouping base characters with their
		/// combining diacritics. Handles surrogate pairs correctly.
		/// </summary>
		/// <param name="text">The text to parse.</param>
		/// <param name="categorizer">The character categorizer.</param>
		/// <returns>An enumeration of character sequences.</returns>
		/// ------------------------------------------------------------------------------------
		internal static IEnumerable<string> ParseCharacterSequences(string text,
			CharacterCategorizer categorizer)
		{
			if (string.IsNullOrEmpty(text))
				yield break;

			var enumerator = StringInfo.GetTextElementEnumerator(text);
			string key = "";
			bool diacriticsFollow = categorizer.DiacriticsFollowBaseCharacters();

			while (enumerator.MoveNext())
			{
				string element = enumerator.GetTextElement();
				// Only single BMP chars can be diacritics (combining marks are all in the BMP)
				bool isDiacritic = element.Length == 1 && categorizer.IsDiacritic(element[0]);

				if (isDiacritic && diacriticsFollow)
				{
					key += element;
				}
				else
				{
					if (key.Length > 0)
						yield return key;
					key = element;
				}
			}

			if (key.Length > 0)
				yield return key;
		}
	}

	#endregion

	#region TextFileToken class
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Text token object used for reading any, nondescript text file in order to discover
	/// all the characters therein.
	/// </summary>
	/// ------------------------------------------------------------------------------------
	internal class TextFileToken : ITextToken
	{
		private string m_text;
		private int m_iLine;
		private string m_scrRefFmtString;

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TextFileToken"/> class.
		/// </summary>
		/// --------------------------------------------------------------------------------
		internal TextFileToken(string text, int iLine, string scrRefFormatString)
		{
			m_text = text;
			m_iLine = iLine;
			m_scrRefFmtString = scrRefFormatString;
		}

		#region ITextToken Members
		/// <summary>Not used.</summary>
		public bool IsNoteStart
		{
			get { return false; }
		}

		/// <summary>Not used.</summary>
		public bool IsParagraphStart
		{
			get { return true; }
		}

		/// <summary>Not used.</summary>
		public string Locale
		{
			get { return null; }
		}

		/// <summary>Gets the scripture reference string.</summary>
		public string ScrRefString
		{
			get { return string.Format(m_scrRefFmtString, m_iLine); }
			set { ; }
		}

		/// <summary>Not used.</summary>
		public string ParaStyleName
		{
			get { return null; }
		}

		/// <summary>Not used.</summary>
		public string CharStyleName
		{
			get { return null; }
		}

		/// <summary>Gets the text.</summary>
		public string Text
		{
			get { return m_text; }
		}

		/// <summary>Force the check to treat the text like verse text.</summary>
		public TextType TextType
		{
			get { return TextType.Verse; }
		}

		/// <summary/>
		public BCVRef MissingEndRef
		{
			get { return null; }
			set { ; }
		}

		/// <summary/>
		public BCVRef MissingStartRef
		{
			get { return null; }
			set { ; }
		}

		/// <summary>Makes a deep copy of the specified text token.</summary>
		public ITextToken Clone()
		{
			return new TextFileToken(m_text, m_iLine, m_scrRefFmtString);
		}
		#endregion
	}

	#endregion
}
