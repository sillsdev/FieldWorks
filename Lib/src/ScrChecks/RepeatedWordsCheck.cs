using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using SILUBS.SharedScrUtils;

// NOT Paratext dependendent

namespace SILUBS.ScriptureChecks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Check to detect repeated words
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class RepeatedWordsCheck : IScrCheckInventory
	{
		IChecksDataSource m_checksDataSource;
		CharacterCategorizer characterCategorizer;

		List<TextTokenSubstring> m_repeatedWords;
		List<string> goodWords = new List<string>();
		string validItems;
		string invalidItems;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="RepeatedWordsCheck"/> class.
		/// </summary>
		/// <param name="_checksDataSource">The checks data source.</param>
		/// ------------------------------------------------------------------------------------
		public RepeatedWordsCheck(IChecksDataSource checksDataSource)
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
		/// Maintain string containing validly repeatable words.
		/// Also keep this a List<string> in goodWords
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ValidItems {
			get { return validItems; }
			set
			{
				validItems = value.Trim();
				goodWords = new List<string>();
				if (validItems != "")
					goodWords = new List<string>(validItems.Split());
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
			get { return invalidItems; }
			set { invalidItems = value.Trim(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns name of check for use in UI.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string CheckName { get { return Localize("Repeated Words"); } }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The unique identifier of the check. This should never be changed!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Guid CheckId { get { return StandardCheckIds.kguidRepeatedWords; } }

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
			get { return 800; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the description for this check.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Description { get { return Localize("Checks for repeated words."); } }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is the column header of the first column when you create an
		/// inventory of this type of error.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string InventoryColumnHeader
		{
			get { return Localize("Words"); }
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
			m_checksDataSource.SetParameterValue("RepeatableWords", validItems);
			m_checksDataSource.SetParameterValue("NonRepeatableWords", invalidItems);
			m_checksDataSource.Save();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find all repeated words. Ignore any found in 'validItemsList'. Call RecordError
		/// delegate whenever any other repeated key is found.
		/// </summary>
		/// <param name="toks">ITextToken's corresponding to the text to be checked.
		/// Typically this is one books worth.</param>
		/// <param name="record">Call this delegate to report each error found.</param>
		/// ------------------------------------------------------------------------------------
		public void Check(IEnumerable<ITextToken> toks, RecordErrorHandler record)
		{
			// Find all repeated words. Put them in 'm_repeatedWords'.
			GetReferences(toks, string.Empty);

			string msg = Localize("Repeated word");

			foreach (TextTokenSubstring tts in m_repeatedWords)
			{
				if (!goodWords.Contains(tts.ToString()))
				{
					tts.Message = msg;
					record(new RecordErrorEventArgs(tts, CheckId));
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a list if TextTokenSubstrings conataining the references and character offsets
		/// where repeated words occur.
		/// </summary>
		/// <param name="tokens">The tokens (from the data source) to check for repeated words.
		/// </param>
		/// <param name="_desiredKey">If looking for occurrences of a specific repeated word,
		/// set this to be that word; otherwise pass an empty string.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public List<TextTokenSubstring> GetReferences(IEnumerable<ITextToken> tokens, string desiredKey)
		{
#if DEBUG
			List<ITextToken> AllTokens = new List<ITextToken>(tokens);
#endif
			characterCategorizer = m_checksDataSource.CharacterCategorizer;
			// Get a string of words that may be validly repeated.
			// Words are separated by blanks.
			ValidItems = m_checksDataSource.GetParameterValue("RepeatableWords");
			// List of words that are known to be not repeatable.
			InvalidItems = m_checksDataSource.GetParameterValue("NonRepeatableWords");

			TextType prevTextType = TextType.Other;
			m_repeatedWords = new List<TextTokenSubstring>();
			ProcessRepeatedWords bodyProcessor =
				new ProcessRepeatedWords(characterCategorizer, m_repeatedWords, desiredKey);
			ProcessRepeatedWords noteProcessor =
				new ProcessRepeatedWords(characterCategorizer, m_repeatedWords, desiredKey);

			foreach (ITextToken tok in tokens)
			{
				if (tok.IsParagraphStart)
				{
					noteProcessor.Reset();
					bodyProcessor.Reset();
				}

				if (tok.TextType == TextType.Note)
				{
					if (tok.IsNoteStart)
						noteProcessor.Reset();
					noteProcessor.ProcessToken(tok);
				}

				// When we leave a caption, we start over checking for repeated words.
				// A caption is a start of a paragraph, so we already start over
				// when we encounter a picture caption.
				if (prevTextType == TextType.PictureCaption)
					noteProcessor.Reset();

				if (tok.TextType == TextType.Verse || tok.TextType == TextType.Other)
				{
					noteProcessor.Reset();
					bodyProcessor.ProcessToken(tok);
				}

				if (tok.TextType == TextType.ChapterNumber)
					bodyProcessor.Reset();

				prevTextType = tok.TextType;
			}

			return m_repeatedWords;
		}
	}

	class ProcessRepeatedWords
	{
		CharacterCategorizer characterCategorizer;
		List<TextTokenSubstring> result;
		string desiredKey;
		string prevWord = "";

		public ProcessRepeatedWords(CharacterCategorizer characterCategorizer,
			List<TextTokenSubstring> result, string desiredKey)
		{
			this.characterCategorizer = characterCategorizer;
			this.result = result;
			this.desiredKey = Normalize(desiredKey);
		}

		public void ProcessToken(ITextToken tok)
		{
			foreach (WordAndPunct wap in characterCategorizer.WordAndPuncts(tok.Text))
				ProcessWord(tok, wap);
		}

		private void ProcessWord(ITextToken tok, WordAndPunct wap)
		{
			if (wap.Word == "")
				return;

			string nextWord = Normalize(wap.Word);

			if (prevWord == nextWord)
				AddWord(tok, wap);

			prevWord = nextWord;

			// If there are characters (such as quotes) between words,
			// then two words are not considered repeating, even if they are identical
			foreach (char cc in wap.Punct)
			{
				if (!char.IsWhiteSpace(cc))
				{
					Reset();
					break;
				}
			}
		}

		private string Normalize(string word)
		{
			string text = characterCategorizer.ToLower(word);
			return text;
		}

		private void AddWord(ITextToken tok, WordAndPunct wap)
		{
			TextTokenSubstring tts = new TextTokenSubstring(tok, wap.Offset, wap.Word.Length);
			if (desiredKey == "" || desiredKey == tts.InventoryText)
				result.Add(tts);
		}

		public void Reset()
		{
			prevWord = "";
		}
	}
}
