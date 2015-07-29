// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;

namespace SILUBS.SharedScrUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Interface for checks that don't need to implement an inventory mode.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IScriptureCheck
	{
		/// <summary>
		/// Execute the check.
		/// Call 'RecordError' for every error found.
		/// </summary>
		/// <param name="toks">ITextToken's corresponding to the text to be checked.
		/// Typically this is one books worth.</param>
		/// <param name="record">Call this delegate to report each error found.</param>
		void Check(IEnumerable<ITextToken> toks, RecordErrorHandler record);

		/// <summary>
		/// The full name of the check, e.g. "Repeated Words". After replacing any spaces
		/// with underscores, this can also be used as a key for looking up a localized
		/// string if the application supports localization.  If this is ever changed,
		/// DO NOT change the CheckId!
		/// </summary>
		string CheckName { get; }

		/// <summary>
		/// The unique identifier of the check. This should never be changed!
		/// </summary>
		Guid CheckId { get; }

		/// <summary>
		/// The group which the check belongs to, e.g. "Basic". After replacing any spaces
		/// with underscores, this can also be used as a key for looking up a localized
		/// string if the application supports localization.
		/// </summary>
		string CheckGroup { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a number that can be used to order this check relative to other checks in the
		/// same group when displaying checks in the UI.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		float RelativeOrder { get; }

		/// <summary>
		/// A description for the check, e.g. "Searches for all occurrences of repeated words".
		/// After replacing any spaces with underscores, this can also be used as a key for
		/// looking up a localized string if the application supports localization.
		/// </summary>
		string Description { get; }
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Interface for scripture checks that provide an inventory mode.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IScrCheckInventory : IScriptureCheck
	{
		/// <summary>
		/// The name of the basic unit that this check covers as it occurs in the
		/// inventory for this check (e.g. "Words"). Empty string if none. After
		/// replacing any spaces with underscores, this can also be used as a key
		/// for looking up a localized string if the application supports localization.
		/// </summary>
		string InventoryColumnHeader { get; }

		/// <summary>
		/// THIS REALLY OUGHT TO BE List
		/// Valid items, separated by spaces.
		/// Inventory form queries this to know how what status to give each item
		/// in the inventory. Inventory form updates this if user has changed the status
		/// of any item.
		/// </summary>
		string ValidItems { get; set; }

		/// <summary>
		/// THIS REALLY OUGHT TO BE List
		/// Invalid items, separated by spaces.
		/// Inventory form queries this to know how what status to give each item
		/// in the inventory. Inventory form updates this if user has changed the status
		/// of any item.
		/// </summary>
		string InvalidItems { get; set; }

		/// <summary>
		/// Get all instances of the item being checked in the token list passed.
		/// This includes both valid and invalid instances.
		/// This is used 1) to create an inventory of these items.
		/// To show the user all instance of an item with a specified key.
		/// 2) With a "desiredKey" in order to fetch instance of a specific
		/// item (e.g. all the places where "the" is a repeated word.
		/// </summary>
		/// <param name="tokens">Tokens for text to be scanned</param>
		/// <param name="desiredKey">If you only want instance of a specific key (e.g. one word, one punctuation pattern,
		/// one character, etc.) place it here. Empty string returns all items.</param>
		/// <returns>List of token substrings</returns>
		List<TextTokenSubstring> GetReferences(IEnumerable<ITextToken> tokens, string desiredKey);

		/// <summary>
		/// Update the parameter values for storing the valid and invalid lists in CheckDataSource
		/// and then save them. This is here because the inventory form does not know the names of
		/// the parameters that need to be saved for a given check, only the check knows this.
		/// </summary>
		void Save();
	}
}
