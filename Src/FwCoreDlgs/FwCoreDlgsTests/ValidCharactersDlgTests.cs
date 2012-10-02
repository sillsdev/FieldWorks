// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009 SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ValidCharactersDlgTests.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using NUnit.Framework;
using SIL.FieldWorks.Common.Utils;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the ValidCharactersDlg.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ValidCharactersDlgTests : ValidCharactersDlg
	{
		#region Member variables
		internal List<string> m_messageBoxText = new List<string>();
		internal int m_BeepCount = 0;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up the ValidCharactersDlg for testing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ValidCharactersDlgTests()
		{
			ReflectionHelper.SetField(this, "m_chrPropEng", new DummyCharPropEngine());
			m_validCharsGridMngr = new DummyValidCharsGridMngr();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Cleans up results after each test
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public void TearDown()
		{
			((DummyValidCharsGridMngr)m_validCharsGridMngr).m_charsInGrid.Clear();
			m_messageBoxText.Clear();
			m_BeepCount = 0;
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
			TextBox txtManualEntry = (TextBox)ReflectionHelper.GetField(this, "txtManualCharEntry");
			txtManualEntry.Text = "A";

			AddSingleCharacter(txtManualEntry);

			((DummyValidCharsGridMngr)m_validCharsGridMngr).VerifyCharacters(new string[] { "A" });
			Assert.AreEqual(String.Empty, txtManualEntry.Text,
				"The manual entry text box should be cleared after adding the character.");
			Assert.AreEqual(0, m_messageBoxText.Count,
				"No message boxes should have been displayed");
			Assert.AreEqual(0, m_BeepCount);
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
			TextBox txtManualEntry = (TextBox)ReflectionHelper.GetField(this, "txtManualCharEntry");
			txtManualEntry.Text = "\u0301";

			Assert.AreEqual(String.Empty, txtManualEntry.Text,
				"The manual entry text box should be cleared.");
			Assert.AreEqual(1, m_messageBoxText.Count, "One message box should have been displayed");
			Assert.AreEqual(FwCoreDlgs.kstidLoneDiacriticNotValid, m_messageBoxText[0]);
			Assert.AreEqual(0, m_BeepCount);
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
			TextBox txtManualEntry = (TextBox)ReflectionHelper.GetField(this, "txtManualCharEntry");
			txtManualEntry.Text = " \u0301";

			Assert.AreEqual(String.Empty, txtManualEntry.Text,
				"The manual entry text box should be cleared.");
			Assert.AreEqual(1, m_messageBoxText.Count, "One message box should have been displayed");
			Assert.AreEqual(FwCoreDlgs.kstidLoneDiacriticNotValid, m_messageBoxText[0]);
			Assert.AreEqual(0, m_BeepCount);
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
			TextBox txtManualEntry = (TextBox)ReflectionHelper.GetField(this, "txtManualCharEntry");
			txtManualEntry.Text = "  ";

			Assert.AreEqual(String.Empty, txtManualEntry.Text,
				"The manual entry text box should be cleared.");
			Assert.AreEqual(0, m_messageBoxText.Count, "No message boxes should have been displayed");
			Assert.AreEqual(1, m_BeepCount);
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
			TextBox txtManualEntry = (TextBox)ReflectionHelper.GetField(this, "txtManualCharEntry");
			txtManualEntry.Text = "\u5678"; // see DummyCharPropEngine.get_GeneralCategory

			Assert.AreEqual(String.Empty, txtManualEntry.Text,
				"The manual entry text box should be cleared.");
			Assert.AreEqual(1, m_BeepCount, "One beep should have been issued");
			Assert.AreEqual(0, m_messageBoxText.Count, "No message boxes should have been displayed");
			Assert.AreEqual(1, m_BeepCount);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that we can add a letter from the Unicode value text box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddSingleCharacter_ValidUnicodeEntry_SingleLetter()
		{
			TextBox txtUnicode = (TextBox)ReflectionHelper.GetField(this, "txtUnicodeValue");
			txtUnicode.Text = "0067";
			AddSingleCharacter(txtUnicode);
			((DummyValidCharsGridMngr)m_validCharsGridMngr).VerifyCharacters(new string[] { "g" });
			Assert.AreEqual(String.Empty, txtUnicode.Text,
				"The Unicode text box should be cleared after adding the character.");
			Assert.AreEqual(0, m_messageBoxText.Count, "No message boxes should have been displayed");
			Assert.AreEqual(0, m_BeepCount);
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
			TextBox txtUnicode = (TextBox)ReflectionHelper.GetField(this, "txtUnicodeValue");
			txtUnicode.Text = "0301";
			AddSingleCharacter(txtUnicode);
			((DummyValidCharsGridMngr)m_validCharsGridMngr).VerifyCharacters(new string[] { });
			Assert.AreEqual("0301", txtUnicode.Text,
				"The Unicode text box should not be cleared to give the user a chance to correct the problem.");
			Assert.AreEqual(1, m_messageBoxText.Count, "One message box should have been displayed");
			Assert.AreEqual(FwCoreDlgs.kstidLoneDiacriticNotValid, m_messageBoxText[0]);
			Assert.AreEqual(0, m_BeepCount);
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
			TextBox txtUnicode = (TextBox)ReflectionHelper.GetField(this, "txtUnicodeValue");
			txtUnicode.Text = "5678";
			AddSingleCharacter(txtUnicode);
			((DummyValidCharsGridMngr)m_validCharsGridMngr).VerifyCharacters(new string[] { });
			Assert.AreEqual("5678", txtUnicode.Text,
				"The Unicode text box should not be cleared to give the user a chance to correct the problem.");
			Assert.AreEqual(1, m_messageBoxText.Count, "One message box should have been displayed");
			Assert.AreEqual(ResourceHelper.GetResourceString("kstidUndefinedCharacterMsg"),
				m_messageBoxText[0]);
			Assert.AreEqual(0, m_BeepCount);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows a message box to warn the user about an invalid operation.
		/// </summary>
		/// <param name="message">The message for the user.</param>
		/// ------------------------------------------------------------------------------------
		protected override void ShowMessageBox(string message)
		{
			m_messageBoxText.Add(message);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Keeps track of the number of beeps that have been issued in the ValidCharactersDlg
		/// during a test.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void IssueBeep()
		{
			m_BeepCount++;
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
			return (!String.IsNullOrEmpty(chr) && (int)chr[0] != 0x5678) ?
				ValidCharacterType.DefinedUnknown : ValidCharacterType.None;
		}
	}

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
		/// Initializes a new instance of the <see cref="DummyValidCharsGridMngr"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DummyValidCharsGridMngr()
		{
		}

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
			Assert.AreEqual(expectedChars.Length, m_charsInGrid.Count,
				"Expected number of characters in ValidCharsGridsManager does not match actual");
			foreach (string character in expectedChars)
			{
				Assert.IsTrue(m_charsInGrid.Contains(character),
					character + " had not been added to the ValidCharsGridsManager");
			}
		}
	}
}
