// Copyright (c) 2009-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.LCModel.Core.WritingSystems;
using SIL.FieldWorks.Common.Widgets;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.FieldWorks.Resources;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the ValidCharactersDlg.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ValidCharactersDlgTests
	{
		private DummyValidCharactersDlg m_dlg;

		/// <summary/>
		[SetUp]
		public void SetUp()
		{
			m_dlg = new DummyValidCharactersDlg();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Cleans up results after each test
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public void TearDown()
		{
			m_dlg.Dispose();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that we can add a single base character from the manual character entry
		/// text box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddSingleCharacter_ValidManualEntry_SingleBaseCharacter()
		{
			m_dlg.ManualCharEntryTextBox.Text = "A";

			m_dlg.CallAddSingleCharacter(m_dlg.ManualCharEntryTextBox);

			m_dlg.ValidCharsGridMngr.VerifyCharacters(new[] { "A" });
			Assert.That(m_dlg.ManualCharEntryTextBox.Text, Is.EqualTo(String.Empty), "The manual entry text box should be cleared after adding the character.");
			Assert.That(m_dlg.MessageBoxText.Count, Is.EqualTo(0), "No message boxes should have been displayed");
			Assert.That(m_dlg.BeepCount, Is.EqualTo(0));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that a lone diacritic typed in the manual character entry text box will get
		/// wiped out and the user will get a message telling them how to deal with diacritics.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddSingleCharacter_InvalidManualEntry_LoneDiacritic()
		{
			m_dlg.ManualCharEntryTextBox.Text = "\u0301";

			Assert.That(m_dlg.ManualCharEntryTextBox.Text, Is.EqualTo(String.Empty), "The manual entry text box should be cleared.");
			Assert.That(m_dlg.MessageBoxText.Count, Is.EqualTo(1), "One message box should have been displayed");
			Assert.That(m_dlg.MessageBoxText[0], Is.EqualTo(FwCoreDlgs.kstidLoneDiacriticNotValid));
			Assert.That(m_dlg.BeepCount, Is.EqualTo(0));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that a space followed by a diacritic typed in the manual character entry
		/// text box will get wiped out and the user will get a message telling them how to
		/// deal with diacritics.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddSingleCharacter_InvalidManualEntry_DiacriticWithLeadingSpace()
		{
			m_dlg.ManualCharEntryTextBox.Text = " \u0301";

			Assert.That(m_dlg.ManualCharEntryTextBox.Text, Is.EqualTo(String.Empty), "The manual entry text box should be cleared.");
			Assert.That(m_dlg.MessageBoxText.Count, Is.EqualTo(1), "One message box should have been displayed");
			Assert.That(m_dlg.MessageBoxText[0], Is.EqualTo(FwCoreDlgs.kstidLoneDiacriticNotValid));
			Assert.That(m_dlg.BeepCount, Is.EqualTo(0));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that two spaces typed in the manual character entry text box will get
		/// cleared and the user will get beeped.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddSingleCharacter_InvalidManualEntry_TwoSpaces()
		{
			m_dlg.ManualCharEntryTextBox.Text = "  ";

			Assert.That(m_dlg.ManualCharEntryTextBox.Text, Is.EqualTo(String.Empty), "The manual entry text box should be cleared.");
			Assert.That(m_dlg.MessageBoxText.Count, Is.EqualTo(0), "No message boxes should have been displayed");
			Assert.That(m_dlg.BeepCount, Is.EqualTo(1));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that an undefined character typed in the manual character entry text box
		/// will get cleared and the user will get beeped.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddSingleCharacter_InvalidManualEntry_BogusChar()
		{
			m_dlg.ManualCharEntryTextBox.Text = "\u2065";

			Assert.That(m_dlg.ManualCharEntryTextBox.Text, Is.EqualTo(String.Empty), "The manual entry text box should be cleared.");
			Assert.That(m_dlg.BeepCount, Is.EqualTo(1), "One beep should have been issued");
			Assert.That(m_dlg.MessageBoxText.Count, Is.EqualTo(0), "No message boxes should have been displayed");
			Assert.That(m_dlg.BeepCount, Is.EqualTo(1));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that we can add a letter from the Unicode value text box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddSingleCharacter_ValidUnicodeEntry_SingleLetter()
		{
			m_dlg.UnicodeValueTextBox.Text = "0067";
			m_dlg.CallAddSingleCharacter(m_dlg.UnicodeValueTextBox);
			m_dlg.ValidCharsGridMngr.VerifyCharacters(new[] { "g" });

			Assert.That(m_dlg.UnicodeValueTextBox.Text, Is.EqualTo(String.Empty), "The Unicode text box should be cleared after adding the character.");
			Assert.That(m_dlg.MessageBoxText.Count, Is.EqualTo(0), "No message boxes should have been displayed");
			Assert.That(m_dlg.BeepCount, Is.EqualTo(0));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that an error message is displayed if the user attempts to add a lone
		/// dicritic from the Unicode value text box. TE-8339
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddSingleCharacter_InvalidUnicodeEntry_Diacritic()
		{
			m_dlg.UnicodeValueTextBox.Text = "0301";
			m_dlg.CallAddSingleCharacter(m_dlg.UnicodeValueTextBox);
			m_dlg.ValidCharsGridMngr.VerifyCharacters(new string[] {  });

			Assert.That(m_dlg.UnicodeValueTextBox.Text, Is.EqualTo("0301"), "The Unicode text box should not be cleared to give the user a chance to correct the problem.");
			Assert.That(m_dlg.MessageBoxText.Count, Is.EqualTo(1), "One message box should have been displayed");
			Assert.That(m_dlg.MessageBoxText[0], Is.EqualTo(FwCoreDlgs.kstidLoneDiacriticNotValid));
			Assert.That(m_dlg.BeepCount, Is.EqualTo(0));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that an error message is displayed if the user attempts to add an undefined
		/// character from the Unicode value text box. TE-8339
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddSingleCharacter_InvalidUnicodeEntry_BogusChar()
		{
			m_dlg.UnicodeValueTextBox.Text = "5678";
			m_dlg.CallAddSingleCharacter(m_dlg.UnicodeValueTextBox);
			m_dlg.ValidCharsGridMngr.VerifyCharacters(new string[] {  });

			Assert.That(m_dlg.UnicodeValueTextBox.Text, Is.EqualTo("5678"), "The Unicode text box should not be cleared to give the user a chance to correct the problem.");
			Assert.That(m_dlg.MessageBoxText.Count, Is.EqualTo(1), "One message box should have been displayed");
			Assert.That(m_dlg.MessageBoxText[0], Is.EqualTo(ResourceHelper.GetResourceString("kstidUndefinedCharacterMsg")));
			Assert.That(m_dlg.BeepCount, Is.EqualTo(0));
		}

		private class Fwr3660ValidCharactersDlg : ValidCharactersDlg
		{
			public Fwr3660ValidCharactersDlg(LcmCache cache, IWritingSystemContainer container,
				CoreWritingSystemDefinition ws)
				: base(cache, container, null, null, ws, "dymmy")
			{
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests invoking from the New Project dialog where we don't have a cache yet
		/// (FWR-3660)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InvokeFromNewProject()
		{
			var wsManager = new WritingSystemManager();
			CoreWritingSystemDefinition ws = wsManager.Create("en");
			var wsContainer = new MemoryWritingSystemContainer(Enumerable.Empty<CoreWritingSystemDefinition>(), new[] {ws}, Enumerable.Empty<CoreWritingSystemDefinition>(),
				new[] {ws}, Enumerable.Empty<CoreWritingSystemDefinition>()) {DefaultVernacularWritingSystem = ws};
			using (var dlg = new Fwr3660ValidCharactersDlg(null, wsContainer, ws))
			{
				Assert.That(dlg, Is.Not.Null);
			}
		}
	}

	#region DummyValidCharactersDlg
	internal class DummyValidCharactersDlg : ValidCharactersDlg
	{
		#region Member variables
		public List<string> MessageBoxText { get; set; }
		public int BeepCount { get; set; }
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up the ValidCharactersDlg for testing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DummyValidCharactersDlg()
		{
			MessageBoxText = new List<string>();
		}

		/// <summary>
		/// Create a ValidCharsGridMngr for testing
		/// </summary>
		protected override ValidCharGridsManager CreateValidCharGridsManager()
		{
			return new DummyValidCharsGridMngr();
		}

		/// <summary>
		/// Exposes txtManualCharEntry text box
		/// </summary>
		public FwTextBox ManualCharEntryTextBox
		{
			get { return (FwTextBox)ReflectionHelper.GetField(this, "txtManualCharEntry"); }
		}

		/// <summary>
		/// Exposes txtUnicodeValue text box
		/// </summary>
		public FwTextBox UnicodeValueTextBox
		{
			get { return (FwTextBox)ReflectionHelper.GetField(this, "txtUnicodeValue"); }
		}

		/// <summary>
		/// Exposes AddSingleCharacter method
		/// </summary>
		public void CallAddSingleCharacter(FwTextBox txt)
		{
			AddSingleCharacter(txt);
		}

		/// <summary>
		/// Exposes m_validCharsGridMngr
		/// </summary>
		public DummyValidCharsGridMngr ValidCharsGridMngr
		{
			get { return (DummyValidCharsGridMngr)m_validCharsGridMngr; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows a message box to warn the user about an invalid operation.
		/// </summary>
		/// <param name="message">The message for the user.</param>
		/// ------------------------------------------------------------------------------------
		protected override void ShowMessageBox(string message)
		{
			MessageBoxText.Add(message);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Keeps track of the number of beeps that have been issued in the ValidCharactersDlg
		/// during a test.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void IssueBeep()
		{
			BeepCount++;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If the specified character is defined as a PUA character in m_langDef, returns its
		/// character type; otherwise, returns a value that indicates whether it is a valid
		/// character as defined by the Unicode Standard.
		/// </summary>
		/// <param name="chr">The character (may consist of more than one Unicode codepoint.</param>
		/// ------------------------------------------------------------------------------------
		protected override ValidCharacterType GetCharacterType(string chr)
		{
			return (!String.IsNullOrEmpty(chr) && chr[0] != 0x5678) ?
				ValidCharacterType.DefinedUnknown : ValidCharacterType.None;
		}
	}
	#endregion

	#region DummyValidCharsGridMngr
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Override of a ValidCharsGridsManager for testing.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DummyValidCharsGridMngr : ValidCharactersDlg.ValidCharGridsManager
	{
		internal List<string> m_charsInGrid = new List<string>();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the character to a list (rather than to a grid).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected internal override void AddCharacter(string chr, ValidCharacterType type,  bool notUsed)
		{
			m_charsInGrid.Add(chr);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that the expected characters were added.
		/// </summary>
		/// <param name="expectedChars">The characters that we expect would be added to the
		/// grid for a particular test.</param>
		/// ------------------------------------------------------------------------------------
		public void VerifyCharacters(string[] expectedChars)
		{
			Assert.That(m_charsInGrid.Count, Is.EqualTo(expectedChars.Length), "Expected number of characters in ValidCharsGridsManager does not match actual");
			foreach (string character in expectedChars)
			{
				Assert.That(m_charsInGrid.Contains(character), Is.True, character + " had not been added to the ValidCharsGridsManager");
			}
		}
	}
	#endregion
}
