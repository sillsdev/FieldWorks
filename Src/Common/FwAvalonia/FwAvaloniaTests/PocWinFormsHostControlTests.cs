// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Reflection;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Poc;

namespace FwAvaloniaTests
{
	[TestFixture]
	public class PocWinFormsHostControlTests
	{
		private static bool ShouldBypass(bool hostContainsFocus, int keyCode)
		{
			var method = typeof(PocWinFormsHostControl).GetMethod(
				"ShouldBypassWinFormsDirectionalKeyHandling",
				BindingFlags.NonPublic | BindingFlags.Static);
			Assert.That(method, Is.Not.Null, "test seam missing");
			return (bool)method.Invoke(null, new object[] { hostContainsFocus, keyCode });
		}

		[Test]
		public void DirectionalKeys_AreBypassed_WhenAvaloniaHostContainsFocus()
		{
			Assert.That(ShouldBypass(true, 0x26), Is.True); // Up
			Assert.That(ShouldBypass(true, 0x28), Is.True); // Down
			Assert.That(ShouldBypass(true, 0x25), Is.True); // Left
			Assert.That(ShouldBypass(true, 0x27), Is.True); // Right
		}

		[Test]
		public void NonDirectionalKeys_AndUnfocusedHost_AreNotBypassed()
		{
			Assert.That(ShouldBypass(false, 0x26), Is.False); // Up
			Assert.That(ShouldBypass(true, 0x0D), Is.False); // Enter
			Assert.That(ShouldBypass(true, 0x09), Is.False); // Tab
		}
	}
}
