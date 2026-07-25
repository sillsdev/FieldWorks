// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;

namespace FwAvaloniaTests
{
	[TestFixture]
	public class LexicalEditHostControlTests
	{
		// A region pane host claims the arrow keys (never Enter) for the hosted Avalonia surface. The
		// claiming decision is shared by every region host through InputKeyClaimPolicy; the pane case
		// passes claimEnterKey:false so Enter keeps its normal meaning in the pane.
		private static bool ShouldBypass(bool hostContainsFocus, int keyCode)
			=> InputKeyClaimPolicy.ShouldClaimKey((Keys)keyCode, hostContainsFocus, claimEnterKey: false);

		[Test]
		public void DirectionalKeys_AreBypassed_WhenAvaloniaHostContainsFocus()
		{
			Assert.That(ShouldBypass(true, 0x26), Is.True);
			Assert.That(ShouldBypass(true, 0x28), Is.True);
			Assert.That(ShouldBypass(true, 0x25), Is.True);
			Assert.That(ShouldBypass(true, 0x27), Is.True);
		}

		[Test]
		public void NonDirectionalKeys_AndUnfocusedHost_AreNotBypassed()
		{
			Assert.That(ShouldBypass(false, 0x26), Is.False);
			Assert.That(ShouldBypass(true, 0x0D), Is.False);
			Assert.That(ShouldBypass(true, 0x09), Is.False);
		}
	}
}