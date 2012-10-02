using System;
using System.Collections.Generic;
using System.Text;
using SILUBS.SharedScrUtils;

namespace SILUBS.ScriptureChecks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Checks all characters to see if they are valid
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class CharactersCheck : IScrCheckInventory
	{
		#region Constants
		private const string kValidItemsParameter = "ValidCharacters";
		private const string kInvalidItemsParameter = "InvalidCharacters";
		private const string kAlwaysValidItemsParameter = "AlwaysValidCharacters";
		#endregion

		#region Data Members
		IChecksDataSource m_checksDataSource;
		CharacterCategorizer m_categorizer;
		List<TextTokenSubstring> m_characterSequences;
		private string m_alwaysValidCharacters;
		string m_validItems;
		string m_invalidItems;
		Dictionary<string, bool> m_validItemsDictionary;
		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="CharactersCheck"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public CharactersCheck(IChecksDataSource _checksDataSource)
		{
			m_checksDataSource = _checksDataSource;
		}
		#endregion

		#region Properties
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
			get
			{
				List<string> itemsList = new List<string>(m_validItemsDictionary.Keys);
				return string.Join(" ", itemsList.ToArray());
			}
			set
			{
				m_validItemsDictionary = StringToDictionary(value);
				m_validItems = value.Trim();
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
				// REVIEW: Why doesn't this add items to the dictionary as well.
				m_invalidItems = value.Trim();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the check.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string CheckName
		{
			get { return m_checksDataSource.GetLocalizedString("Characters"); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The unique identifier of the check. This should never be changed!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Guid CheckId { get { return StandardCheckIds.kguidCharacters; } }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the group which contains this check.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string CheckGroup
		{
			get { return m_checksDataSource.GetLocalizedString("Basic"); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a number that can be used to order this check relative to other checks in the
		/// same group when displaying checks in the UI.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public float RelativeOrder
		{
			get { return 200; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the description for this check.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Description
		{
			get { return m_checksDataSource.GetLocalizedString("Checks for potentially invalid characters."); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is the column header of the first column when you create an
		/// inventory of this type of error.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string InventoryColumnHeader
		{
			get { return m_checksDataSource.GetLocalizedString("Character"); }
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Converts a string to a dictionary
		/// </summary>
		/// <param name="value">Space-delimited list of valid characters</param>
		/// <returns>
		/// Dictionary containing the items in the list passed
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private Dictionary<string, bool> StringToDictionary(string value)
		{
			Dictionary<string, bool> dict = new Dictionary<string, bool>();

			if (value.Length > 0)
			{
				// 02 JUN 2008, Phil Hopper:  Check if space is a valid character.

				if (value[0] == ' ')
					dict[value.Substring(0, 1)] = true;

				value = value.Trim();

				if (value != string.Empty)
				{
					foreach (string item in value.Split())
						dict[item] = true;
				}
			}

			return dict;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the list of default valid items.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetDefaultValidItems()
		{
			m_validItemsDictionary = new Dictionary<string, bool>();

			foreach (char cc in m_categorizer.WordFormingCharacters)
				m_validItemsDictionary[cc.ToString()] = true;

			//foreach (char cc in m_categorizer.PunctuationCharacters)
			//    m_validItemsDictionary[cc.ToString()] = true;
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
		/// Runs the Characters Scripture checks.
		/// </summary>
		/// <param name="toks">The Scripture tokens to check.</param>
		/// <param name="record">Method to record the error.</param>
		/// ------------------------------------------------------------------------------------
		public void Check(IEnumerable<ITextToken> toks, RecordErrorHandler record)
		{
			// This method is called in ScrChecksDataSource.cs - RunCheck(IScriptureCheck check)
			m_categorizer = m_checksDataSource.CharacterCategorizer;

			// Get parameters needed to run this check.
			GetParameters();

			// Find all invalid characters and place them in 'm_characterSequences'
			GetReferences(toks, string.Empty, true);

			foreach (TextTokenSubstring tts in m_characterSequences)
			{
				tts.Message = (tts.ToString().Length > 1) ?
					m_checksDataSource.GetLocalizedString("Invalid or unknown character diacritic combination") :
					m_checksDataSource.GetLocalizedString("Invalid or unknown character");

				record(new RecordErrorEventArgs(tts, CheckId));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get (invalid) character references.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<TextTokenSubstring> GetReferences(IEnumerable<ITextToken> tokens, string desiredKey)
		{
			return GetReferences(tokens, desiredKey, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get (invalid) character references.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private List<TextTokenSubstring> GetReferences(IEnumerable<ITextToken> tokens, string desiredKey,
			bool invalidCharactersOnly)
		{
			if (m_categorizer == null)
				m_categorizer = m_checksDataSource.CharacterCategorizer;

			m_characterSequences = new List<TextTokenSubstring>();
			Dictionary<string, Dictionary<string, bool>> htValidChars =
				new Dictionary<string, Dictionary<string, bool>>();
			Dictionary<string, bool> currentDictionary = null;
			string preferredLocale = m_checksDataSource.GetParameterValue("PreferredLocale") ?? string.Empty;

			foreach (ITextToken tok in tokens)
			{
				string locale = tok.Locale ?? string.Empty;

				if (tok.Text == null || (!invalidCharactersOnly && locale != preferredLocale))
					continue;

				if (!htValidChars.TryGetValue(locale, out currentDictionary))
				{
					currentDictionary = StringToDictionary(GetValidCharacters(locale));
					htValidChars.Add(locale, currentDictionary);
				}

				int offset = 0;

				foreach (string key in ParseCharacterSequences(tok.Text))
				{
					bool lookingForASpecificKey = (desiredKey != "");
					bool keyMatches = (desiredKey == key);
					bool invalidItem = false;

					if (invalidCharactersOnly)
					{
						// REVIEW (BobbydV): IndexOf causes false positives for certain
						// characters (e.g., U+0234 & U+1234). I think Contains is easier to read
						// and should work for both TE and Paratext for the "AlwaysValidCharacters"
						// list. (TomB)
						if (!m_alwaysValidCharacters.Contains(key) &&
							!currentDictionary.ContainsKey(key))
							invalidItem = true;
					}

					if ((lookingForASpecificKey && keyMatches) ||
						(!lookingForASpecificKey && !invalidCharactersOnly) ||
						(invalidCharactersOnly && invalidItem))
					{
						TextTokenSubstring tts = new TextTokenSubstring(tok, offset, key.Length);
						m_characterSequences.Add(tts);
					}

					offset += key.Length;
				}
			}

			return m_characterSequences;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the valid characters list for the specified locale.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private string GetValidCharacters(string locale)
		{
			string parameter = (string.IsNullOrEmpty(locale) ?
				kValidItemsParameter : kValidItemsParameter + "_" + locale);

			string validChars = m_checksDataSource.GetParameterValue(parameter);
			validChars = validChars ?? string.Empty;

			return validChars;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Parses a string into character sequences.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IEnumerable<string> ParseCharacterSequences(string text)
		{
			string key = "";
			bool diacricsFollow = m_categorizer.DiacriticsFollowBaseCharacters();

			foreach (char cc in text)
			{
				if (m_categorizer.IsDiacritic(cc))
				{
					if (diacricsFollow)
					{
						key += cc;
					}
					else
					{
						if (key != "") yield return key;
						key = cc.ToString();
					}
				}
				else
				{
					if (key != "") yield return key;
					key = cc.ToString();
				}
			}

			if (key != "") yield return key;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the parameters needed for this check.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void GetParameters()
		{
			string validItemsText = m_checksDataSource.GetParameterValue(kValidItemsParameter);
			if (validItemsText == null || validItemsText.Trim() == "")
				SetDefaultValidItems();
			else
				ValidItems = validItemsText;

			m_alwaysValidCharacters = m_checksDataSource.GetParameterValue(kAlwaysValidItemsParameter);
			if (String.IsNullOrEmpty(m_alwaysValidCharacters))
				m_alwaysValidCharacters = " \r\n*+\\0123456789";

			InvalidItems = m_checksDataSource.GetParameterValue(kInvalidItemsParameter);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates an inventory of the tokens.
		/// </summary>
		/// <param name="bookNum">The book number.</param>
		/// <param name="inventory">The inventory.</param>
		/// <param name="tokens">The tokens.</param>
		/// ------------------------------------------------------------------------------------
		public void InventoryTokens(int bookNum, TextInventory inventory,
			IEnumerable<ITextToken> tokens)
		{
			foreach (ITextToken tok in tokens)
			{
				foreach (string key in ParseCharacterSequences(tok.Text))
				{
					// Don't inventory spaces, lf, cr.
					if (key == " " || key == "\r" || key == "\n")
						continue;

					inventory.GetValue(key).AddReference(bookNum);
				}
			}
		}
	}
}
