using System;
using System.Collections.Generic;
using System.Text;
using SILUBS.SharedScrUtils;
using System.Diagnostics;

namespace SILUBS.ScriptureChecks
{
	public enum PunctuationTokenType { whitespace, punctuation, number, paragraph, quoteSeparator };
	public enum CheckingLevel { Advanced, Intermediate, Basic };

	#region PunctuationCheck class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Checks sequences of punctuation (in relation to their positions in surrounding text).
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class PunctuationCheck : IScrCheckInventory
	{
		private const string kValidItemsParameter = "ValidPunctuation";
		private const string kInvalidItemsParameter = "InvalidPunctuation";

		private IChecksDataSource m_checksDataSource;
		private CharacterCategorizer m_characterCategorizer;
		private List<TextTokenSubstring> m_punctuationSequences;
		//! (PARATEXT) need to think about the possibility that _ is a punctuation mark in this language.
		internal static string s_whitespaceRep = "_";
		private List<string> m_validItemsList;
		private List<string> m_invalidItemsList;
		private string m_validItems;
		private string m_invalidItems;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="PunctuationCheck"/> class.
		/// </summary>
		/// <param name="checksDataSource">The checks data source.</param>
		/// ------------------------------------------------------------------------------------
		public PunctuationCheck(IChecksDataSource checksDataSource)
		{
			m_checksDataSource = checksDataSource;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a localized version of the specified string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private string Localize(string strToLocalize)
		{
			return m_checksDataSource.GetLocalizedString(strToLocalize);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// THIS REALLY OUGHT TO BE List
		/// Valid items, separated by spaces.
		/// Inventory form queries this to know how what status to give each item
		/// in the inventory. Inventory form updates this if user has changed the status
		/// of any item.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public string ValidItems
		{
			get { return m_validItems; }
			set
			{
				m_validItems = (value == null ? string.Empty : value.Trim());
				m_validItemsList = new List<string>();
				if (m_validItems != string.Empty)
					m_validItemsList = new List<string>(m_validItems.Split());
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// THIS REALLY OUGHT TO BE List
		/// Invalid items, separated by spaces.
		/// Inventory form queries this to know how what status to give each item
		/// in the inventory. Inventory form updates this if user has changed the status
		/// of any item.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public string InvalidItems
		{
			get { return m_invalidItems; }
			set
			{
				m_invalidItems = (value == null ? string.Empty : value.Trim());
				m_invalidItemsList = new List<string>();
				if (m_invalidItems != string.Empty)
					m_invalidItemsList = new List<string>(m_invalidItems.Split());
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The full name of the check, e.g. "Punctuation". After replacing any spaces
		/// with underscores, this can also be used as a key for looking up a localized
		/// string if the application supports localization.  If this is ever changed,
		/// DO NOT change the CheckId!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string CheckName { get { return Localize("Punctuation Patterns"); } }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The unique identifier of the check. This should never be changed!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Guid CheckId { get { return StandardCheckIds.kguidPunctuation; } }

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
			get { return 500; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the description for this check.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Description { get { return Localize("Checks for potential inconsistencies in the use of punctuation."); } }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is the column header of the first column when you create an
		/// inventory of this type of error.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string InventoryColumnHeader
		{
			get { return Localize("Punctuation"); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update the parameter values for storing Paratext's valid and invalid lists in
		/// CheckDataSource and then save them. This is here because the Paratext inventory form
		/// does not know the names of the parameters that need to be saved for a given check,
		/// only the check knows this.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Save()
		{
			m_checksDataSource.SetParameterValue(kValidItemsParameter, ValidItems);
			m_checksDataSource.SetParameterValue(kInvalidItemsParameter, InvalidItems);
			m_checksDataSource.Save();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Execute the check. Call 'RecordError' for every error found.
		/// </summary>
		/// <param name="toks">ITextToken's corresponding to the text to be checked.
		/// Typically this is one books worth.</param>
		/// <param name="record">Call this delegate to report each error found.</param>
		/// ------------------------------------------------------------------------------------
		public void Check(IEnumerable<ITextToken> toks, RecordErrorHandler record)
		{
			m_punctuationSequences = GetReferences(toks, string.Empty);

			string msgInvalid = Localize("Invalid punctuation pattern");
			string msgUnspecified = Localize("Unspecified use of punctuation pattern");

			foreach (TextTokenSubstring tts in m_punctuationSequences)
			{
				string punctCharacter = tts.InventoryText;

				if (!m_validItemsList.Contains(punctCharacter))
				{
					tts.Message = m_invalidItemsList.Contains(punctCharacter) ?
						msgInvalid : msgUnspecified;

					record(new RecordErrorEventArgs(tts, CheckId));
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return a TextTokenSubstring for all occurances of the desiredKey.
		/// </summary>
		/// <param name="tokens"></param>
		/// <param name="desiredKey">e.g., _[_ or empty string to look for all patterns</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public List<TextTokenSubstring> GetReferences(IEnumerable<ITextToken> tokens, string desiredKey)
		{
#if DEBUG
			List<ITextToken> AllTokens = new List<ITextToken>(tokens);
#endif
			m_characterCategorizer = m_checksDataSource.CharacterCategorizer;
			string sXmlMatchedPairs = m_checksDataSource.GetParameterValue("PunctuationPatterns");
			if (sXmlMatchedPairs != null && sXmlMatchedPairs.Trim().Length > 0)
			{
				m_validItemsList = new List<string>();
				m_invalidItemsList = new List<string>();
				PuncPatternsList puncPatternsList = PuncPatternsList.Load(sXmlMatchedPairs,
					m_checksDataSource.GetParameterValue("DefaultWritingSystemName"));
				foreach (PuncPattern pattern in puncPatternsList)
				{
					if (pattern.Valid)
						m_validItemsList.Add(pattern.Pattern);
					else
						m_invalidItemsList.Add(pattern.Pattern);
				}
			}
			else
			{
				ValidItems = m_checksDataSource.GetParameterValue(kValidItemsParameter);
				InvalidItems = m_checksDataSource.GetParameterValue(kInvalidItemsParameter);
			}

			string sLevel = m_checksDataSource.GetParameterValue("PunctCheckLevel");
			CheckingLevel level;
			switch (sLevel)
			{
				case "Advanced": level = CheckingLevel.Advanced; break;
				case "Intermediate": level = CheckingLevel.Intermediate; break;
				case "Basic":
				default:
					level = CheckingLevel.Basic;
					break;
			}
			string sWhitespaceRep = m_checksDataSource.GetParameterValue("PunctWhitespaceChar");
			if (!String.IsNullOrEmpty(sWhitespaceRep))
				s_whitespaceRep = sWhitespaceRep.Substring(0, 1);
			string preferredLocale =
				m_checksDataSource.GetParameterValue("PreferredLocale") ?? string.Empty;

			QuotationMarkCategorizer quotationCategorizer =
				new QuotationMarkCategorizer(m_checksDataSource);

			// create processing state machines, one for body text, one for notes
			ProcessPunctationTokens bodyProcessor = new ProcessPunctationTokens(
				m_characterCategorizer, quotationCategorizer, level);

			ProcessPunctationTokens noteProcessor =	new ProcessPunctationTokens(
				m_characterCategorizer, quotationCategorizer, level);

			m_punctuationSequences = new List<TextTokenSubstring>();

			// build list of note and non-note tokens
			foreach (ITextToken tok in tokens)
			{
				if (tok.Text == null || (tok.Locale ?? string.Empty) != preferredLocale)
					continue;

				if (tok.TextType == TextType.Note)
				{
					// if a new note is starting finalize any punctuation sequences from the previous note
					if (tok.IsNoteStart)
						noteProcessor.FinalizeResult(desiredKey, m_punctuationSequences, true);
					noteProcessor.ProcessToken(tok, desiredKey, m_punctuationSequences);
				}
				else if (tok.TextType == TextType.Verse || tok.TextType == TextType.Other)
				{
					// body text: finalize any note that was in progress and continue with body text
					noteProcessor.FinalizeResult(desiredKey, m_punctuationSequences, true);
					bodyProcessor.ProcessToken(tok, desiredKey, m_punctuationSequences);
				}
				else if (tok.IsParagraphStart)
				{
					bodyProcessor.FinalizeResult(desiredKey, m_punctuationSequences, true);
					bodyProcessor.TreatAsParagraphStart = true;
				}
			}

			noteProcessor.FinalizeResult(desiredKey, m_punctuationSequences, true);
			bodyProcessor.FinalizeResult(desiredKey, m_punctuationSequences, true);

			return m_punctuationSequences;
		}
	}

	#endregion

	#region PunctuationToken class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Record information about one component of a punctuation sequence.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	class PunctuationToken
	{
		public PunctuationTokenType TokenType;
		public TextTokenSubstring Tts;

		// is initial (opening) quotation punctuation, e.g. U+201C LEFT DOUBLE QUOTATION MARK
		public bool IsInitial;

		// is final (closing) quotation punctuation, e.g. U+201D RIGHT DOUBLE QUOTATION MARK
		public bool IsFinal;

		// is token a paragraph break
		public bool IsParaBreak;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="PunctuationToken"/> class for a
		/// whitespace character.
		/// </summary>
		/// <param name="isInitial">if set to <c>true</c> is opening quotation mark.</param>
		/// <param name="isFinal">if set to <c>true</c> is closing quotation mark.</param>
		/// <param name="isParaBreak">if set to <c>true</c> the whitespace represents a newline.
		/// </param>
		/// ------------------------------------------------------------------------------------
		public PunctuationToken(bool isInitial, bool isFinal, bool isParaBreak) :
			this(PunctuationTokenType.whitespace, null, isInitial, isFinal)
		{
			IsParaBreak = isParaBreak;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="PunctuationToken"/> class.
		/// </summary>
		/// <param name="tokenType">Type of the token.</param>
		/// <param name="tts">The TextTokenSubstring.</param>
		/// <param name="isInitial">if set to <c>true</c> is opening quotation mark.</param>
		/// <param name="isFinal">if set to <c>true</c> is closing quotation mark.</param>
		/// ------------------------------------------------------------------------------------
		public PunctuationToken(PunctuationTokenType tokenType, TextTokenSubstring tts,
			bool isInitial,	bool isFinal)
		{
			TokenType = tokenType;
			Tts = tts;
			IsInitial = isInitial;
			IsFinal = isFinal;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance is punctuation or a special whitespace
		/// character to separate same-direction quote marks (which should behave like
		/// punctuation).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsPunctuation
		{
			get
			{
				return TokenType == PunctuationTokenType.punctuation ||
					TokenType == PunctuationTokenType.quoteSeparator;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents the current PunctuationToken.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			switch (TokenType)
			{
				case PunctuationTokenType.whitespace:
				case PunctuationTokenType.quoteSeparator:
					return PunctuationCheck.s_whitespaceRep;
				case PunctuationTokenType.number:
					return string.Empty;
				default:
					return Tts.Text;
			}
		}
	}

	#endregion

	#region ProcessPunctationTokens class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// State machine to process sequences of punctuation tokens.
	/// We have one of these objects for note text, one for body text.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	class ProcessPunctationTokens
	{
		// current punctuation sequence, emptied when finalized or a new word starts
		private List<PunctuationToken> m_puncts = new List<PunctuationToken>();
		private CharacterCategorizer m_categorizer = null; // this lets us know what a punctuation character is
		private QuotationMarkCategorizer m_quotationCategorizer;
		private CheckingLevel m_level;
		private bool m_finalizedWithNumber = false;
		private bool m_fTreatAsParagraphStart = true;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets a value indicating whether to treat a token as a paragraph start even if it
		/// isn't (because a previous token that wasn't processed represented the start of a
		/// para).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal bool TreatAsParagraphStart
		{
			set { m_fTreatAsParagraphStart = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ProcessPunctationTokens"/> class.
		/// </summary>
		/// <param name="categorizer">The categorizer.</param>
		/// <param name="quotationCategorizer">The quotation categorizer.</param>
		/// <param name="level">Indicator to determine how much to combine contiguous
		/// punctuation sequences into patterns. Advanced = All contiguous punctuation and
		/// whitespace characters form a single pattern; Intermediate = Contiguous punctuation
		/// forms a single pattern (delimeted by whitespace); Basic = Each punctuation character
		/// stands alone. In all three modes, whitespace before and/or after a punctuation token
		/// indicates whether is is word-initial, word-medial, word-final, or isolated</param>
		/// ------------------------------------------------------------------------------------
		public ProcessPunctationTokens(CharacterCategorizer categorizer,
			QuotationMarkCategorizer quotationCategorizer, CheckingLevel level)
		{
			m_categorizer = categorizer;
			m_quotationCategorizer = quotationCategorizer;
			m_level = level;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Extract the punctuation sequences from this token
		/// </summary>
		/// <param name="tok"></param>
		/// <param name="desiredKey"></param>
		/// <param name="result"></param>
		/// ------------------------------------------------------------------------------------
		public void ProcessToken(ITextToken tok, string desiredKey, List<TextTokenSubstring> result)
		{
			if (tok.IsParagraphStart || m_fTreatAsParagraphStart)
			{
				ProcessWhitespaceOrParagraph(true);
				m_fTreatAsParagraphStart = false;
			}

			// for each character in token
			for (int i = 0; i < tok.Text.Length; ++i)
			{
				char cc = tok.Text[i];
				if (m_categorizer.IsPunctuation(cc))
					ProcessPunctuation(tok, i);
				else if (char.IsDigit(cc))
				{
					// If the previous finalized was done with a number,
					// and we have a single punctuation mark
					// followed by another number, ignore this sequence,
					// e.g. 3:14
					if (m_finalizedWithNumber && m_puncts.Count == 1 &&
						m_puncts[0].TokenType == PunctuationTokenType.punctuation)
					{
						m_puncts.Clear();
					}
					else
					{
						ProcessDigit(tok, i);
						FinalizeResult(desiredKey, result, false);
					}
				}
				else if (char.IsWhiteSpace(cc))
					ProcessWhitespaceOrParagraph(false);
				else
				{
					// if not punctuation, whitespace, or digit; it must be the start of a new word
					// therefore finalize any open punctuation sequence
					FinalizeResult(desiredKey, result, false);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add punctuation to list
		/// </summary>
		/// <param name="tok">The text token</param>
		/// <param name="i">The index of the punctuation character</param>
		/// ------------------------------------------------------------------------------------
		private void ProcessPunctuation(ITextToken tok, int i)
		{
			TextTokenSubstring tts = new TextTokenSubstring(tok, i, 1);
			bool isInitial = m_quotationCategorizer.IsInitialPunctuation(tts.Text);
			bool isFinal = m_quotationCategorizer.IsFinalPunctuation(tts.Text);
			m_puncts.Add(new PunctuationToken(PunctuationTokenType.punctuation, tts, isInitial, isFinal));

			// special case: treat a sequence like
			// opening quotation punctuation/space/opening quotation punctuation
			// as if the space were not there. an example of this would be
			// U+201C LEFT DOUBLE QUOTATION MARK
			// U+0020 SPACE
			// U+2018 LEFT SINGLE QUOTATION MARK
			// this allows a quotation mark to be considered word initial even if it is followed by a space
			if (m_puncts.Count >= 3)
			{
				// If the last three tokens are punctuation/whitespace/punctuation
				if (m_puncts[m_puncts.Count - 2].TokenType == PunctuationTokenType.whitespace &&
					!m_puncts[m_puncts.Count - 2].IsParaBreak &&
					m_puncts[m_puncts.Count - 3].TokenType == PunctuationTokenType.punctuation)
				{
					// And both punctuation have quote directions which point in the same direction,
					if (m_puncts[m_puncts.Count - 3].IsInitial && m_puncts[m_puncts.Count - 1].IsInitial ||
						m_puncts[m_puncts.Count - 3].IsFinal && m_puncts[m_puncts.Count - 1].IsFinal)
					{
						// THEN mark the whitespace as a quote separator.
						m_puncts[m_puncts.Count - 2].TokenType = PunctuationTokenType.quoteSeparator;
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a number to the list
		/// </summary>
		/// <param name="tok"></param>
		/// <param name="i"></param>
		/// ------------------------------------------------------------------------------------
		private void ProcessDigit(ITextToken tok, int i)
		{
			m_puncts.Add(new PunctuationToken(PunctuationTokenType.number, null, false, false));

#if UNUSED
			// special case: treat a sequence like
			// number/punctuation/number
			// as if the punctuation were not there. an example of this would be 1:2
			// this allows the : in 1:2 not to be counted as punctuation
			if (tokens.Count >= 3)
			{
				// If the last three tokens are number/select punctuation/number
				if (tokens[tokens.Count - 3].TokenType == PunctuationTokenType.number)
				{
					string separator = tokens[tokens.Count - 2].ToString();
					//! make the list of separator characters configurable
					if (separator == "," || separator == "." || separator == "-" || separator == ":")
					{
						tokens.RemoveAt(tokens.Count - 2);

						// The offset (-2) stays the same as the line of code above
						// since after the previous line is executed some of the tokens shift position.
						tokens.RemoveAt(tokens.Count - 2);
					}
				}
			}
#endif
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add whitespace to the list unless the last item in the list is already whitespace
		/// </summary>
		/// <param name="fIsParaStart">True for the token to be a paragraph start, false otherwise.
		/// </param>
		/// ------------------------------------------------------------------------------------
		public void ProcessWhitespaceOrParagraph(bool fIsParaStart)
		{
			if (m_puncts.Count > 0 && m_puncts[m_puncts.Count - 1].TokenType == PunctuationTokenType.whitespace)
			{
				if (!m_puncts[m_puncts.Count - 1].IsParaBreak && fIsParaStart)
					m_puncts[m_puncts.Count - 1].IsParaBreak = true;
				return;
			}

			m_puncts.Add(new PunctuationToken(false, false, fIsParaStart));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="desiredKey"></param>
		/// <param name="result"></param>
		/// <param name="addWhitespace"></param>
		/// ------------------------------------------------------------------------------------
		public void FinalizeResult(string desiredKey, List<TextTokenSubstring> result, bool addWhitespace)
		{
			// If a digit caused FinalizeResult() to be called set a flag, otherwise clear the flag.
			// This flag is tested to help see if a punctuation character occurs between two digits.
			m_finalizedWithNumber =
				(m_puncts.Count > 0 && m_puncts[m_puncts.Count - 1].TokenType == PunctuationTokenType.number);

			// if no punctuation character is found clear sequence and quit
			PunctuationToken currentPTok = null;
			foreach (PunctuationToken pTok in m_puncts)
			{
				if (pTok.TokenType == PunctuationTokenType.punctuation)
				{
					currentPTok = pTok;
					break;
				}
			}
			if (currentPTok == null)
			{
				m_puncts.Clear();
				return;
			}

			// if we have been requested to treat this sequence as if it were followed by whitespace,
			// then add a space to the sequence. This happens, for example, at the end of a footnote.
			// \f + text.\f* otherwise the . would be considered word medial instead of word final
			if (addWhitespace)
				ProcessWhitespaceOrParagraph(false);

			switch (m_level)
			{
				case CheckingLevel.Advanced:
					AdvancedFinalize(currentPTok, desiredKey, result);
					break;
				case CheckingLevel.Intermediate:
					IntermediateFinalize(desiredKey, result);
					break;
				case CheckingLevel.Basic:
					BasicFinalize(desiredKey, result);
					break;
			}

			m_puncts.Clear();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Treat each punctuation and whitespace sequence as a single string. It is called
		/// advanced since many more inventory items for the user to look at, and only advanced
		/// users (we hope) will look at these results.
		/// </summary>
		/// <param name="pTok">The current punctuation token, whose TextToken substring is
		/// modified to indicate a pattern of multiple punctuation characters</param>
		/// <param name="desiredKey">If specified, indicates a specific punctuation pattern to
		/// seek (all others will be discarded); To retrieve all punctation substrings, specify
		/// the empty string.</param>
		/// <param name="result">List of TextTokenSubstring items that will be added to</param>
		/// ------------------------------------------------------------------------------------
		private void AdvancedFinalize(PunctuationToken pTok, string desiredKey,
			List<TextTokenSubstring> result)
		{
			// concatanate all the punctuation sequences into one string
			string pattern = String.Empty;
			foreach (PunctuationToken pTok2 in m_puncts)
			{
				//System.Diagnostics.Debug.Assert(pTok2.Tts == null || pTok2.Tts.Token == pTok.Tts.Token);
				pattern += pTok2.ToString();
			}
			pTok.Tts.InventoryText = pattern;

			if (desiredKey == String.Empty || desiredKey == pTok.Tts.InventoryText)
				result.Add(pTok.Tts);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Treat each punctuation sequence as a single string, breaking the pattern at each
		/// whitespace (Except for whitespace between pairs of quotes that are both in the
		/// same direction (both opening or closing quotes).
		/// </summary>
		/// <param name="desiredKey">If specified, indicates a specific punctuation pattern to
		/// seek (all others will be discarded); To retrieve all punctation substrings, specify
		/// the empty string.</param>
		/// <param name="result">List of TextTokenSubstring items that will be added to</param>
		/// ------------------------------------------------------------------------------------
		private void IntermediateFinalize(string desiredKey, List<TextTokenSubstring> result)
		{
			// concatanate all the punctuation sequences into one string
			string pattern = "";
			PunctuationToken pTok = null;
			PunctuationToken tok2;

			for (int i = 0; i < m_puncts.Count; ++i)
			{
				tok2 = m_puncts[i];
				pattern += tok2.ToString();

				// Every generated result must start with a punctuation character.
				// If we do not currently have a punctuation character (because it
				// null'ed below) remember this one.
				if (tok2.TokenType == PunctuationTokenType.punctuation || tok2.TokenType == PunctuationTokenType.quoteSeparator)
				{
					Debug.Assert(pTok != null || tok2.TokenType == PunctuationTokenType.punctuation, "Quote separator should never be the first non-whitespace character in a sequence (after all, it IS whitespace!)");
					if (pTok == null)
						pTok = tok2;
					else
					{
						if (tok2.Tts != null && pTok.Tts.LastToken != tok2.Tts.FirstToken)
						{
							Debug.Assert(tok2.Tts.FirstToken == tok2.Tts.LastToken);
							pTok.Tts.AddToken(tok2.Tts.FirstToken);
						}
						pTok.Tts++;
					}
				}

				// Generate a pattern when you see a non-leading whitespace or end of list
				if (tok2.TokenType == PunctuationTokenType.whitespace || i == m_puncts.Count - 1)
				{
					if (pTok != null)  // Must have a punctuation token
					{
						pTok.Tts.InventoryText = pattern;
						if (desiredKey == "" || desiredKey == pTok.Tts.InventoryText)
							result.Add(pTok.Tts);
					}

					// Reset pattern to match this token
					if (tok2.TokenType == PunctuationTokenType.whitespace)
						pattern = tok2.ToString();
					else
						pattern = "";

					pTok = null;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Basic finalize is complicated because it generates a pattern for every punctuation
		/// mark. An exception is if multiple consecutive periods occur, then a string of
		/// periods will be in one pattern.
		/// </summary>
		/// <param name="desiredKey">If specified, indicates a specific punctuation pattern to
		/// seek (all others will be discarded); To retrieve all punctation substrings, specify
		/// the empty string.</param>
		/// <param name="result">List of TextTokenSubstring items that will be added to</param>
		/// ------------------------------------------------------------------------------------
		private void BasicFinalize(string desiredKey, List<TextTokenSubstring> result)
		{
			PunctuationToken pTok;
			for (int i = 0; i < m_puncts.Count; ++i)
			{
				pTok = m_puncts[i];
				if (pTok.TokenType != PunctuationTokenType.punctuation)
					continue;

				// Normally i and j end up the same.
				// When multiple consecutive periods occur (e.g. blah...blah)
				// i will be the first period and j the last period.
				int j = i;
				while (m_puncts[i].ToString() == "." && j + 1 < m_puncts.Count &&
					m_puncts[j + 1].ToString() == ".")
				{
					++j;
				}

				string pattern = PunctuationSequencePatternPrefix(i);
				for (int k = i; k <= j; ++k)
					pattern += m_puncts[k].ToString();

				pattern += PunctuationSequencePatternSuffix(j);
				pTok.Tts.InventoryText = pattern;

				if (desiredKey == String.Empty || desiredKey == pTok.Tts.InventoryText)
					result.Add(pTok.Tts);

				i = j;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Look at previous non-punctuation tokens, the first token that is found determines
		/// the prefix for this pattern for example, if it finds whitespace token, it returns
		/// a prefix of _
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private string PunctuationSequencePatternPrefix(int index)
		{
			for (int i = index - 1; i >= 0; --i)
			{
				if (!m_puncts[i].IsPunctuation)
					return m_puncts[i].ToString();
			}

			return string.Empty;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Look at following non-punctuation tokens, the first token that is found determines
		/// the suffix for this pattern for example, if it finds whitespace token, it returns
		/// a suffix of _
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private string PunctuationSequencePatternSuffix(int index)
		{
			for (int i = index; i < m_puncts.Count; ++i)
			{
				if (!m_puncts[i].IsPunctuation)
					return m_puncts[i].ToString();
			}

			return string.Empty;
		}
	}

	#endregion
}
