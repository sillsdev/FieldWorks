// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Covers which UI-mode values select the New UI, and how that combines with the
	/// FW_AVALONIA opt-in.
	/// </summary>
	[TestFixture]
	public class UIModeGatesTests
	{
		[TestCase("New")]
		[TestCase("new")]
		[TestCase("NEW")]
		public void ShouldUseAvaloniaUI_TrueOnlyForNew(string uiMode)
		{
			Assert.That(UIModeGates.ShouldUseAvaloniaUI(uiMode), Is.True);
		}

		[TestCase(null)]
		[TestCase("")]
		[TestCase("Legacy")]
		[TestCase(" New ")]
		[TestCase("Newer")]
		public void ShouldUseAvaloniaUI_FalseForEverythingElse(string uiMode)
		{
			Assert.That(UIModeGates.ShouldUseAvaloniaUI(uiMode), Is.False);
		}

		[Test]
		public void ShouldUseAvaloniaUIFromSettings_NeedsBothTheOptInAndNew()
		{
			var original = Environment.GetEnvironmentVariable(UIModeGates.SwitchingEnabledVariable);
			try
			{
				Environment.SetEnvironmentVariable(UIModeGates.SwitchingEnabledVariable, "1");
				Assert.That(UIModeGates.ShouldUseAvaloniaUIFromSettings("New"), Is.True);
				Assert.That(UIModeGates.ShouldUseAvaloniaUIFromSettings("Legacy"), Is.False);

				Environment.SetEnvironmentVariable(UIModeGates.SwitchingEnabledVariable, null);
				Assert.That(UIModeGates.ShouldUseAvaloniaUIFromSettings("New"), Is.False);
			}
			finally
			{
				Environment.SetEnvironmentVariable(UIModeGates.SwitchingEnabledVariable, original);
			}
		}
	}
}
