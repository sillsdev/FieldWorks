// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Covers which FLEX_AI_EXPORT values count as opting in to the AI-analysis export.
	/// </summary>
	[TestFixture]
	public class AiExportGateTests
	{
		[TestCase("1")]
		[TestCase("true")]
		[TestCase("TRUE")]
		[TestCase("yes")]
		[TestCase("on")]
		[TestCase(" 1 ")]
		public void IsEnabled_TreatsAnyOtherValueAsOptedIn(string value)
		{
			Assert.That(AiExportGate.IsEnabled(value), Is.True);
		}

		[TestCase(null)]
		[TestCase("")]
		[TestCase("   ")]
		[TestCase("0")]
		[TestCase("false")]
		[TestCase("False")]
		[TestCase("off")]
		[TestCase(" off ")]
		public void IsEnabled_FailsClosedForUnsetAndNegativeValues(string value)
		{
			Assert.That(AiExportGate.IsEnabled(value), Is.False);
		}

		[Test]
		public void IsEnabled_ReadsTheProcessEnvironmentAndDefaultsToOff()
		{
			var original = Environment.GetEnvironmentVariable(AiExportGate.EnabledVariable);
			try
			{
				Environment.SetEnvironmentVariable(AiExportGate.EnabledVariable, "1");
				Assert.That(AiExportGate.IsEnabled(), Is.True);

				Environment.SetEnvironmentVariable(AiExportGate.EnabledVariable, null);
				Assert.That(AiExportGate.IsEnabled(), Is.False);
			}
			finally
			{
				Environment.SetEnvironmentVariable(AiExportGate.EnabledVariable, original);
			}
		}
	}
}
