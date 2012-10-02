using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using SILUBS.ScriptureChecks;
using SILUBS.SharedScrUtils;
using System.Diagnostics;

namespace SILUBS.ScriptureChecks
{
	#region QuotationCheck class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class QuotationCheck : IScrCheckInventory
	{
		private IChecksDataSource m_chkDataSource;
		private CharacterCategorizer m_charCategorizer;
		private List<TextTokenSubstring> m_qmProblems;
		private readonly string m_validItemsParameter = "ValidQuotationMarks";
		private readonly string m_invalidItemsParameter = "InvalidQuotationMarks";
		private string m_validItems;
		private string m_invalidItems;
		private List<string> m_validItemsList;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="QuotationCheck"/> class.
		/// ------------------------------------------------------------------------------------
		public QuotationCheck(IChecksDataSource checksDataSource)
		{
			m_chkDataSource = checksDataSource;
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
		/// THIS REALLY OUGHT TO BE List
		/// Valid items, separated by spaces.
		/// Inventory form queries this to know how what status to give each item
		/// in the inventory. Inventory form updates this if user has changed the status
		/// of any item.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ValidItems
		{
			get { return m_validItems; }
			set
			{
				m_validItems = value.Trim();
				m_validItemsList = (m_validItems == string.Empty ?
					new List<string>() : new List<string>(m_validItems.Split()));
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
			set	{ m_invalidItems = value.Trim(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The full name of the check, e.g. "Quotation Errors". After replacing any spaces
		/// with underscores, this can also be used as a key for looking up a localized
		/// string if the application supports localization.  If this is ever changed,
		/// DO NOT change the CheckId!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string CheckName
		{
			get { return Localize("Quotation Marks"); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The unique identifier of the check. This should never be changed!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Guid CheckId { get { return StandardCheckIds.kguidQuotations; } }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the group which contains this check.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string CheckGroup
		{
			get { return Localize("Basic"); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a number that can be used to order this check relative to other checks in the
		/// same group when displaying checks in the UI.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public float RelativeOrder
		{
			get { return 400; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the description for this check.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Description
		{
			get { return Localize("Checks for potential inconsistencies in the markup of quotations."); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is the column header of the first column when you create an
		/// inventory of this type of error.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public string InventoryColumnHeader
		{
			get { return Localize("Quotations"); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Saves the parameters for this check.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Save()
		{
			m_chkDataSource.SetParameterValue(m_validItemsParameter, m_validItems);
			m_chkDataSource.SetParameterValue(m_invalidItemsParameter, m_invalidItems);
			m_chkDataSource.Save();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks the given tokens for quotation errors. Ignores any found in 'validItemsList'.
		/// Calls the given RecordError handler for each one.
		/// </summary>
		/// <param name="toks">The tokens to check.</param>
		/// <param name="record">Method to call to record errors.</param>
		/// ------------------------------------------------------------------------------------
		public void Check(IEnumerable<ITextToken> toks, RecordErrorHandler record)
		{
			foreach (TextTokenSubstring tts in GetReferences(toks, string.Empty))
			{
				string punctChar = tts.ToString();
				if (!m_validItemsList.Contains(punctChar))
					record(new RecordErrorEventArgs(tts, CheckId));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a list if TextTokenSubstrings containing the references and character offsets
		/// where quotation problems occur.
		/// </summary>
		/// <param name="tokens">The tokens (from the data source) to check for quotation problems.</param>
		/// <param name="desiredKey">empty string.</param>
		/// ------------------------------------------------------------------------------------
		public List<TextTokenSubstring> GetReferences(IEnumerable<ITextToken> tokens, string desiredKey)
		{
			m_charCategorizer = m_chkDataSource.CharacterCategorizer;
			ValidItems = m_chkDataSource.GetParameterValue(m_validItemsParameter);
			InvalidItems = m_chkDataSource.GetParameterValue(m_invalidItemsParameter);

			QuotationMarkCategorizer qmCategorizer = new QuotationMarkCategorizer(m_chkDataSource);
			m_qmProblems = new List<TextTokenSubstring>();

			QTokenProcessor bodyProcessor =	new QTokenProcessor(m_chkDataSource,
				m_charCategorizer, qmCategorizer, desiredKey, m_qmProblems);

			QTokenProcessor noteProcessor =	new QTokenProcessor(m_chkDataSource,
				m_charCategorizer, qmCategorizer, desiredKey, m_qmProblems);

			VerseTextToken scrToken = new VerseTextToken();
			foreach (ITextToken tok in tokens)
			{
				scrToken.Token = tok;
				if (scrToken.TextType == TextType.Note)
				{
					// If a new note is starting finalize any sequences from the previous note.
					if (scrToken.IsNoteStart)
						noteProcessor.FinalizeResult();
					noteProcessor.ProcessToken((VerseTextToken)scrToken.Clone());
				}
				else if (scrToken.TextType == TextType.Verse || scrToken.TextType == TextType.Other ||
					tok.IsParagraphStart)
				{
					// body text: finalize any note that was in progress and continue with body text
					noteProcessor.FinalizeResult();
					bodyProcessor.ProcessToken((VerseTextToken)scrToken.Clone());
				}
			}

			noteProcessor.FinalizeResult();
			bodyProcessor.FinalizeResult();
			return m_qmProblems;
		}
	}

	#endregion

	#region QTokenProcessor class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class dedicated to the processing of quotation-related tokens
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class QTokenProcessor
	{
		private Regex m_regExQuotes;
		private string m_desiredKey;
		private bool m_verboseQuotes;
		private bool m_fFoundMissingContinuer = false;
		private string m_noCloserMsg;
		private string m_noOpenerMsg;
		private IChecksDataSource m_chkDataSource;
		private CharacterCategorizer m_charCategorizer;
		private QuotationMarkCategorizer m_qmCategorizer;
		private List<TextTokenSubstring> m_results;
		private List<QToken> m_quotationRelatedTokens = new List<QToken>();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="QuotationRelatedTokensProcessor"/> class.
		/// </summary>
		/// <param name="source">The checks data source.</param>
		/// <param name="charCategorizer">The character categorizer.</param>
		/// <param name="qmCategorizer">The quotation mark categorizer.</param>
		/// <param name="desiredKey">The desired key (can be string.Empty).</param>
		/// <param name="results">The result.</param>
		/// ------------------------------------------------------------------------------------
		internal QTokenProcessor(IChecksDataSource dataSource,
			CharacterCategorizer charCategorizer, QuotationMarkCategorizer qmCategorizer,
			string desiredKey, List<TextTokenSubstring> results)
		{
			m_chkDataSource = dataSource;
			m_charCategorizer = charCategorizer;
			m_qmCategorizer = qmCategorizer;
			m_desiredKey = desiredKey;
			m_results = results;
			m_verboseQuotes = (m_chkDataSource.GetParameterValue("VerboseQuotes") == "Yes");
			m_noCloserMsg = Localize("Unmatched opening mark: level {0}");
			m_noOpenerMsg = Localize("Unmatched closing mark: level {0}");
			m_regExQuotes = new Regex(qmCategorizer.Pattern);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Localizes the specified string.
		/// </summary>
		/// <param name="strToLocalize">The string to localize.</param>
		/// <returns>The localized string</returns>
		/// ------------------------------------------------------------------------------------
		private string Localize(string strToLocalize)
		{
			return m_chkDataSource.GetLocalizedString(strToLocalize);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If the token starts a typographic paragraph, store it as a paragraph-start token and
		/// highlight (shows up on user interface) its text. Otherwise, if the token is
		/// a quotation mark (either opening or closing, as defined by the quotation
		/// categorizer), store it as a quotation mark token.
		/// </summary>
		/// <param name="tok">The token being processed</param>
		/// ------------------------------------------------------------------------------------
		internal void ProcessToken(VerseTextToken tok)
		{
			if (tok.Token.IsParagraphStart)
			{
				TextTokenSubstring tts = new TextTokenSubstring(tok is VerseTextToken ?
					((VerseTextToken)tok).Token : tok, 0, 0);
				ParaStartToken pstok = new ParaStartToken(tts, tok.ParaStyleName);
				m_quotationRelatedTokens.Add(pstok);
			}

			AddTextToParaStartTokens(tok);

			// Find the first non whitespace, non quotation mark character in the token's
			// text. This will be used in the following loop to determine what quotation
			// marks precede all other characters in the token (i.e. what quotation marks
			// begin the paragraph and are possible continuers).
			string exp = m_regExQuotes.ToString();
			exp = exp.Replace("]", "\\]"); // Make sure brackets are escaped
			Regex regEx = new Regex(string.Format("[^{0}|\\s]", exp));
			Match match = regEx.Match(tok.Text);
			int iFirstNoneQMarkChar = (match.Success ? match.Index : -1);

			// Now find all the quotation marks in the token's text.
			MatchCollection mc = m_regExQuotes.Matches(tok.Text);

			// Go through all the quotation marks found, creating quotation tokens
			// for each.
			foreach (Match m in mc)
			{
				TextTokenSubstring tts = new TextTokenSubstring(tok is VerseTextToken ?
					((VerseTextToken)tok).Token : tok, m.Index, m.Length);

				bool fIsOpener = m_qmCategorizer.IsInitialPunctuation(tts.Text);
				bool fPossibleContinuer = (m.Index < iFirstNoneQMarkChar && tok.IsParagraphStart);
				QuotationMarkToken qmt = new QuotationMarkToken(tts, m_qmCategorizer,
					fIsOpener, fPossibleContinuer);
				m_quotationRelatedTokens.Add(qmt);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds paragraph-start tokens (REVIEW: I think there can only be one) in the list of
		/// quotation-related substring tokens that are in the given token replace them (if
		/// possible) with new paragraph-start tokens that include the first word or character
		/// of the token so that there will be some text to highlight in the view when this
		/// error is selected in the list being displayed
		/// </summary>
		/// <param name="tok">The token being processed</param>
		/// ------------------------------------------------------------------------------------
		private void AddTextToParaStartTokens(ITextToken tok)
		{
			// We need to have some text in the token in order to highlight
			if (tok.Text.Length == 0)
				return;

			// Find something to highlight
			int offset = 0;
			int length = 0;
			List<WordAndPunct> words = m_charCategorizer.WordAndPuncts(tok.Text);
			if (words.Count > 0)
			{
				offset = words[0].Offset;
				length = Math.Max(words[0].Word.Length, words[0].Punct.Length);
			}

			// If nothing is found, highlight the first character in the token
			if (length == 0)
				length = 1;

			for (int i = m_quotationRelatedTokens.Count - 1; i >= 0; i--)
			{
				ParaStartToken qrtok = m_quotationRelatedTokens[i] as ParaStartToken;

				if (qrtok != null)
				{
					if (qrtok.Tts.Text == string.Empty ||
						qrtok.Tts.FirstToken.TextType == TextType.VerseNumber ||
						qrtok.Tts.FirstToken.TextType == TextType.ChapterNumber)
					{
						// We have now found a paragraph-start token with no text to highlight
						// in a list window. Update the text of the token with the new text
						// that was found.
						qrtok.Tts = new TextTokenSubstring(tok is VerseTextToken ?
							((VerseTextToken)tok).Token : tok, offset, length);
					}
					break; // Don't change the text of earlier start tokens
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Goes through the list of quotation related tokens and generates errors for missing
		/// continuers and quotations.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void FinalizeResult()
		{
			if (m_quotationRelatedTokens.Count == 0)
				return;

			OpenQuotes openQuotes = new OpenQuotes();
			string prevStyleName = string.Empty;
			for (int i = 0; i < m_quotationRelatedTokens.Count; i++)
			{
				QToken qrtok = m_quotationRelatedTokens[i];
				if (qrtok is QuotationMarkToken)
				{
					QuotationMarkToken qt = (QuotationMarkToken)qrtok;
					CheckQuote(qt, openQuotes);
					openQuotes.MostRecent = qt;
					m_fFoundMissingContinuer = false;
				}
				else if (qrtok is ParaStartToken)
				{
					ParaStartToken pstok = qrtok as ParaStartToken;
					List<string> continuersExpected =
						GetContinuersNeeded(pstok.StyleName, prevStyleName, openQuotes.Level);

					prevStyleName = pstok.StyleName;

					if (continuersExpected.Count > 0)
					{
						if (MatchContinuers(i, continuersExpected, openQuotes.Level))
						{
							i += continuersExpected.Count;
						}
						else
						{
							int contLevel = GetExpectedContinuerLevel(
								continuersExpected[continuersExpected.Count - 1], openQuotes.Level);
							ReportError(pstok, string.Format(continuersExpected.Count == 1 ?
								Localize("Missing continuation mark: level {0}") :
								Localize("Missing continuation marks: levels 1-{0}"),
								contLevel));

							m_fFoundMissingContinuer = true;
						}
					}
				}
			}

			CheckForRemaining(openQuotes);
			m_quotationRelatedTokens.Clear();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the expected continuer level.
		/// </summary>
		/// <param name="continuer">The continuer.</param>
		/// <param name="currentLevel">The current level.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private int GetExpectedContinuerLevel(string continuer, int currentLevel)
		{
			ParagraphContinuationType paraCont = m_qmCategorizer.ContinuationType;
			switch (paraCont)
			{
				case ParagraphContinuationType.RequireOutermost: return 1;
				case ParagraphContinuationType.RequireInnermost: return currentLevel;
				case ParagraphContinuationType.RequireAll:
					for (int i = 1; i <= currentLevel; i++)
					{
						if (m_qmCategorizer.GetContinuationMarkForLevel(i) == continuer)
							return i;
					}
					break;
			}

			return 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes paragraph-start tokens encountered and returns a list of the continuers
		/// that should follow the marker, based on the marker, the currently open quotes, and
		/// the quotation categorizer settings.
		/// </summary>
		/// <param name="styleName">The style being checked in the token for whether it should have
		/// continuers</param>
		/// <param name="prevStyleName">Name of the prev style.</param>
		/// <param name="level">The current level</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private List<string> GetContinuersNeeded(string styleName, string prevStyleName,
			int level)
		{
			List<string> continuers = new List<string>();

			// Check if the quotation categorizer is set to continue at this style
			// or when it follows the previous style.
			if (!m_qmCategorizer.CanStyleContinueQuotation(styleName, prevStyleName) ||
				level < 1)
			{
				return continuers;
			}

			ParagraphContinuationType paraCont = m_qmCategorizer.ContinuationType;

			if (paraCont == ParagraphContinuationType.None)
				return continuers;

			if (paraCont == ParagraphContinuationType.RequireOutermost)
				continuers.Add(m_qmCategorizer.GetContinuationMarkForLevel(1));
			else if (paraCont == ParagraphContinuationType.RequireInnermost)
				continuers.Add(m_qmCategorizer.GetContinuationMarkForLevel(level));
			else
			{
				for (int i = 1; i <= level; i++)
					continuers.Add(m_qmCategorizer.GetContinuationMarkForLevel(i));
			}

			return continuers;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This function is called each time a marker is encountered that should be followed by one
		/// or more continuers. Returns true if the expected continuers are present, false otherwise.
		/// </summary>
		/// <param name="qrToksIndex">The index of the token being processed</param>
		/// <param name="continuersNeeded">A list of the continuers expected</param>
		/// <param name="currentLevel">The currect level</param>
		/// ------------------------------------------------------------------------------------
		private bool MatchContinuers(int qrToksIndex, List<string> continuersNeeded, int currentLevel)
		{
			for (int i = 0; i < continuersNeeded.Count; i++)
			{
				if ((qrToksIndex + i + 1 >= m_quotationRelatedTokens.Count) ||
					(continuersNeeded[i] != m_quotationRelatedTokens[qrToksIndex + i + 1].Tts.Text))
				{
					return false;
				}

				QuotationMarkToken qmTok =
					m_quotationRelatedTokens[qrToksIndex + i + 1] as QuotationMarkToken;


				if (qmTok == null || !qmTok.PossibleContinuer)
					return false;

				GenerateTraceMsg(qmTok,
					string.Format(Localize("Level {0} quote continuer"), qmTok.PossibleLevel));
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This function is called after all the quotation related tokens have been processed.
		/// It generates errors for quotes that are still open at this point.
		/// </summary>
		/// <param name="current">The currently open quotes</param>
		/// ------------------------------------------------------------------------------------
		private void CheckForRemaining(OpenQuotes current)
		{
			if (current.Level == 0)
				return;

			// If the last quotation mark encountered was a closer, and this quotation mark system
			// collapses adjacent quotes, assume it was multiple quotes collapsed into one
			if (m_qmCategorizer.CollapseAdjacentQuotes && !current.MostRecent.IsOpener)
				return;

			// Print errors starting with inner quotes
			for (int i = current.Level; i > 1; i--)
			{
				if (current.Openers[i - 1] is QToken)
					ReportError(current.Openers[i - 1] as QToken, string.Format(m_noCloserMsg, i));
			}

			// Prints error for the outermost quote
			if (m_qmCategorizer.TopLevelClosingExists && current.Openers[0] is QToken)
				ReportError(current.Openers[0] as QToken, string.Format(m_noCloserMsg, 1));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Outputs (the text token substring of) the given quotation related token, attaching
		/// the given error message to it.
		/// </summary>
		/// <param name="qrTok">The token being processed</param>
		/// <param name="message">The error message to be displayed</param>
		/// ------------------------------------------------------------------------------------
		private void ReportError(QToken qrTok, string message)
		{
			Output(new TextTokenSubstring(qrTok.Tts, message));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If verbose quotes are activated, outputs (the text token substring of) the given
		/// quotation related token, attaching the given trace message to it.
		/// </summary>
		/// <param name="qmTok">The token being processed</param>
		/// <param name="message">The trace message to be displayed</param>
		/// ------------------------------------------------------------------------------------
		private void GenerateTraceMsg(QToken qmTok, string message)
		{
			// Only output the trace message in verbose mode
			if (m_verboseQuotes)
				ReportError(qmTok, message);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the text token substring of a quotation related token to the list of results.
		/// At this point the message of the substring is either an error or trace message.
		/// </summary>
		/// <param name="tts">The text token substring being processed</param>
		/// ------------------------------------------------------------------------------------
		private void Output(TextTokenSubstring tts)
		{
			if (m_desiredKey == string.Empty || m_desiredKey == tts.InventoryText)
				m_results.Add(tts);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given the information of the currently open quotation marks, process the next encountered
		/// quotation mark, updating the information and generating errors where appropriate.
		/// </summary>
		/// <param name="qmTok">The quotation mark token being processed</param>
		/// <param name="openQuotes">The currently open quotes</param>
		/// ------------------------------------------------------------------------------------
		private void CheckQuote(QuotationMarkToken qmtok, OpenQuotes openQuotes)
		{
			GenerateTraceMsg(qmtok, string.Format(qmtok.IsOpener ?
				Localize("Level {0} quote opened") : Localize("Level {0} quote closed"),
				(qmtok.IsOpener ? openQuotes.Level + 1 : openQuotes.Level)));

			if (m_qmCategorizer.IsMarkForLevel(qmtok.Tts.Text, openQuotes.Level + 1) && qmtok.IsOpener)
			{
				// The quote is opened properly
				openQuotes.Level++;
				openQuotes.Openers.Add(qmtok);
				return;
			}
			else if (m_qmCategorizer.IsMarkForLevel(qmtok.Tts.Text, openQuotes.Level) &&
				(!qmtok.IsOpener || m_qmCategorizer.OpeningAndClosingAreIdentical(openQuotes.Level)))
			{
				// The quote is closed properly
				openQuotes.Level--;
				openQuotes.Openers.RemoveAt(openQuotes.Level);
				return;
			}

			int possibleQuoteMarkLevel = m_qmCategorizer.Level(qmtok.Tts.Text,
				openQuotes.Level, qmtok.IsOpener);

			if (m_fFoundMissingContinuer)
			{
				Debug.Assert(openQuotes.Level == openQuotes.Openers.Count);
				Debug.Assert(possibleQuoteMarkLevel != 0);

				int newLevel = qmtok.IsOpener ? possibleQuoteMarkLevel : possibleQuoteMarkLevel - 1;
				if (newLevel < openQuotes.Openers.Count)
				{
					while (openQuotes.Openers.Count > newLevel)
						openQuotes.Openers.RemoveAt(openQuotes.Openers.Count - 1);
				}
				else if (newLevel > openQuotes.Openers.Count)
				{
					while (openQuotes.Openers.Count < newLevel)
						openQuotes.Openers.Add("Missing Quote");
				}
				openQuotes.Level = newLevel;
				if (qmtok.IsOpener)
					openQuotes.Openers.Add(qmtok);
				else if (openQuotes.Openers.Count > 0 && openQuotes.Openers.Count > openQuotes.Level)
					openQuotes.Openers.RemoveAt(openQuotes.Level);
				return;
			}
			else if (!m_qmCategorizer.TopLevelClosingExists && possibleQuoteMarkLevel == 1 &&
				openQuotes.Level == 1)
			{
				// Opens a top-level quote when top-level closing quotes do not exist
				openQuotes.Openers.RemoveAt(0);
				openQuotes.Openers.Add(qmtok);
				return;
			}
			else if (possibleQuoteMarkLevel > openQuotes.Level && !qmtok.IsOpener)
			{
				// The quote was closed, but was not opened
				if (!m_qmCategorizer.CollapseAdjacentQuotes || openQuotes.MostRecent == null)
				{
					ReportError(qmtok, string.Format(Localize(m_noOpenerMsg),
						possibleQuoteMarkLevel));
				}
				return;
			}
			else if (possibleQuoteMarkLevel > openQuotes.Level + 1 && qmtok.IsOpener)
			{
				// The opener for the quote belongs to a quote level that is too high
				ReportError(qmtok, string.Format(
					Localize("Unexpected opening mark: level {0}"),
					possibleQuoteMarkLevel));

				// Add missing tokens for skipped levels
				while (openQuotes.Openers.Count < possibleQuoteMarkLevel - 1)
					openQuotes.Openers.Add("Missing Quote");

				openQuotes.Level = possibleQuoteMarkLevel;
				openQuotes.Openers.Add(qmtok);
				return;
			}
			else if (possibleQuoteMarkLevel <= openQuotes.Level && qmtok.IsOpener)
			{
				// Opens a quote at the level already open or at too far out a level
				for (int i = openQuotes.Level; i >= possibleQuoteMarkLevel; i--)
				{
					if (!(openQuotes.Openers[i - 1] is QToken))
						continue;

					ReportError(openQuotes.Openers[i - 1] as QToken,
						string.Format(m_noCloserMsg, i));
				}

				openQuotes.Openers.RemoveRange(possibleQuoteMarkLevel - 1,
					openQuotes.Level - possibleQuoteMarkLevel + 1);
				openQuotes.Level = possibleQuoteMarkLevel;
				openQuotes.Openers.Add(qmtok);
				return;
			}

			// A quote outside the current one is closed before the current one
			for (int i = possibleQuoteMarkLevel; i < openQuotes.Level; i++)
			{
				if (!(openQuotes.Openers[i] is QToken))
					continue;

				ReportError(openQuotes.Openers[i] as QToken,
					string.Format(m_noCloserMsg, i + 1));
			}

			openQuotes.Openers.RemoveRange(possibleQuoteMarkLevel - 1, openQuotes.Level - possibleQuoteMarkLevel);
			openQuotes.Level = possibleQuoteMarkLevel - 1;
		}
	}

	#endregion

	#region QuotationMarkCategorizer class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Quotation Mark Categorizer (don't ask!)
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class QuotationMarkCategorizer
	{
		private QuotationMarksList m_quoteMarks;
		public bool CollapseAdjacentQuotes;
		private StylePropsInfo m_styleInfo;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="QuotationMarkCategorizer"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal QuotationMarkCategorizer(IChecksDataSource source)
		{
			m_quoteMarks = QuotationMarksList.Load(source.GetParameterValue("QuotationMarkInfo"),
				source.GetParameterValue("DefaultWritingSystemName"));
			m_styleInfo = StylePropsInfo.Load(source.GetParameterValue("StylesInfo"));
			CollapseAdjacentQuotes = source.GetParameterValue("CollapseAdjacentQuotes") == "No";
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the paragraph continuation type.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal ParagraphContinuationType ContinuationType
		{
			get	{ return m_quoteMarks.ContinuationType; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the paragraph continuation mark (i.e. opening or closing).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal ParagraphContinuationMark ContinuationMark
		{
			get { return m_quoteMarks.ContinuationMark; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the paragraph continuation mark for the specified level.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal string GetContinuationMarkForLevel(int level)
		{
			if (level > m_quoteMarks.Levels)
				return null;

			return (m_quoteMarks.ContinuationMark == ParagraphContinuationMark.Opening ?
				m_quoteMarks[level - 1].Opening : m_quoteMarks[level - 1].Closing);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the opening quotation mark for the specified level.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal string GetOpenerForLevel(int level)
		{
			return (level > m_quoteMarks.Levels ? null : m_quoteMarks[level - 1].Opening);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the closing quotation mark for the specified level.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal string GetCloserForLevel(int level)
		{
			return (level > m_quoteMarks.Levels ? null : m_quoteMarks[level - 1].Closing);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified style name is legitimate for continuing
		/// a quotation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal bool CanStyleContinueQuotation(string styleName, string prevStyleName)
		{
			if (m_styleInfo != null && m_styleInfo.SentenceInitial != null)
			{
				foreach (StyleInfo spi in m_styleInfo.SentenceInitial)
				{
					if (spi.StyleName == styleName)
					{
						if (spi.UseType == StyleInfo.UseTypes.prose)
							return true;

						if (spi.UseType == StyleInfo.UseTypes.line && IsProseOrStanzaBreak(prevStyleName))
							return true;
					}
				}
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether or not the specified style name represents a style whose use is
		/// prose or stanzabreak.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool IsProseOrStanzaBreak(string styleName)
		{
			foreach (StyleInfo spi in m_styleInfo.SentenceInitial)
			{
				if (spi.StyleName == styleName && spi.UseType == StyleInfo.UseTypes.prose)
					return true;
			}

			foreach (StyleInfo spi in m_styleInfo.Special)
			{
				if (spi.StyleName == styleName && spi.UseType == StyleInfo.UseTypes.stanzabreak)
					return true;
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the first level closing quotation does not exist.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal bool TopLevelClosingExists
		{
			get { return m_quoteMarks.Levels >= 1 && !string.IsNullOrEmpty(m_quoteMarks[0].Closing); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns whether or not the specified level contains an opening and closing quote
		/// mark that are identical.
		/// </summary>
		/// <param name="iLevel">The level to check</param>
		/// <returns>True if the opening and closing quote marks are identical, false otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		internal bool OpeningAndClosingAreIdentical(int level)
		{
			if (level > m_quoteMarks.QMarksList.Count)
				return false; // Just in case

			QuotationMarks qmark = m_quoteMarks.QMarksList[level - 1];
			return (qmark.Opening == qmark.Closing);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified quotation mark is initial punctuation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal bool IsInitialPunctuation(string opening)
		{
			foreach (QuotationMarks qmark in m_quoteMarks.QMarksList)
			{
				if (opening == qmark.Opening)
					return true;
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether [is final punctuation] [the specified quotation mark].
		/// </summary>
		/// <param name="quotationMark">The quotation mark.</param>
		/// ------------------------------------------------------------------------------------
		internal bool IsFinalPunctuation(string closing)
		{
			foreach (QuotationMarks qmark in m_quoteMarks.QMarksList)
			{
				if (closing == qmark.Closing)
					return true;
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified quotation mark is for the specified level.
		/// </summary>
		/// <param name="qmark">The quotation mark to check.</param>
		/// <param name="level">The level.</param>
		/// <returns>
		/// true if the specified quotation mark is for the specified level; false otherwise.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		internal bool IsMarkForLevel(string qmark, int level)
		{
			return Level(qmark, level) == level;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the level of the specified quotaion mark.
		/// </summary>
		/// <param name="qmark">The mark to get the level of</param>
		/// <param name="startingLevel">The level to start searching at</param>
		/// <param name="fSearchForward">True to search forward from the starting level, false
		/// to search backwards from the starting level</param>
		/// ------------------------------------------------------------------------------------
		internal int Level(string qmark, int startingLevel, bool fSearchForward)
		{
			if (startingLevel <= m_quoteMarks.Levels && startingLevel > 0)
			{
				int endLevel = (fSearchForward ? m_quoteMarks.Levels - 1 : 0);
				for (int i = startingLevel - 1; i != endLevel; i += (fSearchForward ? 1 : -1))
				{
					if (qmark == m_quoteMarks[i].Opening || qmark == m_quoteMarks[i].Closing)
						return i + 1;
				}
			}

			return Level(qmark, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the level of the specified quotaion mark.
		/// </summary>
		/// <param name="qmark">The mark to get the level of</param>
		/// <param name="expectedLevel">The level that the mark is expected to have. This level
		/// is checked first.</param>
		/// ------------------------------------------------------------------------------------
		internal int Level(string qmark, int expectedLevel)
		{
			if (expectedLevel > 0 && expectedLevel <= m_quoteMarks.Levels)
			{
				// Check the expected level first. If it matches the opening or the closing, then
				// the expected level is considered to be the level of the mark.
				if (m_quoteMarks[expectedLevel - 1].Opening == qmark ||
					m_quoteMarks[expectedLevel - 1].Closing == qmark)
					return expectedLevel;
			}

			for (int i = 0; i < m_quoteMarks.Levels; i++)
			{
				if (qmark == m_quoteMarks[i].Opening || qmark == m_quoteMarks[i].Closing)
					return i + 1;
			}

			return 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the pattern.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal string Pattern
		{
			get
			{
				List<string> quotationMarks = new List<string>();

				foreach (QuotationMarks qmark in m_quoteMarks.QMarksList)
				{
					if (!string.IsNullOrEmpty(qmark.Opening) && !quotationMarks.Contains(qmark.Opening))
						quotationMarks.Add(Regex.Escape(qmark.Opening));

					if (!string.IsNullOrEmpty(qmark.Closing) && !quotationMarks.Contains(qmark.Closing))
						quotationMarks.Add(Regex.Escape(qmark.Closing));
				}

				return string.Join("|", quotationMarks.ToArray());
			}
		}
	}

	#endregion

	#region OpenQuotes class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class OpenQuotes
	{
		/// <summary>
		/// A list of the open quotes starting with the most deeply nested.
		/// </summary>
		internal ArrayList Openers = new ArrayList();

		/// <summary>
		/// The number of currently open quotes.
		/// </summary>
		internal int Level;

		/// <summary>
		/// The most recently encountered quotation mark.
		/// </summary>
		internal QuotationMarkToken MostRecent;
	}

	#endregion

	#region QToken class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class QToken
	{
		internal TextTokenSubstring Tts;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents the current
		/// <see cref="T:System.Object"/>.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			if (Tts == null)
				return "<null>";

			return string.Format("Text: {0}, Message: {1}",
				string.IsNullOrEmpty(Tts.Text) ? "-" : Tts.Text,
				string.IsNullOrEmpty(Tts.Message) ? "-" : Tts.Message);
		}
	}

	#endregion

	#region QuotationMarkToken class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class QuotationMarkToken : QToken
	{
		private QuotationMarkCategorizer m_categorizer;
		private bool m_fPossibleContinuer;
		private bool m_fIsOpener;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="QuotationMarkToken"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal QuotationMarkToken(TextTokenSubstring tts, QuotationMarkCategorizer categorizer,
			bool fIsOpener, bool fPossibleContinuer)
		{
			Tts = tts;
			m_categorizer = categorizer;
			m_fIsOpener = fIsOpener;
			m_fPossibleContinuer = fPossibleContinuer;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the quotation mark was found at the beginning of
		/// a paragraph and is thus, a possible continuer.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal bool PossibleContinuer
		{
			get { return m_fPossibleContinuer; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the possible level that this .
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal int PossibleLevel
		{
			get { return m_categorizer.Level(Tts.Text, 0); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance is opener.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal bool IsOpener
		{
			get { return m_fIsOpener; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return "Quote-" + Tts.Text + (IsOpener ? ", Opener" : ", Closer") +
				(m_fPossibleContinuer ? ", PossibleContinuer" : string.Empty) + ", " + Tts.Message;
		}
	}

	#endregion

	#region ParaStartToken class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class ParaStartToken : QToken
	{
		internal string StyleName;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ParaStartToken"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal ParaStartToken(TextTokenSubstring tts, string styleName)
		{
			Tts = tts;
			StyleName = styleName;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return "New paragraph-" + StyleName + ", " + Tts.ParagraphStyle + " " + Tts.Message;
		}
	}

	#endregion
}
