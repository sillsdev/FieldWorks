// ---------------------------------------------------------------------------------------------
// Copyright (c) 2008-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: TestChecksDataSource.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using SILUBS.SharedScrUtils;

namespace SILUBS.ScriptureChecks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test Checks data source
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TestChecksDataSource : IChecksDataSource
	{
		private Dictionary<string, string> m_parameters = new Dictionary<string, string>();
		internal List<ITextToken> m_tokens = new List<ITextToken>();

		#region IChecksDataSource Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the books present.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<int> BooksPresent
		{
			get { return new List<int>(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the character categorizer.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public CharacterCategorizer CharacterCategorizer
		{
			get { return new CharacterCategorizer(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the parameter value.
		/// </summary>
		/// <param name="key">The key.</param>
		/// ------------------------------------------------------------------------------------
		public string GetParameterValue(string key)
		{
			string value;

			if (m_parameters.TryGetValue(key, out value))
				return value;

			if (key.Contains("ValidCharacters"))
				return value;

			return string.Empty;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the text.
		/// </summary>
		/// <param name="bookNum">The book num.</param>
		/// <param name="chapterNum">The chapter num.</param>
		/// ------------------------------------------------------------------------------------
		public bool GetText(int bookNum, int chapterNum)
		{
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the localized name of the check.
		/// </summary>
		/// <param name="scrCheckName">Name of the check.</param>
		/// ------------------------------------------------------------------------------------
		public string GetUiCheckName(string scrCheckName)
		{
			return scrCheckName;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the localized name of the inventory column header.
		/// </summary>
		/// <param name="scrCheckUnit">The check unit.</param>
		/// ------------------------------------------------------------------------------------
		public string GetUiInventoryColumnHeader(string scrCheckUnit)
		{
			return scrCheckUnit;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// No-op.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Save()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the parameter value.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="value">The value.</param>
		/// ------------------------------------------------------------------------------------
		public void SetParameterValue(string key, string value)
		{
			m_parameters[key] = value;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an enumarable thingy to enumerate the tokens.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IEnumerable<ITextToken> TextTokens()
		{
			return m_tokens;
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

		#endregion
	}
}
