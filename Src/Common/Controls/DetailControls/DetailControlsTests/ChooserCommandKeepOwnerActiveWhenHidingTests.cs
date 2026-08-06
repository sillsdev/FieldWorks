// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// Guards the KeepOwnerActiveWhenHiding opt-in contract for the affix chooser commands
	/// (LT-22578). A chooser command whose Execute() opens another modal dialog must return true
	/// so ReallySimpleListChooser.HideForCommand re-activates the FLEx main window as the chooser
	/// hides; without it an unrelated application can flash in front while the new dialog loads. A
	/// command that opens no dialog must stay false (the base default). The flag is the only part
	/// of the fix that is deterministic and UI-free; the actual owner activation and window
	/// Z-order require a live message pump and are verified manually.
	/// </summary>
	[TestFixture]
	public class ChooserCommandKeepOwnerActiveWhenHidingTests
	{
		/// <summary>
		/// Opens the modal New Entry dialog (InsertEntryDlg), so it must opt in.
		/// </summary>
		[Test]
		public void MakeInflAffixEntryChooserCommand_OptsIn()
		{
			var command = new MakeInflAffixEntryChooserCommand(
				null, true, "label", true, null, null, null);
			Assert.That(command.KeepOwnerActiveWhenHiding, Is.True);
		}

		/// <summary>
		/// Creates a slot without opening a dialog, so it keeps the base default of false. This
		/// also exercises the base ChooserCommand default (this command does not override it).
		/// </summary>
		[Test]
		public void MakeInflAffixSlotChooserCommand_DoesNotOptIn()
		{
			var command = new MakeInflAffixSlotChooserCommand(
				null, true, "label", 0, false, null, null);
			Assert.That(command.KeepOwnerActiveWhenHiding, Is.False);
		}
	}
}
