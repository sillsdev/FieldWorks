//-------------------------------------------------------------------------------------------------
// <copyright file="String.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//
//    The use and distribution terms for this software are covered by the
//    Common Public License 1.0 (http://opensource.org/licenses/cpl.php)
//    which can be found in the file CPL.TXT at the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by
//    the terms of this license.
//
//    You must not remove this notice, or any other, from this software.
// </copyright>
//
// <summary>
// Implements some useful static methods on the String class that were introduced with the
// .NET 2.0 Framework.
// </summary>
//-------------------------------------------------------------------------------------------------

#if !USE_NET20_FRAMEWORK

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.ComponentModel;
	using System.Globalization;

	/// <summary>
	/// Specifies the culture, case, and sort rules to be used by certain overloads of the
	/// <see cref="String.Compare"/> and <see cref="String.Equals"/> methods.
	/// </summary>
	public enum StringComparison
	{
		/// <summary>
		/// Compare strings using culture-sensitive sort rules and the current culture.
		/// </summary>
		CurrentCulture,

		/// <summary>
		/// Compare strings using culture-sensitive sort rules, the current culture, and ignoring the case of the strings being compared.
		/// </summary>
		CurrentCultureIgnoreCase,

		/// <summary>
		/// Compare strings using culture-sensitive sort rules and the invariant culture.
		/// </summary>
		InvariantCulture,

		/// <summary>
		/// Compare strings using culture-sensitive sort rules, the invariant culture, and ignoring the case of the strings being compared.
		/// </summary>
		InvariantCultureIgnoreCase,

		/// <summary>
		/// Compare strings using ordinal sort rules.
		/// </summary>
		Ordinal,

		///// <summary>
		///// Compare strings using ordinal sort rules and ignoring the case of the strings being compared.
		///// </summary>
		// OrdinalIgnoreCase, This is way to hard (and error-prone) to duplicate in the 1.1 Framework.
	}

	/// <summary>
	/// Implements some useful static methods on the String class that were introduced with the
	/// .NET 2.0 Framework and that we can safely replicate with the 1.1 Framework.
	/// </summary>
	/// <remarks>
	/// The name is the same as System.String so that when compiling against the 2.0 Framework,
	/// none of the calling code has to change. Note that this entire class is not even defined
	/// if we're compiling against the 2.0 Framework.
	/// </remarks>
	public sealed class String
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		/// <summary>
		/// Represents the empty string. This field is read-only.
		/// </summary>
		public static readonly string Empty = System.String.Empty;
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Prevent direct instantiation of this class.
		/// </summary>
		private String()
		{
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Compares two specified <see cref="System.String"/> objects. A parameter
		/// specifies whether the comparison uses the current or invariant culture, honors or
		/// ignores case, and uses word or ordinal sort rules.
		/// </summary>
		/// <param name="strA">The first <see cref="System.String"/> object.</param>
		/// <param name="strB">The second <b>String</b> object.</param>
		/// <param name="comparisonType">One of the <see cref="StringComparison"/> values.</param>
		/// <returns>
		/// A 32-bit signed integer indicating the lexical relationship between the two comparands.
		/// <list type="table">
		/// <listheader><term>Value</term><description>Condition</description></listheader>
		/// <item><term>Less than zero</term><description><paramref name="strA"/> is less than <paramref name="strB"/>.</description></item>
		/// <item><term>Zero</term><description><paramref name="strA"/> equals <paramref name="strB"/>.</description></item>
		/// <item><term>Greater than zero</term><description><paramref name="strA"/> is greater than <paramref name="strB"/>.</description></item>
		/// </list>
		/// </returns>
		public static int Compare(string strA, string strB, StringComparison comparisonType)
		{
			switch (comparisonType)
			{
				case StringComparison.CurrentCulture:
					return System.String.Compare(strA, strB, false, CultureInfo.CurrentCulture);

				case StringComparison.CurrentCultureIgnoreCase:
					return System.String.Compare(strA, strB, true, CultureInfo.CurrentCulture);

				case StringComparison.InvariantCulture:
					return System.String.Compare(strA, strB, false, CultureInfo.InvariantCulture);

				case StringComparison.InvariantCultureIgnoreCase:
					return System.String.Compare(strA, strB, true, CultureInfo.InvariantCulture);

				case StringComparison.Ordinal:
					return System.String.CompareOrdinal(strA, strB);

				default:
					throw new InvalidEnumArgumentException("comparisonType", (int)comparisonType, typeof(StringComparison));
			}
		}

		/// <summary>
		/// Compares two specified <see cref="System.String"/> objects. A parameter
		/// specifies whether the comparison uses the current or invariant culture, honors or
		/// ignores case, and uses word or ordinal sort rules.
		/// </summary>
		/// <param name="strA">The first <see cref="System.String"/> object.</param>
		/// <param name="indexA">The position of the substring within <paramref name="strA"/>.</param>
		/// <param name="strB">The second <b>String</b> object.</param>
		/// <param name="indexB">The position of the substring within <paramref name="strB"/>.</param>
		/// <param name="length">The maximum number of characters in the substrings to compare.</param>
		/// <param name="comparisonType">One of the <see cref="StringComparison"/> values.</param>
		/// <returns>
		/// A 32-bit signed integer indicating the lexical relationship between the two comparands.
		/// <list type="table">
		/// <listheader><term>Value</term><description>Condition</description></listheader>
		/// <item><term>Less than zero</term><description>The substring in <paramref name="strA"/> is less than the substring in <paramref name="strB"/>.</description></item>
		/// <item><term>Zero</term><description>The substrings are equal, or the <paramref name="length"/> parameter is zero.</description></item>
		/// <item><term>Greater than zero</term><description>The substring in <paramref name="strA"/> is greater than the substring in <paramref name="strB"/>.</description></item>
		/// </list>
		/// </returns>
		public static int Compare(string strA, int indexA, string strB, int indexB, int length, StringComparison comparisonType)
		{
			switch (comparisonType)
			{
				case StringComparison.CurrentCulture:
					return System.String.Compare(strA, indexA, strB, indexB, length, false, CultureInfo.CurrentCulture);

				case StringComparison.CurrentCultureIgnoreCase:
					return System.String.Compare(strA, indexA, strB, indexB, length, true, CultureInfo.CurrentCulture);

				case StringComparison.InvariantCulture:
					return System.String.Compare(strA, indexA, strB, indexB, length, false, CultureInfo.InvariantCulture);

				case StringComparison.InvariantCultureIgnoreCase:
					return System.String.Compare(strA, indexA, strB, indexB, length, true, CultureInfo.InvariantCulture);

				case StringComparison.Ordinal:
					return System.String.CompareOrdinal(strA, indexA, strB, indexB, length);

				default:
					throw new InvalidEnumArgumentException("comparisonType", (int)comparisonType, typeof(StringComparison));
			}
		}

		/// <summary>
		/// Determines whether two specified String objects have the same value. A parameter
		/// specifies the culture, case, and sort rules used in the comparison.
		/// </summary>
		/// <param name="a">A <see cref="System.String"/> object or <see langword="null"/>.</param>
		/// <param name="b">A <b>System.String</b> object or <see langword="null"/>.</param>
		/// <param name="comparisonType">One of the <see cref="StringComparison"/> values.</param>
		/// <returns>true if the value of the <paramref name="a"/> parameter is equal to the value of the <paramref name="b"/> parameter; otherwise, false.</returns>
		public static bool Equals(string a, string b, StringComparison comparisonType)
		{
			return (Compare(a, b, comparisonType) == 0);
		}

		/// <summary>
		/// Replaces the format item in a specified string with the text equivalent of the value of a corresponding
		/// <see cref="Object"/> instance in a specified array. A specified parameter supplies culture-specific
		/// formatting information.
		/// </summary>
		/// <param name="provider">An <see cref="IFormatProvider"/> that supplies culture-specific formatting information.</param>
		/// <param name="format">A string containing zero or more format items.</param>
		/// <param name="args">An <see cref="Object"/> array containing zero or more objects to format.</param>
		/// <returns>A copy of <paramref name="format"/> in which the format items have been replaced by the string equivalent of the corresponding instances of <b>Object</b> in <paramref name="args"/>.</returns>
		public static string Format(IFormatProvider provider, string format, params object[] args)
		{
			return System.String.Format(provider, format, args);
		}

		/// <summary>
		/// Indicates whether the specified <see cref="String"/> object is <see langword="null"/> or an <see cref="String.Empty">Empty</see> string.
		/// </summary>
		/// <param name="value">A <see cref="String"/> reference.</param>
		/// <returns>true if the <paramref name="value"/> parameter is <see langword="null"/> on an empty string (""); otherwise, false.</returns>
		public static bool IsNullOrEmpty(string value)
		{
			return (value == null || value.Length == 0);
		}
		#endregion
	}
}
#endif // !USE_NET20_FRAMEWORK
