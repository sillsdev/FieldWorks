// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// Verifies the affix chooser commands keep the FLEx main window active when the chooser hides
	/// to run them (KeepOwnerActiveWhenHiding is true by default). See LT-22578.
	/// </summary>
	[TestFixture]
	public class ChooserCommandKeepOwnerActiveWhenHidingTests
	{
		/// <summary>
		/// Opens the modal New Entry dialog (InsertEntryDlg) after the chooser hides.
		/// </summary>
		[Test]
		public void MakeInflAffixEntryChooserCommand_KeepsOwnerActive()
		{
			var command = new MakeInflAffixEntryChooserCommand(
				null, true, "label", true, null, null, null);
			Assert.That(command.KeepOwnerActiveWhenHiding, Is.True);
		}

		/// <summary>
		/// Creates a slot (no dialog) after the chooser hides.
		/// </summary>
		[Test]
		public void MakeInflAffixSlotChooserCommand_KeepsOwnerActive()
		{
			var command = new MakeInflAffixSlotChooserCommand(
				null, true, "label", 0, false, null, null);
			Assert.That(command.KeepOwnerActiveWhenHiding, Is.True);
		}
	}
}
