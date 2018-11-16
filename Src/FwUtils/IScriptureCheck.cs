// Copyright (c) 2012-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Interface for checks that don't need to implement an inventory mode.
	/// </summary>
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

		/// <summary>
		/// Gets a number that can be used to order this check relative to other checks in the
		/// same group when displaying checks in the UI.
		/// </summary>
		float RelativeOrder { get; }

		/// <summary>
		/// A description for the check, e.g. "Searches for all occurrences of repeated words".
		/// After replacing any spaces with underscores, this can also be used as a key for
		/// looking up a localized string if the application supports localization.
		/// </summary>
		string Description { get; }
	}
}