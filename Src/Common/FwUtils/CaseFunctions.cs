// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: CaseFunctions.cs
// Responsibility: FW Team
// ---------------------------------------------------------------------------------------------
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// CaseFunctions provides a set of case conversion functions derived from the ICU
	/// library. They should be used in preference to the built-in .NET case functions
	/// because they will properly handle PUA characters for which the user has defined
	/// case properties and conversions.
	///
	/// The constructor takes an icuLocale argument, usually obtained from a writing system.
	/// </summary>
	public class CaseFunctions
	{
		private readonly string m_icuLocale;

		/// <summary>
		/// Make one for the specified locale (which may be null, default, or empty, root).
		/// </summary>
		/// <param name="icuLocale"></param>
		public CaseFunctions(string icuLocale)
		{
			m_icuLocale = icuLocale;
		}

		/// <summary>
		/// Retrieve the locale used to create it.
		/// </summary>
		public string IcuLocale
		{
			get { return m_icuLocale; }
		}

		/// <summary>
		/// Convert string to lower case equivalent.
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public string ToLower(string input)
		{
			return Icu.ToLower(input, m_icuLocale);
		}
		/// <summary>
		/// Convert string to title case equivalent.
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public string ToTitle(string input)
		{
			return Icu.ToTitle(input, m_icuLocale);
		}
		/// <summary>
		/// Convert string to upper case equivalent.
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public string ToUpper(string input)
		{
			return Icu.ToUpper(input, m_icuLocale);
		}

		/// <summary>
		/// Convert all-lower-case string to title, otherwise to lower
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public string SwitchTitleAndLower(string input)
		{
			if (StringCase(input) == StringCaseStatus.allLower)
				return ToTitle(input);

			return ToLower(input);
		}

		/// <summary>
		/// Return an enumeration member indicating whether the string is
		/// all lower case, title case, or something else.
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public StringCaseStatus StringCase(string input)
		{
			if (input == null)
				return StringCaseStatus.allLower;
			string lower = ToLower(input);
			if (lower == input)
				return StringCaseStatus.allLower;
			string title = ToTitle(input);
			if (title == input)
				return StringCaseStatus.title;

			// NOTE: this case should be handled by the previous check, ICU should handle surrogate pairs properly
			// If we start with a surrogate pair, title can be achieved with TWO characters that don't match.
			//if (lower.Length > 1 && input.Length > 1 && Surrogates.IsLeadSurrogate(input[0]) && lower.Substring(2) == input.Substring(2))
			//    return StringCaseStatus.title;

			return StringCaseStatus.mixed;
		}
	}
	/// <summary>
	/// Possible status values for StringCase function.
	/// </summary>
	public enum StringCaseStatus
	{
		/// <summary>
		/// every character is lower case (includes empty string special case)
		/// </summary>
		allLower,
		/// <summary>
		/// First letter is upper, all others lower. For now this includes a one-char upper-case string.
		/// Longer title strings ("The Fellowship of the Ring") will be classified mixed.
		/// </summary>
		title,
		/// <summary>
		/// Some letter other than (but not excluding) the first is upper. For now this includes all-upper.
		/// </summary>
		mixed,
	}
}
