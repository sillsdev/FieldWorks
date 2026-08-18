// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.FieldWorks.XWorks.LexEd;

namespace LexEdDllTests
{
	/// <summary>
	/// Verifies the Lexicon "Add a Component" and "Add a Complex Form" chooser commands keep the
	/// FLEx main window active when the chooser hides to run them (KeepOwnerActiveWhenHiding is
	/// true by default). Both open a modal dialog (LinkEntryOrSenseDlg / EntryGoDlg). See LT-22578.
	/// </summary>
	[TestFixture]
	public class EntrySequenceChooserCommandKeepOwnerActiveWhenHidingTests
	{
		/// <summary>
		/// "Add a Component" opens the modal LinkEntryOrSenseDlg.
		/// </summary>
		[Test]
		public void AddPrimaryLexemeChooserCommand_KeepsOwnerActive()
		{
			var command = new AddPrimaryLexemeChooserCommand(
				null, false, "label", null, null, null, null);
			Assert.That(command.KeepOwnerActiveWhenHiding, Is.True);
		}

		/// <summary>
		/// "Add a Complex Form" opens the modal EntryGoDlg.
		/// </summary>
		[Test]
		public void AddComplexFormChooserCommand_KeepsOwnerActive()
		{
			var command = new AddComplexFormChooserCommand(
				null, false, "label", null, null, null, null);
			Assert.That(command.KeepOwnerActiveWhenHiding, Is.True);
		}
	}
}
