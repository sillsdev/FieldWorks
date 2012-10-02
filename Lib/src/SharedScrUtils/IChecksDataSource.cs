using System;
using System.Collections.Generic;

namespace SILUBS.SharedScrUtils
{
	public static class CheckUtils
	{
		public const char kStyleNamesDelimiter = '\uFFFD';
	}

	public interface IChecksDataSource
	{
		/// <summary>
		/// Retrieve named checking parameter value.
		/// Checks use this to get their setup information.
		/// If the key isn't found, the implementation should return an empty string.
		/// </summary>
		/// <param name="key">Parameter name</param>
		/// <returns>Parameter value</returns>
		string GetParameterValue(string key);

		/// <summary>
		/// Set the named checking parameter value. This is normally done by
		/// the inventory display associated with the check.
		/// </summary>
		/// <param name="key">Parmameter name</param>
		/// <param name="value">Parameter value</param>
		void SetParameterValue(string key, string value);

		/// <summary>
		/// Save all changes to parameter values
		/// </summary>
		void Save();

		/// <summary>
		/// Read the text for the specified book number. Parse into Tokens.
		/// The tokens are accessed via the TextTokens() method.
		/// We split this operation into two parts since we often want to create
		/// the tokens list once and then present them to several different checks.
		/// REVIEW: Rather than splitting this in this way, would it make more sense
		/// to leave the onus on the implementor to cache the list if desired?
		/// </summary>
		/// <param name="bookNum">Number of book. This may be different in different
		/// applications. This number comes from BooksPresent below.</param>
		/// <param name="chapterNum">0=read whole book, else specified chapter number</param>
		/// <returns><c>true</c> for success, <c>false</c> if no tokens are found for the
		/// requested book and chapter</returns>
		bool GetText(int bookNum, int chapterNum);

		/// <summary>
		/// Enumerate all the ITextToken's from the most recent GetText call.
		/// </summary>
		/// <returns></returns>
		// REVIEW: Why isn't this a property instead of a method?
		IEnumerable<ITextToken> TextTokens();

		/// <summary>
		/// Return a list of the book numbers present.
		/// This list is used as an argument to GetText above when retrieving the
		/// data for each book.
		/// The book number is arbitrary as long as BooksPresent/GetText agree.
		/// This list should be in ascending order (because TextInventory.AddReference
		/// can be implemented more efficiently if this is so --- and it gets called
		/// a zillion times when building a character inventory).
		/// </summary>
		List<int> BooksPresent { get; }

		/// <summary>
		/// Get information about characters and words.
		/// This is needed for platform such as Paratext which allow the user to
		/// overide some chracter info. Platform which don't allow this can mostly
		/// likely just use the distributed version of CharacterCategorizer with
		/// an empty conctructor.
		/// ISSUE: what to do about word medial punctuation, I think every application
		/// will have to have a way to specify this list.
		/// </summary>
		/// <returns>A character categorizer</returns>
		CharacterCategorizer CharacterCategorizer { get; }

		/// <summary>
		/// Returns a localized version of the specified string.
		/// </summary>
		/// <param name="strToLocalize"></param>
		/// <returns>A Localized version of strToLocalize</returns>
		string GetLocalizedString(string strToLocalize);
	}
}
