// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.FieldWorks.XWorks.LexEd;

namespace LexEdDllTests
{
	/// <summary>
	/// Guards the KeepOwnerActiveWhenHiding opt-in contract for the Lexicon "Add a Component" and
	/// "Add a Complex Form" chooser commands (LT-22578). Both are launched from a chooser that
	/// hides itself and then open a modal dialog (LinkEntryOrSenseDlg / EntryGoDlg), so both must
	/// opt in to keep an unrelated application from flashing in front while the dialog loads. See
	/// ChooserCommandKeepOwnerActiveWhenHidingTests (DetailControls) for the affix equivalents and
	/// the rationale for why only the opt-in flag is unit-tested.
	/// </summary>
	[TestFixture]
	public class EntrySequenceChooserCommandKeepOwnerActiveWhenHidingTests
	{
		/// <summary>
		/// "Add a Component" opens the modal LinkEntryOrSenseDlg, so it must opt in.
		/// </summary>
		[Test]
		public void AddPrimaryLexemeChooserCommand_OptsIn()
		{
			var command = new AddPrimaryLexemeChooserCommand(
				null, false, "label", null, null, null, null);
			Assert.That(command.KeepOwnerActiveWhenHiding, Is.True);
		}

		/// <summary>
		/// "Add a Complex Form" opens the modal EntryGoDlg, so it must opt in.
		/// </summary>
		[Test]
		public void AddComplexFormChooserCommand_OptsIn()
		{
			var command = new AddComplexFormChooserCommand(
				null, false, "label", null, null, null, null);
			Assert.That(command.KeepOwnerActiveWhenHiding, Is.True);
		}
	}
}
