// Copyright (c) 2010-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using LanguageExplorer.Filters;
using NUnit.Framework;
using SIL.LCModel.Core.Text;

namespace LanguageExplorerTests.Filters
{
	/// <summary>
	/// Test the logic in the DateTimeMatcher.  (lots more tests could be written...)
	/// </summary>
	[TestFixture]
	[SetCulture("en-US")]
	public class DateTimeMatcherTests
	{
		public const int WsDummy = 987654321;

		/// <summary>
		/// Tests the method Matches() with DateMatchType.Before and GenDate data.
		/// </summary>
		[Test]
		public void MatchBefore()
		{
			/*
			 * For example, given a filter "Before Jan 17 1990", records labeled Jan 16 1990,
			 * Dec 1989, 1989, or Before or About any of those, will always match. And Jan 18
			 * 1990, Feb 1990, 1991, or After or About any of those, will always fail.  However,
			 * Jan 1990 (which could be before or after the 17th), 1990 (could be before or
			 * after Jan 17), Before 2001 (could be also before 1990), After 1900 (could also be
			 * after 1990), and the like, will match only if the user asks for ambiguous
			 * matches.
			 */
			var matchBefore = new DateTimeMatcher(new DateTime(1990, 1, 17, 0, 0, 0), new DateTime(1990, 1, 17, 23, 59, 59), DateMatchType.Before);
			matchBefore.HandleGenDate = true;

			var fMatch = matchBefore.Matches(TsStringUtils.MakeString("January 18, 1990", WsDummy));
			Assert.IsFalse(fMatch, "1/18/90 not before 1/17/90");

			fMatch = matchBefore.Matches(TsStringUtils.MakeString("January 17, 1990", WsDummy));
			Assert.IsTrue(fMatch, "1/17/90 before (or on) 1/17/90");

			fMatch = matchBefore.Matches(TsStringUtils.MakeString("January 16, 1990", WsDummy));
			Assert.IsTrue(fMatch, "1/16/90 before 1/17/90");

			fMatch = matchBefore.Matches(TsStringUtils.MakeString("February, 1990", WsDummy));
			Assert.IsFalse(fMatch, "2/90 not before 1/17/90");

			fMatch = matchBefore.Matches(TsStringUtils.MakeString("1990", WsDummy));
			Assert.IsFalse(fMatch, "1990 not before 1/17/90");

			fMatch = matchBefore.Matches(TsStringUtils.MakeString("About 1990", WsDummy));
			Assert.IsFalse(fMatch, "About 1990 not before 1/17/90");

			fMatch = matchBefore.Matches(TsStringUtils.MakeString("Before 1990", WsDummy));
			Assert.IsTrue(fMatch, "Before 1990 before 1/17/90");

			fMatch = matchBefore.Matches(TsStringUtils.MakeString("After 1990", WsDummy));
			Assert.IsFalse(fMatch, "After 1990 not before 1/17/90");

			fMatch = matchBefore.Matches(TsStringUtils.MakeString("Before 1991", WsDummy));
			Assert.IsFalse(fMatch, "Before 1991 not before 1/17/90");

			fMatch = matchBefore.Matches(TsStringUtils.MakeString("January, 1990", WsDummy));
			Assert.IsFalse(fMatch, "January, 1990 not before 1/17/90");

			fMatch = matchBefore.Matches(TsStringUtils.MakeString("Before January, 1990", WsDummy));
			Assert.IsTrue(fMatch, "Before January, 1990 before 1/17/90");

			fMatch = matchBefore.Matches(TsStringUtils.MakeString("Before 2001", WsDummy));
			Assert.IsFalse(fMatch, "Before 2001 not before 1/17/90");

			fMatch = matchBefore.Matches(TsStringUtils.MakeString("After 1900", WsDummy));
			Assert.IsFalse(fMatch, "After 1900 not (necessarily) before 1/17/90");

			matchBefore.UnspecificMatching = true;

			fMatch = matchBefore.Matches(TsStringUtils.MakeString("January 18, 1990", WsDummy));
			Assert.IsFalse(fMatch, "1/18/90 not before 1/17/90");

			fMatch = matchBefore.Matches(TsStringUtils.MakeString("January 17, 1990", WsDummy));
			Assert.IsTrue(fMatch, "1/17/90 before (or on) 1/17/90");

			fMatch = matchBefore.Matches(TsStringUtils.MakeString("January 16, 1990", WsDummy));
			Assert.IsTrue(fMatch, "1/16/90 before 1/17/90");

			fMatch = matchBefore.Matches(TsStringUtils.MakeString("February, 1990", WsDummy));
			Assert.IsFalse(fMatch, "2/90 not before 1/17/90");

			fMatch = matchBefore.Matches(TsStringUtils.MakeString("1990", WsDummy));
			Assert.IsTrue(fMatch, "1990 possibly before 1/17/90");

			fMatch = matchBefore.Matches(TsStringUtils.MakeString("About 1990", WsDummy));
			Assert.IsTrue(fMatch, "About 1990 possibly before 1/17/90");

			fMatch = matchBefore.Matches(TsStringUtils.MakeString("Before 1990", WsDummy));
			Assert.IsTrue(fMatch, "Before 1990 before 1/17/90");

			fMatch = matchBefore.Matches(TsStringUtils.MakeString("After 1990", WsDummy));
			Assert.IsFalse(fMatch, "After 1990 not before 1/17/90");

			fMatch = matchBefore.Matches(TsStringUtils.MakeString("Before 1991", WsDummy));
			Assert.IsTrue(fMatch, "Before 1991 possibly before 1/17/90");

			fMatch = matchBefore.Matches(TsStringUtils.MakeString("January, 1990", WsDummy));
			Assert.IsTrue(fMatch, "January, 1990 possibly before 1/17/90");

			fMatch = matchBefore.Matches(TsStringUtils.MakeString("Before January, 1990", WsDummy));
			Assert.IsTrue(fMatch, "Before January, 1990 before 1/17/90");

			fMatch = matchBefore.Matches(TsStringUtils.MakeString("Before 2001", WsDummy));
			Assert.IsTrue(fMatch, "Before 2001 possibly before 1/17/90");

			fMatch = matchBefore.Matches(TsStringUtils.MakeString("After 1900", WsDummy));
			Assert.IsTrue(fMatch, "After 1900 possibly before 1/17/90");
		}

		/// <summary>
		/// Tests the method Matches() with DateMatchType.After and GenDate data.
		/// </summary>
		[Test]
		public void TestMatchAfter()
		{
			var matchAfter = new DateTimeMatcher(new DateTime(1990, 1, 17, 0, 0, 0), new DateTime(1990, 1, 17, 23, 59, 59), DateMatchType.After);
			matchAfter.HandleGenDate = true;

			var fMatch = matchAfter.Matches(TsStringUtils.MakeString("January 18, 1990", WsDummy));
			Assert.IsTrue(fMatch, "1/18/90 after 1/17/90");

			fMatch = matchAfter.Matches(TsStringUtils.MakeString("January 17, 1990", WsDummy));
			Assert.IsTrue(fMatch, "1/17/90 after (or on) 1/17/90");

			fMatch = matchAfter.Matches(TsStringUtils.MakeString("January 16, 1990", WsDummy));
			Assert.IsFalse(fMatch, "1/16/90 not after 1/17/90");

			fMatch = matchAfter.Matches(TsStringUtils.MakeString("February, 1990", WsDummy));
			Assert.IsTrue(fMatch, "2/90 after 1/17/90");

			fMatch = matchAfter.Matches(TsStringUtils.MakeString("1990", WsDummy));
			Assert.IsFalse(fMatch, "1990 not after 1/17/90");

			fMatch = matchAfter.Matches(TsStringUtils.MakeString("About 1990", WsDummy));
			Assert.IsFalse(fMatch, "About 1990 not after 1/17/90");

			fMatch = matchAfter.Matches(TsStringUtils.MakeString("Before 1990", WsDummy));
			Assert.IsFalse(fMatch, "Before 1990 not after 1/17/90");

			fMatch = matchAfter.Matches(TsStringUtils.MakeString("After 1990", WsDummy));
			Assert.IsTrue(fMatch, "After 1990 after 1/17/90");

			fMatch = matchAfter.Matches(TsStringUtils.MakeString("Before 1991", WsDummy));
			Assert.IsFalse(fMatch, "Before 1991 not (necessarily) after 1/17/90");

			fMatch = matchAfter.Matches(TsStringUtils.MakeString("January, 1990", WsDummy));
			Assert.IsFalse(fMatch, "January, 1990 not after 1/17/90");

			fMatch = matchAfter.Matches(TsStringUtils.MakeString("Before January, 1990", WsDummy));
			Assert.IsFalse(fMatch, "Before January, 1990 not after 1/17/90");

			fMatch = matchAfter.Matches(TsStringUtils.MakeString("Before 2001", WsDummy));
			Assert.IsFalse(fMatch, "Before 2001 not (necessarily) after 1/17/90");

			fMatch = matchAfter.Matches(TsStringUtils.MakeString("After 1900", WsDummy));
			Assert.IsFalse(fMatch, "After 1900 not (necessarily) after 1/17/90");

			matchAfter.UnspecificMatching = true;

			fMatch = matchAfter.Matches(TsStringUtils.MakeString("January 18, 1990", WsDummy));
			Assert.IsTrue(fMatch, "1/18/90 after 1/17/90");

			fMatch = matchAfter.Matches(TsStringUtils.MakeString("January 17, 1990", WsDummy));
			Assert.IsTrue(fMatch, "1/17/90 after (or on) 1/17/90");

			fMatch = matchAfter.Matches(TsStringUtils.MakeString("January 16, 1990", WsDummy));
			Assert.IsFalse(fMatch, "1/16/90 not after 1/17/90");

			fMatch = matchAfter.Matches(TsStringUtils.MakeString("February, 1990", WsDummy));
			Assert.IsTrue(fMatch, "2/90 after 1/17/90");

			fMatch = matchAfter.Matches(TsStringUtils.MakeString("1990", WsDummy));
			Assert.IsTrue(fMatch, "1990 possibly after 1/17/90");

			fMatch = matchAfter.Matches(TsStringUtils.MakeString("About 1990", WsDummy));
			Assert.IsTrue(fMatch, "About 1990 possibly after 1/17/90");

			fMatch = matchAfter.Matches(TsStringUtils.MakeString("Before 1990", WsDummy));
			Assert.IsFalse(fMatch, "Before 1990 not after 1/17/90");

			fMatch = matchAfter.Matches(TsStringUtils.MakeString("After 1990", WsDummy));
			Assert.IsTrue(fMatch, "After 1990 after 1/17/90");

			fMatch = matchAfter.Matches(TsStringUtils.MakeString("Before 1991", WsDummy));
			Assert.IsTrue(fMatch, "Before 1991 possibly after 1/17/90");

			fMatch = matchAfter.Matches(TsStringUtils.MakeString("January, 1990", WsDummy));
			Assert.IsTrue(fMatch, "January, 1990 possibly after 1/17/90");

			fMatch = matchAfter.Matches(TsStringUtils.MakeString("Before January, 1990", WsDummy));
			Assert.IsFalse(fMatch, "Before January, 1990 not after 1/17/90");

			fMatch = matchAfter.Matches(TsStringUtils.MakeString("Before 2001", WsDummy));
			Assert.IsTrue(fMatch, "Before 2001 possibly after 1/17/90");

			fMatch = matchAfter.Matches(TsStringUtils.MakeString("After 1900", WsDummy));
			Assert.IsTrue(fMatch, "After 1900 possibly after 1/17/90");
		}

		/// <summary>
		/// Tests the method Matches() with DateMatchType.Range and GenDate data.
		/// </summary>
		[Test]
		public void TestMatchRange()
		{
			/*
			 * Similarly, if the filter asks for dates between Feb 15, 1990 and Feb 17, 1992,
			 * then Feb 16 1990, Mar 1990, or 1991 will always match, and Feb 14 1990, Jan 1990,
			 * 1989, 1993, Before 1990, and the like will always fail. But Feb 1990, 1992,
			 * Before 2001, After 1900, and the like will match only if we want ambiguous
			 * matches.
			 */
			var matchRange = new DateTimeMatcher(new DateTime(1990, 2, 15, 0, 0, 0), new DateTime(1992, 2, 17, 23, 59, 59), DateMatchType.Range);
			matchRange.HandleGenDate = true;

			var fMatch = matchRange.Matches(TsStringUtils.MakeString("February 16, 1990", WsDummy));
			Assert.IsTrue(fMatch, "Feb 16, 1990 between 2/15/90 and 2/17/92");

			fMatch = matchRange.Matches(TsStringUtils.MakeString("March, 1990", WsDummy));
			Assert.IsTrue(fMatch, "Mar 1990 between 2/15/90 and 2/17/92");

			fMatch = matchRange.Matches(TsStringUtils.MakeString("1991", WsDummy));
			Assert.IsTrue(fMatch, "1991 between 2/15/90 and 2/17/92");

			fMatch = matchRange.Matches(TsStringUtils.MakeString("February 14, 1990", WsDummy));
			Assert.IsFalse(fMatch, "Feb 14, 1990 not between 2/15/90 and 2/17/92");

			fMatch = matchRange.Matches(TsStringUtils.MakeString("January, 1990", WsDummy));
			Assert.IsFalse(fMatch, "Jan 1990 not between 2/15/90 and 2/17/92");

			fMatch = matchRange.Matches(TsStringUtils.MakeString("1989", WsDummy));
			Assert.IsFalse(fMatch, "1989 not between 2/15/90 and 2/17/92");

			fMatch = matchRange.Matches(TsStringUtils.MakeString("1993", WsDummy));
			Assert.IsFalse(fMatch, "1993 not between 2/15/90 and 2/17/92");

			fMatch = matchRange.Matches(TsStringUtils.MakeString("Before 1990", WsDummy));
			Assert.IsFalse(fMatch, "Before 1990 not between 2/15/90 and 2/17/92");

			fMatch = matchRange.Matches(TsStringUtils.MakeString("After 1992", WsDummy));
			Assert.IsFalse(fMatch, "After 1992 not between 2/15/90 and 2/17/92");

			fMatch = matchRange.Matches(TsStringUtils.MakeString("February, 1990", WsDummy));
			Assert.IsFalse(fMatch, "Feb 1990 not (necessarily) between 2/15/90 and 2/17/92");

			fMatch = matchRange.Matches(TsStringUtils.MakeString("February, 1992", WsDummy));
			Assert.IsFalse(fMatch, "Feb 1992 not (necessarily) between 2/15/90 and 2/17/92");

			fMatch = matchRange.Matches(TsStringUtils.MakeString("1990", WsDummy));
			Assert.IsFalse(fMatch, "1990 not (necessarily) between 2/15/90 and 2/17/92");

			fMatch = matchRange.Matches(TsStringUtils.MakeString("1992", WsDummy));
			Assert.IsFalse(fMatch, "1992 not (necessarily) between 2/15/90 and 2/17/92");

			fMatch = matchRange.Matches(TsStringUtils.MakeString("Before 1992", WsDummy));
			Assert.IsFalse(fMatch, "Before 1992 not (necessarily) between 2/15/90 and 2/17/92");

			fMatch = matchRange.Matches(TsStringUtils.MakeString("Before 2001", WsDummy));
			Assert.IsFalse(fMatch, "Before 2001 not (necessarily) between 2/15/90 and 2/17/92");

			fMatch = matchRange.Matches(TsStringUtils.MakeString("After 1900", WsDummy));
			Assert.IsFalse(fMatch, "After 1900 not (necessarily) between 2/15/90 and 2/17/92");

			matchRange.UnspecificMatching = true;

			fMatch = matchRange.Matches(TsStringUtils.MakeString("February 16, 1990", WsDummy));
			Assert.IsTrue(fMatch, "Feb 16, 1990 between 2/15/90 and 2/17/92");

			fMatch = matchRange.Matches(TsStringUtils.MakeString("March, 1990", WsDummy));
			Assert.IsTrue(fMatch, "Mar 1990 between 2/15/90 and 2/17/92");

			fMatch = matchRange.Matches(TsStringUtils.MakeString("1991", WsDummy));
			Assert.IsTrue(fMatch, "1991 between 2/15/90 and 2/17/92");

			fMatch = matchRange.Matches(TsStringUtils.MakeString("February 14, 1990", WsDummy));
			Assert.IsFalse(fMatch, "Feb 14, 1990 not between 2/15/90 and 2/17/92");

			fMatch = matchRange.Matches(TsStringUtils.MakeString("January, 1990", WsDummy));
			Assert.IsFalse(fMatch, "Jan 1990 not between 2/15/90 and 2/17/92");

			fMatch = matchRange.Matches(TsStringUtils.MakeString("1989", WsDummy));
			Assert.IsFalse(fMatch, "1989 not between 2/15/90 and 2/17/92");

			fMatch = matchRange.Matches(TsStringUtils.MakeString("1993", WsDummy));
			Assert.IsFalse(fMatch, "1993 not between 2/15/90 and 2/17/92");

			fMatch = matchRange.Matches(TsStringUtils.MakeString("Before 1990", WsDummy));
			Assert.IsFalse(fMatch, "Before 1990 not between 2/15/90 and 2/17/92");

			fMatch = matchRange.Matches(TsStringUtils.MakeString("After 1992", WsDummy));
			Assert.IsFalse(fMatch, "After 1992 not between 2/15/90 and 2/17/92");

			fMatch = matchRange.Matches(TsStringUtils.MakeString("February, 1990", WsDummy));
			Assert.IsTrue(fMatch, "Feb 1990 possibly between 2/15/90 and 2/17/92");

			fMatch = matchRange.Matches(TsStringUtils.MakeString("February, 1992", WsDummy));
			Assert.IsTrue(fMatch, "Feb 1992 possibly between 2/15/90 and 2/17/92");

			fMatch = matchRange.Matches(TsStringUtils.MakeString("1990", WsDummy));
			Assert.IsTrue(fMatch, "1990 possibly between 2/15/90 and 2/17/92");

			fMatch = matchRange.Matches(TsStringUtils.MakeString("1992", WsDummy));
			Assert.IsTrue(fMatch, "1992 possibly between 2/15/90 and 2/17/92");

			fMatch = matchRange.Matches(TsStringUtils.MakeString("Before 1992", WsDummy));
			Assert.IsTrue(fMatch, "Before 1992 possibly between 2/15/90 and 2/17/92");

			fMatch = matchRange.Matches(TsStringUtils.MakeString("Before 2001", WsDummy));
			Assert.IsTrue(fMatch, "Before 2001 possibly between 2/15/90 and 2/17/92");

			fMatch = matchRange.Matches(TsStringUtils.MakeString("After 1900", WsDummy));
			Assert.IsTrue(fMatch, "After 1900 possibly between 2/15/90 and 2/17/92");
		}
	}
}