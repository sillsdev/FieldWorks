// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: AssertEx.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.Test.TestUtils
{
	/// <summary>
	/// Summary description for AssertEx.
	/// </summary>
	public class AssertEx
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///  No public constructor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private AssertEx()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that the given run of the given ITsString contains the specified text and
		/// properties. Also checks that the TSS is in normal form decomposed.
		/// </summary>
		/// <param name="tss">The ITsString to test</param>
		/// <param name="iRun">Zero-based run index to check</param>
		/// <param name="expectedText">Expected contents of run</param>
		/// <param name="expectedCharStyle">Expected character style name, or null if expecting
		/// default paragraph character props</param>
		/// <param name="expectedWs">Expected writing system for the run</param>
		/// ------------------------------------------------------------------------------------
		public static void RunIsCorrect(ITsString tss, int iRun, string expectedText,
			string expectedCharStyle, int expectedWs)
		{
			RunIsCorrect(tss, iRun, expectedText, expectedCharStyle, expectedWs, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that the given run of the given ITsString contains the specified text and
		/// properties.
		/// </summary>
		/// <param name="tss">The ITsString to test</param>
		/// <param name="iRun">Zero-based run index to check</param>
		/// <param name="expectedText">Expected contents of run</param>
		/// <param name="expectedCharStyle">Expected character style name, or null if expecting
		/// default paragraph character props</param>
		/// <param name="expectedWs">Expected writing system for the run</param>
		/// <param name="fExpectNFD">Pass <c>true</c> to make sure that TSS is in normal
		/// form decomposed (which it probably should be if it has been saved to the DB); pass
		/// <c>false</c> if the string is not expected to be decomposed.</param>
		/// ------------------------------------------------------------------------------------
		public static void RunIsCorrect(ITsString tss, int iRun, string expectedText,
			string expectedCharStyle, int expectedWs, bool fExpectNFD)
		{
			Assert.AreEqual(fExpectNFD,
				tss.get_IsNormalizedForm(FwNormalizationMode.knmNFD));

			// If both strings are null then they're equal and there's nothing else to compare.
			if (expectedText == null)
			{
				Assert.IsNull(tss.Text);
				return;
			}

			// If both strings are 0-length, then they're equal; otherwise compare them.
			if (expectedText.Length == 0)
				Assert.AreEqual(0, tss.Length);
			else
			{
				// compare strings
				// apparently IndexOf performs Unicode normalization.
				if (expectedText.IndexOf(tss.get_RunText(iRun), StringComparison.Ordinal) != 0)
				{
					Assert.Fail("Run " + iRun + " text differs. Expected <" +
						expectedText + "> but was <" + tss.get_RunText(iRun) + ">");
				}
			}

			ITsTextProps ttp1 = StyleUtils.CharStyleTextProps(expectedCharStyle, expectedWs);
			ITsTextProps ttp2 = tss.get_Properties(iRun);
			string sWhy;
			if (!TsTextPropsHelper.PropsAreEqual(ttp1, ttp2, out sWhy))
				Assert.Fail(sWhy);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares two ITsStrings to each other
		/// </summary>
		/// <param name="tssExpected"></param>
		/// <param name="tss"></param>
		/// ------------------------------------------------------------------------------------
		public static void AreTsStringsEqual(ITsString tssExpected, ITsString tss)
		{
			AreTsStringsEqual(tssExpected, tss, string.Empty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares two ITsStrings to each other
		/// </summary>
		/// <param name="tssExpected">The ITsString expected.</param>
		/// <param name="tss">The actual ITsString.</param>
		/// <param name="message">The message to insert before the difference explanation.</param>
		/// ------------------------------------------------------------------------------------
		public static void AreTsStringsEqual(ITsString tssExpected, ITsString tss, string message)
		{
			string sWhy;
			if (!TsStringHelper.TsStringsAreEqual(tssExpected, tss, out sWhy))
				Assert.Fail(string.IsNullOrEmpty(message) ? sWhy : message + ": " + sWhy);
		}
	}
}
