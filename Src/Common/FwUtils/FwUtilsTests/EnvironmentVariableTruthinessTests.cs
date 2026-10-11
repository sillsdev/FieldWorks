// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Covers which environment-variable values count as opting in to a feature.
	/// </summary>
	[TestFixture]
	public class EnvironmentVariableTruthinessTests
	{
		private const string TestVariable = "FW_TEST_TRUTHINESS";

		[TestCase("1")]
		[TestCase("true")]
		[TestCase("TRUE")]
		[TestCase("yes")]
		[TestCase("on")]
		[TestCase(" 1 ")]
		public void IsTrueValue_TreatsAnyOtherValueAsOptedIn(string value)
		{
			Assert.That(EnvironmentVariables.IsTrueValue(value), Is.True);
		}

		[TestCase(null)]
		[TestCase("")]
		[TestCase("   ")]
		[TestCase("0")]
		[TestCase("false")]
		[TestCase("False")]
		[TestCase("off")]
		[TestCase(" off ")]
		public void IsTrueValue_FailsClosedForUnsetAndNegativeValues(string value)
		{
			Assert.That(EnvironmentVariables.IsTrueValue(value), Is.False);
		}

		[Test]
		public void IsTrue_ReadsTheProcessEnvironmentAndDefaultsToOff()
		{
			var original = Environment.GetEnvironmentVariable(TestVariable);
			try
			{
				Environment.SetEnvironmentVariable(TestVariable, "1");
				Assert.That(EnvironmentVariables.IsTrue(TestVariable), Is.True);

				Environment.SetEnvironmentVariable(TestVariable, "0");
				Assert.That(EnvironmentVariables.IsTrue(TestVariable), Is.False);

				Environment.SetEnvironmentVariable(TestVariable, null);
				Assert.That(EnvironmentVariables.IsTrue(TestVariable), Is.False);
			}
			finally
			{
				Environment.SetEnvironmentVariable(TestVariable, original);
			}
		}
	}
}
