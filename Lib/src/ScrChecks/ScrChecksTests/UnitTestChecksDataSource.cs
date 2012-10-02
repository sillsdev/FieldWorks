using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using SILUBS.SharedScrUtils;

namespace SILUBS.ScriptureChecks
{
	/// <summary>
	/// SEE ICheckDataSource for documentation of these functions !!!!
	/// </summary>
	public class UnitTestChecksDataSource : IChecksDataSource
	{
		List<UnitTestUSFMTextToken> tokens2 = null;
		internal string m_extraWordFormingCharacters = String.Empty;

		Dictionary<string, string> parameterValues = new Dictionary<string, string>();
		string text;

		public UnitTestChecksDataSource()
		{
		}

		public string GetParameterValue(string key)
		{
			string value;
			if (!parameterValues.TryGetValue(key, out value))
				value = "";

			return value;
		}

		public void SetParameterValue(string key, string value)
		{
			parameterValues[key] = value;
		}

		public void Save()
		{
			//scrText.Save(scrText.Name);
		}

		public string Text
		{
			set
			{
				text = value;
				UnitTestTokenizer tokenizer = new UnitTestTokenizer();
				tokens2 = tokenizer.Tokenize(text);
			}
			get
			{
				return text;
			}
		}

		public IEnumerable<ITextToken> TextTokens()
		{
			foreach (UnitTestUSFMTextToken tok in tokens2)
			{
				yield return (ITextToken)tok;
			}
		}

		public CharacterCategorizer CharacterCategorizer
		{
			get { return new CharacterCategorizer(m_extraWordFormingCharacters, "-",
				String.Empty);
			}
		}

		public List<int> BooksPresent
		{
			get
			{
				List<int> present = new List<int>();
				present.Add(1);
				return present;
			}
		}

		public bool GetText(int bookNum, int chapterNum)
		{
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a localized version of the specified string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string GetLocalizedString(string strToLocalize)
		{
			return strToLocalize;
		}
	}
}
