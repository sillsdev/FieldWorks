// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// Contains a list of all the possible error messages our dialog box can have
	/// </summary>
	public enum ErrorMessage
	{
		/// <summary>No error</summary>
		none,
		/// <summary> Codepoint is too short in length (3&lt;length&lt;7) </summary>
		shortCodepoint,
		/// <summary> A name must be provided </summary>
		emptyName,
		/// <summary>Numeric digits may not be negative</summary>
		numericDashDigit,
		/// <summary>Numeric digits may not be fractions</summary>
		numericSlashDigit,
		/// <summary>Numeric digits may not contain the '.'</summary>
		numericDotDigit,
		/// <summary>The fraction is malformed</summary>
		numericMalformedFraction,
		/// <summary>The codepoint is outside the PUA range</summary>
		outsidePua,
		/// <summary>The codepoint is within the Surrogate range</summary>
		inSurrogateRange,
		/// <summary>Zero is not a valid code point</summary>
		zeroCodepoint,
		/// <summary>Codepoint length must be 4-6 digits</summary>
		longCodepoint,
		/// <summary>If there is a decomposition type, you need a decomposition</summary>
		mustEnterDecomp,
		/// <summary>Codepoint is not a valid 21-bit Unicode code point ranging from 0 through 10FFFF</summary>
		outsideRange
	}
}