// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Covers which FW_AVALONIA values count as opting in to the New UI. The rule is a pure function,
	/// so nothing here reads or writes the process environment.
	/// </summary>
	[TestFixture]
	public class UIModeGatesTests
	{
		[TestCase("1")]
		[TestCase("true")]
		[TestCase("TRUE")]
		[TestCase("yes")]
		[TestCase("on")]
		[TestCase(" 1 ")]
		public void IsSwitchingEnabled_TreatsAnyOtherValueAsOptedIn(string value)
		{
			Assert.That(UIModeGates.IsSwitchingEnabled(value), Is.True);
		}

		[TestCase(null)]
		[TestCase("")]
		[TestCase("   ")]
		[TestCase("0")]
		[TestCase("false")]
		[TestCase("False")]
		[TestCase("off")]
		[TestCase(" off ")]
		public void IsSwitchingEnabled_FailsClosedForUnsetAndNegativeValues(string value)
		{
			Assert.That(UIModeGates.IsSwitchingEnabled(value), Is.False);
		}
	}
}
