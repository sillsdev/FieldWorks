using System;
using System.Collections.Generic;
using System.Text;
using SILUBS.SharedScrUtils;
using System.Xml;
using System.Diagnostics;

namespace SILUBS.ScriptureChecks
{
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Check for capitalization: styles that should begin with a capital letter and
	/// words after sentence-final punctuation.
	/// </summary>
	/// ------------------------------------------------------------------------------------
	public class CapitalizationCheck : IScriptureCheck
	{
		#region Member variables
		/// <summary>provides Scripture data to this check</summary>
		IChecksDataSource m_chkDataSource;

		/// <summary>capitalization errors detected in this check</summary>
		List<TextTokenSubstring> m_capitalizationErrors;

		/// <summary>name of parameter to provide serialized style information for this check.</summary>
		readonly string kStyleSheetInfoParameter = "StylesInfo";
		/// <summary>name of parameter to provide punctuation that occurs sentence-finally</summary>
		readonly string kSentenceFinalPuncParameter = "SentenceFinalPunctuation";

		private StylePropsInfo m_stylePropsInfo;
		/// <summary>Dictionary keyed by the style name containing the type of style (character/paragraph)
		/// and a value indicating why it should begin with a capital.</summary>
		private Dictionary<string, StyleCapInfo> m_allCapitalizedStyles = new Dictionary<string,StyleCapInfo>();

		/// <summary>string containing punctuation that ends sentences.</summary>
		string m_SentenceFinalPunc = null;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="CapitalizationCheck"/> class.
		/// </summary>
		/// <param name="_checksDataSource">The data source for the check.</param>
		/// ------------------------------------------------------------------------------------
		public CapitalizationCheck(IChecksDataSource _checksDataSource)
		{

			m_chkDataSource = _checksDataSource;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a localized version of the specified string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private string Localize(string strToLocalize)
		{
			return m_chkDataSource.GetLocalizedString(strToLocalize);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the check.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string CheckName { get { return Localize("Capitalization"); } }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The unique identifier of the check. This should never be changed!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Guid CheckId { get { return StandardCheckIds.kguidCapitalization; } }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the group which contains this check.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string CheckGroup { get { return Localize("Basic"); } }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a number that can be used to order this check relative to other checks in the
		/// same group when displaying checks in the UI.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public float RelativeOrder
		{
			get { return 600; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the description for this check.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Description { get { return Localize("Checks for potential inconsistencies in capitalization."); } }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is the column header of the first column when you create an
		/// inventory of this type of error.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string InventoryColumnHeader
		{
			get { return Localize("Style name or preceding punctuation."); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks the specified Scripture tokens for capitalization within styles.
		/// </summary>
		/// <param name="toks">The tokens from scripture.</param>
		/// <param name="record">The record.</param>
		/// ------------------------------------------------------------------------------------
		public void Check(IEnumerable<ITextToken> toks, RecordErrorHandler record)
		{
			GetReferences(toks);

			foreach (TextTokenSubstring tts in m_capitalizationErrors)
				record(new RecordErrorEventArgs(tts, CheckId));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the references where capitalization errors occurred.
		/// </summary>
		/// <param name="tokens">The Scripture tokens.</param>
		/// <returns>list of capitalization errors.</returns>
		/// ------------------------------------------------------------------------------------
		public List<TextTokenSubstring> GetReferences(IEnumerable<ITextToken> tokens)
		{
			m_SentenceFinalPunc = m_chkDataSource.GetParameterValue(kSentenceFinalPuncParameter);
			if (m_stylePropsInfo == null)
			{
				string styleInfo = m_chkDataSource.GetParameterValue(kStyleSheetInfoParameter);
				Debug.Assert(!string.IsNullOrEmpty(styleInfo), "Style information not provided.");
				m_stylePropsInfo = StylePropsInfo.Load(styleInfo);
				CreateCapitalStyleDictionary();
				Debug.Assert(m_allCapitalizedStyles.Count > 0, "No styles require capitalization.");
			}

			CapitalizationProcessor bodyPuncProcessor = new CapitalizationProcessor(m_chkDataSource, m_allCapitalizedStyles);
			CapitalizationProcessor notePuncProcessor = new CapitalizationProcessor(m_chkDataSource, m_allCapitalizedStyles);
			notePuncProcessor.ProcessParagraphsSeparately = true;

			m_capitalizationErrors = new List<TextTokenSubstring>();
			VerseTextToken scrTok = new VerseTextToken();

			ITextToken tok;
			foreach (ITextToken token in tokens)
			{
				if (token.TextType == TextType.Note || token.TextType == TextType.PictureCaption)
					tok = token;
				else
				{
					// Make the token one of our special capitalization text tokens.
					scrTok.Token = token;
					tok = scrTok;
				}

				if (tok.TextType == TextType.Note)
					notePuncProcessor.ProcessToken(tok, m_capitalizationErrors);
				else if (tok.TextType == TextType.Verse || tok.TextType == TextType.Other)
					bodyPuncProcessor.ProcessToken(tok, m_capitalizationErrors);
			}

			return m_capitalizationErrors;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the dictionary of styles information that will be used in this check.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CreateCapitalStyleDictionary()
		{
			AddListToDictionary(m_stylePropsInfo.SentenceInitial, StyleCapInfo.CapCheckTypes.SentenceInitial);
			AddListToDictionary(m_stylePropsInfo.ProperNouns, StyleCapInfo.CapCheckTypes.ProperNoun);
			AddListToDictionary(m_stylePropsInfo.Table, StyleCapInfo.CapCheckTypes.Table);
			AddListToDictionary(m_stylePropsInfo.List, StyleCapInfo.CapCheckTypes.List);
			AddListToDictionary(m_stylePropsInfo.Special, StyleCapInfo.CapCheckTypes.Special);
			AddListToDictionary(m_stylePropsInfo.Heading, StyleCapInfo.CapCheckTypes.Heading);
			AddListToDictionary(m_stylePropsInfo.Title, StyleCapInfo.CapCheckTypes.Title);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the list of styles to dictionary.
		/// </summary>
		/// <param name="list">The list of style info (name and type of style).</param>
		/// <param name="capType">The reason this style should begin with a capital letter.</param>
		/// ------------------------------------------------------------------------------------
		private void AddListToDictionary(List<StyleInfo> list, StyleCapInfo.CapCheckTypes capType)
		{
			foreach (StyleInfo styleInfo in list)
			{
				m_allCapitalizedStyles.Add(styleInfo.StyleName.Replace("_", " "),
					new StyleCapInfo(styleInfo.StyleType, capType));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the error message given the style's reason for capitalization.
		/// </summary>
		/// <param name="dataSource">The data source.</param>
		/// <param name="capReasonType">Reason why a character should have been capitalized.</param>
		/// <param name="styleName">Name of the style or string.Empty if not relevant.</param>
		/// <returns>error message.</returns>
		/// ------------------------------------------------------------------------------------
		internal static string GetErrorMessage(IChecksDataSource dataSource,
			StyleCapInfo.CapCheckTypes capReasonType, string styleName)
		{
			switch (capReasonType)
			{
				case StyleCapInfo.CapCheckTypes.SentenceInitial:
					return dataSource.GetLocalizedString("Sentence should begin with a capital letter");
				case StyleCapInfo.CapCheckTypes.Heading:
					return dataSource.GetLocalizedString("Heading should begin with a capital letter");
				case StyleCapInfo.CapCheckTypes.Title:
					return dataSource.GetLocalizedString("Title should begin with a capital letter");
				case StyleCapInfo.CapCheckTypes.List:
					return dataSource.GetLocalizedString("List paragraphs should begin with a capital letter");
				case StyleCapInfo.CapCheckTypes.Table:
					return dataSource.GetLocalizedString("Table contents should begin with a capital letter");
				case StyleCapInfo.CapCheckTypes.ProperNoun:
					return dataSource.GetLocalizedString("Proper nouns should begin with a capital letter");
				case StyleCapInfo.CapCheckTypes.Special:
					return String.Format(dataSource.GetLocalizedString(
						"Text in the {0} style should begin with a capital letter"), styleName);
			}

			throw new Exception("Reason for capitalizing the style " + styleName +
				" is not handled in GetErrorMessage (" + capReasonType.ToString() + ")");
		}
	}

	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Information about a style that is useful for capitalization checking.
	/// </summary>
	/// ------------------------------------------------------------------------------------
	public class StyleCapInfo
	{
		/// <summary>categories of styles that should be capitalized</summary>
		public enum CapCheckTypes
		{
			/// <summary>styles used sentence initially.</summary>
			SentenceInitial,
			/// <summary>styles used for proper nouns.</summary>
			ProperNoun,
			/// <summary>styles used in a table.</summary>
			Table,
			/// <summary>styles used in a list.</summary>
			List,
			/// <summary>styles used for special elements.</summary>
			Special,
			/// <summary>styles used in a heading.</summary>
			Heading,
			/// <summary>styles used in a title.</summary>
			Title
		}

		/// <summary>type of style either paragraph or character</summary>
		public StyleInfo.StyleTypes m_type;
		/// <summary>reason why the style is capitalized</summary>
		public CapCheckTypes m_capCheck;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="StyleCapInfo"/> class.
		/// </summary>
		/// <param name="type">The type of style (character or paragraph).</param>
		/// <param name="capCheck">The reason for the capitalization in this style.</param>
		/// ------------------------------------------------------------------------------------
		public StyleCapInfo(StyleInfo.StyleTypes type, CapCheckTypes capCheck)
		{
			m_type = type;
			m_capCheck = capCheck;
		}
	}

	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Check capitalization for styles and sentences.
	/// </summary>
	/// ------------------------------------------------------------------------------------
	public class CapitalizationProcessor
	{
		#region Data members
		/// <summary>provides Scripture data to this check.</summary>
		private IChecksDataSource m_checksDataSource;
		/// <summary>provides the category of characters, e.g. word-forming, etc.</summary>
		CharacterCategorizer m_categorizer;
		/// <summary>abbreviations relevant to this check.</summary>
		string[] m_abbreviations;
		/// <summary>valid punctuation at the end of a sentence.</summary>
		List<char> m_validSentenceFinalPuncts = new List<char>();
		private bool m_fAtSentenceStart = true;

		/// <summary>the current paragraph style.</summary>
		private string m_paragraphStyle = "";
		/// <summary>the current character style.</summary>
		private string m_characterStyle = "";

		private bool m_foundParagraphText = true;
		private bool m_foundCharacterText = true;

		private bool m_processParagraphsSeparately = false;

		/// <summary>Dictionary keyed by the style name containing the type of style (character/paragraph)
		/// and a value indicating why it should begin with a capital.</summary>
		private Dictionary<string, StyleCapInfo> m_allCapitalizedStyles = null;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ProcessSentenceFinalPunct"/> class.
		/// </summary>
		/// <param name="checksDataSource">The source of data for Scripture checking.</param>
		/// <param name="allCapitalizedStyles">Dictionary keyed by the style name containing the
		/// type of style (character/paragraph) and a value indicating why it should begin with
		/// a capital.</param>
		/// ------------------------------------------------------------------------------------
		public CapitalizationProcessor(IChecksDataSource checksDataSource,
			Dictionary<string, StyleCapInfo> allCapitalizedStyles)
		{
			m_checksDataSource = checksDataSource;
			m_categorizer = checksDataSource.CharacterCategorizer;
			m_abbreviations = checksDataSource.GetParameterValue("Abbreviations").Split();
			m_allCapitalizedStyles = allCapitalizedStyles;

			string sentenceFinalPunc = checksDataSource.GetParameterValue("SentenceFinalPunctuation");
			if (!string.IsNullOrEmpty(sentenceFinalPunc))
			{
				foreach (char ch in sentenceFinalPunc)
					m_validSentenceFinalPuncts.Add(ch);
			}
			else
			{
				// No punctuation is set up for this writing system that contains sentence-final punctuation.
				// Define sentence-final punctuation with these characters as a fallback: '.', '?', and '!'
				m_validSentenceFinalPuncts.Add('.');
				m_validSentenceFinalPuncts.Add('?');
				m_validSentenceFinalPuncts.Add('!');
			}
		}

		#endregion

		#region Public methods
		public bool ProcessParagraphsSeparately
		{
			get { return m_processParagraphsSeparately; }
			set { m_processParagraphsSeparately = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the Scripture token.
		/// </summary>
		/// <param name="tok">The token.</param>
		/// <param name="result">The result.</param>
		/// ------------------------------------------------------------------------------------
		public void ProcessToken(ITextToken tok, List<TextTokenSubstring> result)
		{
			string tokenText = RemoveAbbreviations(tok);

			RecordParagraphStyle(tok);
			RecordCharacterStyle(tok);

			// must be at least one character in token to check the case of
			if (tok.Text == String.Empty)
				return;

			for (int iChar = 0; iChar < tokenText.Length; iChar++)
			{
				char ch = tokenText[iChar];

				if (IsSentenceFinalPunctuation(ch))
				{
					m_fAtSentenceStart = iChar + 1 == tokenText.Length ||
						(iChar + 1 < tokenText.Length && !char.IsDigit(tokenText[iChar + 1]));
					continue;
				}

				if (!m_categorizer.IsWordFormingCharacter(ch))
					continue;

				if (m_categorizer.IsLower(ch))
				{
					TextTokenSubstring tts = GetSubstring(tok, iChar);

					if (!CheckForParaCapitalizationError(tok, tts, result) &&
						!CheckForCharStyleCapilizationError(tok, tts, result) &&
						m_fAtSentenceStart)
					{
						tts.Message = CapitalizationCheck.GetErrorMessage(m_checksDataSource,
						StyleCapInfo.CapCheckTypes.SentenceInitial, string.Empty);
						result.Add(tts);
					}
				}
				m_fAtSentenceStart = false;
				m_foundCharacterText = true;
				m_foundParagraphText = true;
			}
		}
		#endregion

		#region Private Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the substring for the character starting at position iChar.
		/// </summary>
		/// <param name="tok">The token</param>
		/// <param name="iChar">The index of the character.</param>
		/// ------------------------------------------------------------------------------------
		private TextTokenSubstring GetSubstring(ITextToken tok, int iChar)
		{
			int iCharLength = GetLengthOfChar(tok, iChar);
			TextTokenSubstring tts = new TextTokenSubstring((tok is VerseTextToken ?
				((VerseTextToken)tok).Token : tok), iChar, iCharLength);
			return tts;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Records the paragraph style.
		/// </summary>
		/// <param name="tok">The Scripture token.</param>
		/// ------------------------------------------------------------------------------------
		private void RecordParagraphStyle(ITextToken tok)
		{
			if (tok.IsParagraphStart)
			{
				m_paragraphStyle = tok.ParaStyleName;
				m_foundParagraphText = false;
				if (m_processParagraphsSeparately)
					m_fAtSentenceStart = false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Records the character style.
		/// </summary>
		/// <param name="tok">The Scripture token.</param>
		/// ------------------------------------------------------------------------------------
		private void RecordCharacterStyle(ITextToken tok)
		{
			if (tok.CharStyleName != m_characterStyle)
			{
				m_characterStyle = tok.CharStyleName;
				m_foundCharacterText = false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes the abbreviations from a Scripture token.
		/// </summary>
		/// <param name="tok">The Scripture token.</param>
		/// <returns>Scripture token with any abbreviations replaced with spaces.</returns>
		/// ------------------------------------------------------------------------------------
		private string RemoveAbbreviations(ITextToken tok)
		{
			string tokenText = tok.Text;
			foreach (string abbreviation in m_abbreviations)
			{
				if (abbreviation == "")
					continue;

				string spaces = new string(' ', abbreviation.Length);
				tokenText = tokenText.Replace(abbreviation, spaces);
			}

			Debug.Assert(tok.Text.Length == tokenText.Length,
				"Length of text should not change",
				"Abbreviations are replaced by spaces, but the overall text length should stay the same.");
			return tokenText;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a checking error if paragraph style requires an initial uppercase letter,
		/// but the tssFirstLetter is lowercase.
		/// </summary>
		/// <param name="tok">The Scripture token.</param>
		/// <param name="ttsFirstLetter">The token substring of the first word-forming character
		/// in the given token.</param>
		/// <param name="result">The error results.</param>
		/// <returns><c>true</c> if an error was added to the list of results; otherwise
		/// <c>false</c></returns>
		/// ------------------------------------------------------------------------------------
		private bool CheckForParaCapitalizationError(ITextToken tok,
			TextTokenSubstring ttsFirstLetter, List<TextTokenSubstring> result)
		{
			if (m_foundParagraphText)
				return false;

			m_foundParagraphText = true;

			// The first character of the paragraph is lowercase.
			// Look it up in the capitalized styles dictionary to determine if it should be uppercase.
			StyleCapInfo styleCapInfo;
			if (m_allCapitalizedStyles.TryGetValue(m_paragraphStyle, out styleCapInfo))
			{
				ttsFirstLetter.InventoryText = m_paragraphStyle;

				ttsFirstLetter.Message = CapitalizationCheck.GetErrorMessage(m_checksDataSource,
					styleCapInfo.m_capCheck, m_paragraphStyle);
				result.Add(ttsFirstLetter);
				return true;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a checking error if character style requires an initial uppercase letter,
		/// but the tssFirstLetter is lowercase.
		/// </summary>
		/// <param name="tok">The Scripture token.</param>
		/// <param name="ttsFirstLetter">The token substring of the first word-forming character
		/// in the given token.</param>
		/// <param name="result">The result.</param>
		/// <returns><c>true</c> if an error was added to the list of results; otherwise
		/// <c>false</c></returns>
		/// ------------------------------------------------------------------------------------
		private bool CheckForCharStyleCapilizationError(ITextToken tok,
			TextTokenSubstring ttsFirstLetter, List<TextTokenSubstring> result)
		{
			if (m_foundCharacterText)
				return false;

			m_foundCharacterText = true;

			// The first word-forming character of the character style is lowercase.
			// Look it up in the capitalized styles dictionary to determine if it should be uppercase.
			StyleCapInfo styleCapInfo;
			if (m_allCapitalizedStyles.TryGetValue(m_characterStyle, out styleCapInfo) &&
				styleCapInfo.m_type == StyleInfo.StyleTypes.character)
			{
				ttsFirstLetter.InventoryText = m_characterStyle;
				ttsFirstLetter.Message = CapitalizationCheck.GetErrorMessage(m_checksDataSource,
					styleCapInfo.m_capCheck, m_characterStyle);
				result.Add(ttsFirstLetter);
				return true;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified character is sentence final punctuation.
		/// </summary>
		/// <param name="ch">The specified character.</param>
		/// <returns>
		/// 	<c>true</c> if the specified character is sentence final punctuation; otherwise, <c>false</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private bool IsSentenceFinalPunctuation(char ch)
		{
			return m_validSentenceFinalPuncts.IndexOf(ch) >= 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the length of the character including any associated diacritics that follow
		/// the base character.
		/// </summary>
		/// <param name="tok">The text token.</param>
		/// <param name="iBaseCharacter">The index of the base character in the text token.</param>
		/// <returns>length of the character, including all following diacritics</returns>
		/// ------------------------------------------------------------------------------------
		private int GetLengthOfChar(ITextToken tok, int iBaseCharacter)
		{
			int charLength = 1;
			int iChar = iBaseCharacter + 1;
			while(iChar < tok.Text.Length && m_categorizer.IsDiacritic(tok.Text[iChar++]))
				charLength++;

			return charLength;
		}
		#endregion
	}
}
