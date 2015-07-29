// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Text;
using SILUBS.SharedScrUtils;

namespace SILUBS.ScriptureChecks
{
	#region MixedCapitalizationCheck class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class MixedCapitalizationCheck : IScrCheckInventory
	{
		private const string kValidItemsParameter = "ValidMixedCapitalization";
		private const string kInvalidItemsParameter = "InvalidMixedCapitalization";

		private IChecksDataSource m_checksDataSource;
		private CharacterCategorizer m_characterCategorizer;
		private List<TextTokenSubstring> m_mixedCapitalization;
		private string m_validItems;
		private string m_invalidItems;
		private List<string> m_validItemsList;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="checksDataSource"></param>
		/// ------------------------------------------------------------------------------------
		public MixedCapitalizationCheck(IChecksDataSource checksDataSource)
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
			set	{ m_invalidItems = (value == null ? string.Empty : value.Trim()); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string CheckName { get { return Localize("Mixed Capitalization"); } }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The unique identifier of the check. This should never be changed!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Guid CheckId { get { return StandardCheckIds.kguidMixedCapitalization; } }

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
			get { return 700; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the description for this check.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Description { get { return Localize("Checks for words with a potentially invalid mix of uppercase and lowercase letters."); } }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is the column header of the first column when you create an
		/// inventory of this type of error.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string InventoryColumnHeader
		{
			get { return Localize("Mixed Capitalization Word"); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update the parameter values for storing the valid and invalid lists in CheckDataSource
		/// and then save them. This is here because the inventory form does not know the names of
		/// the parameters that need to be saved for a given check, only the check knows this.
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
		/// Execute the check. Call 'RecordError' for every error found.
		/// </summary>
		/// <param name="toks">ITextTokens corresponding to the text to be checked.
		/// Typically this is one books worth.</param>
		/// <param name="record">Call this delegate to report each error found.</param>
		/// ------------------------------------------------------------------------------------
		public void Check(IEnumerable<ITextToken> toks, RecordErrorHandler record)
		{
			m_mixedCapitalization = GetReferences(toks, string.Empty);

			string msg = Localize("Word has mixed capitalization");

			foreach (TextTokenSubstring tts in m_mixedCapitalization)
			{
				if (!m_validItemsList.Contains(tts.ToString()))
				{
					tts.Message = msg;
					record(new RecordErrorEventArgs(tts, CheckId));
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get all instances of the item being checked in the token list passed.
		/// This includes both valid and invalid instances.
		/// This is used 1) to create an inventory of these items.
		/// To show the user all instance of an item with a specified key.
		/// 2) With a "desiredKey" in order to fetch instance of a specific
		/// item (e.g. all the places where "the" is a repeated word.
		/// </summary>
		/// <param name="tokens">Tokens for text to be scanned</param>
		/// <param name="desiredKey">If you only want instance of a specific key (e.g. one word,
		/// one punctuation pattern, one character, etc.) place it here. Empty string returns
		/// all items.</param>
		/// <returns>List of token substrings</returns>
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
			m_characterCategorizer = m_checksDataSource.CharacterCategorizer;
			ValidItems = m_checksDataSource.GetParameterValue(kValidItemsParameter);
			InvalidItems = m_checksDataSource.GetParameterValue(kInvalidItemsParameter);

			string preferredLocale =
				m_checksDataSource.GetParameterValue("PreferredLocale") ?? string.Empty;

			m_mixedCapitalization = new List<TextTokenSubstring>();
			ProcessMixedCapitalization processor =
				new ProcessMixedCapitalization(m_checksDataSource, m_mixedCapitalization);

			foreach (ITextToken tok in tokens)
			{
				if ((tok.Locale ?? string.Empty) != preferredLocale)
					continue;

				foreach (WordAndPunct wap in m_characterCategorizer.WordAndPuncts(tok.Text))
					processor.ProcessWord(tok, wap, desiredKey);
			}

			return m_mixedCapitalization;
		}
	}

	#endregion

	#region AWord class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class AWord
	{
		private CharacterCategorizer m_categorizer;
		private string m_text = string.Empty;
		private string m_prefix = string.Empty;
		private string m_suffix = string.Empty;
		private int m_upperCaseLetters = 0;
		private int m_lowerCaseLetters = 0;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="text"></param>
		/// <param name="categorizer"></param>
		/// ------------------------------------------------------------------------------------
		public AWord(string text, CharacterCategorizer categorizer)
		{
			this.m_text = text;
			this.m_categorizer = categorizer;

			string word = CountLettersAndReturnWordWithOnlyWordFormingCharacters(text);
			if (m_lowerCaseLetters == 0 || m_upperCaseLetters == 0)
				return;
			FindPrefixAndSuffixIfAny(word);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private string CountLettersAndReturnWordWithOnlyWordFormingCharacters(string text)
		{
			for (int i = 0; i < text.Length; i++)
			{
				char cc = text[i];
				if (m_categorizer.IsUpper(cc))
					m_upperCaseLetters++;
				if (m_categorizer.IsLower(cc))
					m_lowerCaseLetters++;
				if (m_categorizer.IsTitle(cc))
				{
					m_upperCaseLetters++;
					m_lowerCaseLetters++;
				}
				if (!m_categorizer.IsWordFormingCharacter(cc))
				{
					text = text.Remove(i, 1);
					i--;
				}
			}
			return text;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="word"></param>
		/// ------------------------------------------------------------------------------------
		private void FindPrefixAndSuffixIfAny(string word)
		{
			for (int i = 1; i < word.Length; i++)
			{
				char cc = word[i];
				if (m_categorizer.IsUpper(cc) || m_categorizer.IsTitle(cc))
				{
					m_prefix = word.Substring(0, i);
					m_suffix = word.Substring(i);
					return;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Prefix
		{
			get { return m_prefix; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Suffix
		{
			get { return m_suffix; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return "(" + m_prefix + ")" + m_text + "(" + m_suffix + ")";
		}
	}

	#endregion

	#region ProcessMixedCapitalization class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	class ProcessMixedCapitalization
	{
		private CharacterCategorizer m_categorizer;
		private List<string> m_uncapitalizedPrefixes;
		private List<string> m_capitalizedSuffixes;
		private List<string> m_capitalizedPrefixes;
		private List<TextTokenSubstring> m_result;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="checksDataSource"></param>
		/// <param name="result"></param>
		/// ------------------------------------------------------------------------------------
		public ProcessMixedCapitalization(IChecksDataSource checksDataSource,
			List<TextTokenSubstring> result)
		{
			m_categorizer = checksDataSource.CharacterCategorizer;
			m_result = result;

			m_uncapitalizedPrefixes = new List<string>(
				checksDataSource.GetParameterValue("UncapitalizedPrefixes").Split());

			m_capitalizedSuffixes =	new List<string>(
				checksDataSource.GetParameterValue("CapitalizedSuffixes").Split());

			m_capitalizedPrefixes =	new List<string>(
				checksDataSource.GetParameterValue("CapitalizedPrefixes").Split());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="tok"></param>
		/// <param name="wap"></param>
		/// <param name="desiredKey"></param>
		/// ------------------------------------------------------------------------------------
		public void ProcessWord(ITextToken tok, WordAndPunct wap, string desiredKey)
		{
			AWord word = new AWord(wap.Word, m_categorizer);

			if (word.Prefix == string.Empty && word.Suffix == string.Empty)
				return;
			if (m_uncapitalizedPrefixes.Contains(word.Prefix))
				return;
			if (m_uncapitalizedPrefixes.Contains("*" + word.Prefix[word.Prefix.Length - 1]))
				return;
			if (m_uncapitalizedPrefixes.Contains("*"))
				return;
			if (m_capitalizedSuffixes.Contains(word.Suffix))
				return;
			if (m_capitalizedPrefixes.Contains(word.Prefix))
				return;

			AddWord(tok, wap, desiredKey);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="tok"></param>
		/// <param name="wap"></param>
		/// <param name="desiredKey"></param>
		/// ------------------------------------------------------------------------------------
		private void AddWord(ITextToken tok, WordAndPunct wap, string desiredKey)
		{
			TextTokenSubstring tts = new TextTokenSubstring(tok, wap.Offset, wap.Word.Length);
			if (String.IsNullOrEmpty(desiredKey) || desiredKey == tts.InventoryText)
				m_result.Add(tts);
		}
	}

	#endregion
}
