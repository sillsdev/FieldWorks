// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;

namespace FwAvaloniaTests
{
	/// <summary>
	/// The host's input-key claiming decision: the arrow keys are always claimed for the hosted Avalonia
	/// surface, Enter only when the host opted in (the dialog case), and nothing is claimed unless the host
	/// holds focus. Covers the pure decision (<see cref="InputKeyClaimPolicy.ShouldClaimKey"/>);
	/// the actual WinForms key routing it drives is confirmed manually in the running app.
	/// </summary>
	[TestFixture]
	public class InputKeyClaimingAvaloniaHostTests
	{
		[TestCase(Keys.Up)]
		[TestCase(Keys.Down)]
		[TestCase(Keys.Left)]
		[TestCase(Keys.Right)]
		public void ArrowKeys_AreClaimedWhenFocused(Keys key)
		{
			Assert.That(InputKeyClaimPolicy.ShouldClaimKey(key, hostContainsFocus: true, claimEnterKey: false),
				Is.True, "arrow keys are always claimed while the host holds focus");
		}

		[TestCase(Keys.Up)]
		[TestCase(Keys.Down)]
		[TestCase(Keys.Left)]
		[TestCase(Keys.Right)]
		[TestCase(Keys.Enter)]
		public void NothingIsClaimedWhenNotFocused(Keys key)
		{
			Assert.That(InputKeyClaimPolicy.ShouldClaimKey(key, hostContainsFocus: false, claimEnterKey: true),
				Is.False, "keys route normally when focus is elsewhere in the parent");
		}

		[Test]
		public void Enter_IsClaimedOnlyWhenOptedIn()
		{
			Assert.That(InputKeyClaimPolicy.ShouldClaimKey(Keys.Enter, hostContainsFocus: true, claimEnterKey: true),
				Is.True, "a dialog host claims Enter so it reaches the hosted content");
			Assert.That(InputKeyClaimPolicy.ShouldClaimKey(Keys.Enter, hostContainsFocus: true, claimEnterKey: false),
				Is.False, "a detail pane host leaves Enter to WinForms");
		}

		[TestCase(Keys.Tab)]
		[TestCase(Keys.A)]
		[TestCase(Keys.Escape)]
		public void OtherKeys_AreNeverClaimed(Keys key)
		{
			Assert.That(InputKeyClaimPolicy.ShouldClaimKey(key, hostContainsFocus: true, claimEnterKey: true),
				Is.False, "only the navigation keys are claimed; everything else routes normally");
		}
	}
}
