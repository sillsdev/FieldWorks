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
	/// control, Enter only when the host opted in (the dialog case), and nothing is claimed unless the host
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

		[Test]
		public void TabKeys_AreClaimedWhenFocused()
		{
			Assert.That(InputKeyClaimPolicy.ShouldClaimKey(Keys.Tab, hostContainsFocus: true, claimEnterKey: false),
				Is.True, "Tab is claimed while the host holds focus, so Avalonia's own navigation sees it");
			Assert.That(InputKeyClaimPolicy.ShouldClaimKey(Keys.Tab | Keys.Control, hostContainsFocus: true, claimEnterKey: false),
				Is.False, "Ctrl+Tab is excluded so the surrounding application keeps it for its own tab/window switching");
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

		// LT-22820: a detail pane claims Ctrl+Z and Ctrl+Y so the Edit menu's shortcuts do not
		// run FieldWorks' Undo before the focused text box undoes its own typing.
		[TestCase(Keys.Control | Keys.Z)]
		[TestCase(Keys.Control | Keys.Y)]
		public void UndoAndRedoKeys_AreClaimedOnlyWhenOptedIn(Keys key)
		{
			Assert.That(InputKeyClaimPolicy.ShouldClaimKey(key, hostContainsFocus: true,
				claimEnterKey: false, claimUndoRedoKeys: true), Is.True, "a detail pane claims it");
			Assert.That(InputKeyClaimPolicy.ShouldClaimKey(key, hostContainsFocus: true,
				claimEnterKey: true), Is.False, "other hosts leave it to WinForms");
			Assert.That(InputKeyClaimPolicy.ShouldClaimKey(key, hostContainsFocus: false,
				claimEnterKey: false, claimUndoRedoKeys: true), Is.False, "and only while focused");
		}

		[TestCase(Keys.Control | Keys.Shift | Keys.Z)]
		[TestCase(Keys.Control | Keys.Q)]
		public void OtherControlKeys_AreNotClaimed_AsUndoOrRedo(Keys key)
		{
			Assert.That(InputKeyClaimPolicy.ShouldClaimKey(key, hostContainsFocus: true,
				claimEnterKey: false, claimUndoRedoKeys: true), Is.False);
		}

		[TestCase(Keys.A)]
		[TestCase(Keys.Escape)]
		public void OtherKeys_AreNeverClaimed(Keys key)
		{
			Assert.That(InputKeyClaimPolicy.ShouldClaimKey(key, hostContainsFocus: true, claimEnterKey: true),
				Is.False, "only the navigation keys are claimed; everything else routes normally");
		}
	}
}
