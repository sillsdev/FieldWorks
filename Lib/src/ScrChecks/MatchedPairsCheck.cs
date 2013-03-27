using System;
using System.Collections.Generic;
using System.Text;
using SILUBS.SharedScrUtils;

namespace SILUBS.ScriptureChecks
{
	#region MatchedPairsCheck class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// The matched pairs check has an inventory mode in Paratext. TE doesn't use the inventory
	/// stuff.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class MatchedPairsCheck : IScrCheckInventory
	{
		private const string kValidItemsParameter = "MatchedPairingCharacters";
		private const string kInvalidItemsParameter = "UnmatchedPairingCharacters";

		private IChecksDataSource m_checksDataSource;
//		private CharacterCategorizer m_characterCategorizer;
		private List<TextTokenSubstring> m_unmatchedPairs;
		private string m_validItems;
		private string m_invalidItems;
		private List<string> m_validItemsList;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="MatchedPairsCheck"/> class.
		/// </summary>
		/// <param name="checksDataSource">The checks data source.</param>
		/// ------------------------------------------------------------------------------------
		public MatchedPairsCheck(IChecksDataSource checksDataSource)
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
		/// ------------------------------------------------------------------------------------
		public string InvalidItems
		{
			get { return m_invalidItems; }
			set { m_invalidItems = (value == null ? string.Empty : value.Trim());}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The full name of the check, e.g. "Matched Pairs". After replacing any spaces
		/// with underscores, this can also be used as a key for looking up a localized
		/// string if the application supports localization.  If this is ever changed,
		/// DO NOT change the CheckId!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string CheckName { get { return Localize("Matching Punctuation Pairs"); } }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The unique identifier of the check. This should never be changed!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Guid CheckId { get { return StandardCheckIds.kguidMatchedPairs; } }

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
			get { return 300; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the description for this check.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Description { get { return Localize("Checks for unmatched parentheses or other punctuation that normally occurs in pairs."); } }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is the column header of the first column when you create an
		/// inventory of this type of error (not used in TE).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string InventoryColumnHeader
		{
			get { return Localize("Pairs"); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update all parameter values in CheckDataSource and then save them.
		/// This is here because the inventory form does not know what parameters
		/// need to be saved for a given check, only the check knows this.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Save()
		{
			m_checksDataSource.SetParameterValue(kValidItemsParameter, m_validItems);
			m_checksDataSource.SetParameterValue(kInvalidItemsParameter, m_invalidItems);
			m_checksDataSource.Save();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="toks"></param>
		/// <param name="record"></param>
		/// ------------------------------------------------------------------------------------
		public void Check(IEnumerable<ITextToken> toks, RecordErrorHandler record)
		{
			m_unmatchedPairs = GetReferences(toks, string.Empty);

			foreach (TextTokenSubstring tts in m_unmatchedPairs)
			{
				if (!m_validItemsList.Contains(tts.ToString()))
					record(new RecordErrorEventArgs(tts, CheckId));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<TextTokenSubstring> GetReferences(IEnumerable<ITextToken> tokens, string desiredKey)
		{
#if DEBUG
			List<ITextToken> AllTokens = new List<ITextToken>(tokens);
			if (AllTokens.Count == 0)
			{
				// Keep the compiler from complaining about assigning to a variable, but not using it.
			}
#endif
//			m_characterCategorizer = m_checksDataSource.CharacterCategorizer;
			ValidItems = m_checksDataSource.GetParameterValue(kValidItemsParameter);
			InvalidItems = m_checksDataSource.GetParameterValue(kInvalidItemsParameter);

			string preferredLocale =
				m_checksDataSource.GetParameterValue("PreferredLocale") ?? string.Empty;

			string poeticStyles =
				m_checksDataSource.GetParameterValue("PoeticStyles");

			string introductionOutlineStyles =
				m_checksDataSource.GetParameterValue("IntroductionOutlineStyles");

			MatchedPairList pairList =
				MatchedPairList.Load(m_checksDataSource.GetParameterValue("MatchedPairs"),
				m_checksDataSource.GetParameterValue("DefaultWritingSystemName"));

			StyleCategorizer styleCategorizer =
				new StyleCategorizer(poeticStyles, introductionOutlineStyles);

			ProcessMatchedPairTokens bodyProcessor = new ProcessMatchedPairTokens(
				m_checksDataSource, pairList, styleCategorizer);

			ProcessMatchedPairTokens noteProcessor = new ProcessMatchedPairTokens(
				m_checksDataSource, pairList, styleCategorizer);

			m_unmatchedPairs = new List<TextTokenSubstring>();

			foreach (ITextToken tok in tokens)
			{
				if (tok.Text == null || (tok.Locale ?? string.Empty) != preferredLocale)
					continue;

				if (tok.TextType == TextType.Note)
				{
					// if a new note is starting finalize any sequences from the previous note
					if (tok.IsNoteStart)
						noteProcessor.FinalizeResult(desiredKey, m_unmatchedPairs);
					noteProcessor.ProcessToken(tok, desiredKey, m_unmatchedPairs);
				}
				else if (tok.TextType == TextType.Verse || tok.TextType == TextType.Other || tok.IsParagraphStart)
				{
					// body text: finalize any note that was in progress and continue with body text
					noteProcessor.FinalizeResult(desiredKey, m_unmatchedPairs);
					bodyProcessor.ProcessToken(tok, desiredKey, m_unmatchedPairs);
				}
			}

			noteProcessor.FinalizeResult(desiredKey, m_unmatchedPairs);
			bodyProcessor.FinalizeResult(desiredKey, m_unmatchedPairs);

			return m_unmatchedPairs;
		}
	}

	#endregion

	#region ProcessMatchedPairTokens class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	class ProcessMatchedPairTokens
	{
		private List<TextTokenSubstring> m_pairTokensFound = new List<TextTokenSubstring>();
		private MatchedPairList m_pairList;
		private StyleCategorizer m_styleCategorizer;
		private IChecksDataSource m_checksDataSource;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ProcessMatchedPairTokens(IChecksDataSource checksDataSource,
			MatchedPairList pairList, StyleCategorizer styleCategorizer)
		{
			m_checksDataSource = checksDataSource;
			m_pairList = pairList;
			m_styleCategorizer = styleCategorizer;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether or not any of the found pair tokens is part of a
		/// pair that should be closed by the end of the paragraph in which it is found.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool AnyFoundPairsClosedByPara
		{
			get
			{
				foreach (TextTokenSubstring tok in m_pairTokensFound)
				{
					MatchedPair pair = m_pairList.GetPairForOpen(tok.Text);
					if (pair != null && !pair.PermitParaSpanning)
						return true;
				}

				return false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ProcessToken(ITextToken tok, string desiredKey, List<TextTokenSubstring> result)
		{
			if (AnyFoundPairsClosedByPara && tok.IsParagraphStart &&
				!m_styleCategorizer.IsPoeticStyle(tok.ParaStyleName))
			{
				FinalizeResult(desiredKey, result);
			}

			for (int i = 0; i < tok.Text.Length; i++)
			{
				string cc = tok.Text.Substring(i, 1);
				if (m_pairList.BelongsToPair(cc))
				{
					StoreFoundPairToken(tok, i);
					RemoveMatchedPunctAtEndOfFirstWordInIntroOutline(tok, i);
					RemoveIfMatchedPairFound();
					RecordOverlappingPairs();
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void StoreFoundPairToken(ITextToken tok, int i)
		{
			TextTokenSubstring tts = new TextTokenSubstring(tok, i, 1);

			// Assign an initial, default message which may be changed later
			tts.Message =  m_checksDataSource.GetLocalizedString("Unmatched punctuation");
			m_pairTokensFound.Add(tts);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void RemoveMatchedPunctAtEndOfFirstWordInIntroOutline(ITextToken tok, int i)
		{
			if (!m_styleCategorizer.IsIntroductionOutlineStyle(tok.ParaStyleName))
				return;

			// See if we are at the end of the first word
			string[] words = tok.Text.Split();
			string firstWord = words[0];
			if (i + 1 != firstWord.Length)
				return;

			int lastFoundPairToken = m_pairTokensFound.Count - 1;

			// If the current matched pair is in an introduction outline,
			// ends the first word, and is a closing punct, remove it.
			if (m_pairList.IsClose(m_pairTokensFound[lastFoundPairToken].Text))
				m_pairTokensFound.RemoveAt(lastFoundPairToken);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks if the last two pair tokens in the found pair tokens are a matched pair.
		/// If so, they are removed from the found list since a matched set has been complete.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void RemoveIfMatchedPairFound()
		{
			if (m_pairTokensFound.Count < 2)
				return;

			TextTokenSubstring possibleClose = m_pairTokensFound[m_pairTokensFound.Count - 1];
			TextTokenSubstring possibleOpen = m_pairTokensFound[m_pairTokensFound.Count - 2];

			if (m_pairList.IsMatchedPair(possibleOpen.Text, possibleClose.Text))
			{
				// Found a matched pair, remove last two tokens
				m_pairTokensFound.RemoveAt(m_pairTokensFound.Count - 1);
				m_pairTokensFound.RemoveAt(m_pairTokensFound.Count - 1);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void RecordOverlappingPairs()
		{
			if (m_pairTokensFound.Count < 4)
				return;

			TextTokenSubstring tok1 = m_pairTokensFound[m_pairTokensFound.Count - 4];
			TextTokenSubstring tok2 = m_pairTokensFound[m_pairTokensFound.Count - 3];
			TextTokenSubstring tok3 = m_pairTokensFound[m_pairTokensFound.Count - 2];
			TextTokenSubstring tok4 = m_pairTokensFound[m_pairTokensFound.Count - 1];

			// Check if pairs are overlapping.
			if (m_pairList.IsOpen(tok1.Text) && m_pairList.IsOpen(tok2.Text) &&
				m_pairList.IsMatchedPair(tok1.Text, tok3.Text) &&
				m_pairList.IsMatchedPair(tok2.Text, tok4.Text))
			{
				// Found overlapping pairs, so record this by changing
				// the message in the needed TextTokenSubstrings
				string msg = m_checksDataSource.GetLocalizedString("Overlapping pair");
				tok1.Message = tok2.Message = tok3.Message = tok4.Message = msg;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void FinalizeResult(string desiredKey, List<TextTokenSubstring> result)
		{
			// Each matched pair character left in the list is invalid,
			// so add to the list of unmatchedPairs
			foreach (TextTokenSubstring tok in m_pairTokensFound)
			{
				if (desiredKey == string.Empty || desiredKey == tok.InventoryText)
					result.Add(tok);
			}

			m_pairTokensFound.Clear();
		}
	}

	#endregion

	#region StyleCategorizer class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	class StyleCategorizer
	{
		private List<string> m_poeticStyles;
		private List<string> m_introOutlineStyles;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StyleCategorizer(string customPoeticStyles, string customIntroOutlineStyles)
		{
			m_poeticStyles = new List<string>(
				customPoeticStyles.Split(CheckUtils.kStyleNamesDelimiter));

			m_introOutlineStyles = new List<string>(
				customIntroOutlineStyles.Split(CheckUtils.kStyleNamesDelimiter));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsPoeticStyle(string style)
		{
			return (m_poeticStyles.IndexOf(style) >= 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsIntroductionOutlineStyle(string style)
		{
			return (m_introOutlineStyles.IndexOf(style) >= 0);
		}
	}

	#endregion
}
