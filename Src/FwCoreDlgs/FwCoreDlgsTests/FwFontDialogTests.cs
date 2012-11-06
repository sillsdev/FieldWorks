// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File:
// Responsibility:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------

using System.Windows.Forms;
using NUnit.Framework;
using System;
using SIL.FieldWorks.FDO.FDOTests;
using System.Collections.Generic;
using System.Linq;
using XCore;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary></summary>
	[TestFixture]
	public class FwFontDialogTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		class FakeFwFontDialog : FwFontDialog
		{
			public FakeFwFontDialog(IHelpTopicProvider helpTopicProvider) : base(helpTopicProvider)
			{
			}

			/// <summary>
			/// No-op
			/// </summary>
			protected override void UpdatePreview()
			{
			}
		}

		/// <summary>
		/// Object to test
		/// </summary>
		private FwFontDialog m_dialog;

		#region Setup
		/// <summary></summary>
		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();
			m_dialog = new FakeFwFontDialog(null);
		}

		/// <summary></summary>
		[TearDown]
		public override void TestTearDown()
		{
			base.TestTearDown();
			m_dialog.Dispose();
		}
		#endregion

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure font names are alphabetically sorted.
		/// Related to FWNX-273: Fonts not in alphabetical order
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		[Test]
		public void FillFontList_IsAlphabeticallySorted()
		{
			const int firstFontInListLocation = 3;
			CallMethod(m_dialog,"FillFontList");
			var fontNames = ((ListBox) GetField(m_dialog, "m_lbFontNames")).Items;
			for (var i = firstFontInListLocation; i+1 < fontNames.Count; i++)
			{
				// Check that each font in the list is alphabetically before the next font in the list
				Assert.LessOrEqual(fontNames[i] as string, fontNames[i+1] as string, "Font names not alphabetically sorted.");
			}
		}

		/// <summary></summary>
		[Test]
		public void UpdateFontSizeIfValid_Valid_True()
		{
			int? actual;
			bool returnValue;
			returnValue = GetBoolResult(m_dialog, "UpdateFontSizeIfValid", new object[] {"11"});
			actual = GetProperty(m_dialog, "FontSize") as int?;
			Assert.That(actual, Is.EqualTo(11));
			Assert.That(returnValue, Is.True);

			returnValue = GetBoolResult(m_dialog, "UpdateFontSizeIfValid", new object[] {"42"});
			actual = GetProperty(m_dialog, "FontSize") as int?;
			Assert.That(actual, Is.EqualTo(42));
			Assert.That(returnValue, Is.True);
		}

		/// <summary></summary>
		[Test]
		public void UpdateFontSizeIfValid_Null_Unchanged()
		{
			UpdateFontSizeIfValid_VerifyUnchanged(null);
		}

		/// <summary>Can't have 0 as the font size</summary>
		[Test]
		public void UpdateFontSizeIfValid_Zero_Unchanged()
		{
			UpdateFontSizeIfValid_VerifyUnchanged("0");
		}

		/// <summary></summary>
		[Test]
		public void UpdateFontSizeIfValid_NegativeNumber_Unchanged()
		{
			UpdateFontSizeIfValid_VerifyUnchanged("-23");
		}

		/// <summary></summary>
		[Test]
		public void UpdateFontSizeIfValid_3DigitNumber_Works()
		{
			int? actual;
			bool returnValue;
			returnValue = GetBoolResult(m_dialog, "UpdateFontSizeIfValid", new object[] {"321"});
			actual = GetProperty(m_dialog, "FontSize") as int?;
			Assert.That(actual, Is.EqualTo(321));
			Assert.That(returnValue, Is.True);
		}

		/// <summary></summary>
		[Test]
		public void UpdateFontSizeIfValid_BigNumber_Unchanged()
		{
			UpdateFontSizeIfValid_VerifyUnchanged("4321");
		}

		/// <summary></summary>
		[Test]
		public void UpdateFontSizeIfValid_NonNumber_Unchanged()
		{
			UpdateFontSizeIfValid_VerifyUnchanged("");
			UpdateFontSizeIfValid_VerifyUnchanged("a");
			UpdateFontSizeIfValid_VerifyUnchanged(" a");
			UpdateFontSizeIfValid_VerifyUnchanged("a ");
			UpdateFontSizeIfValid_VerifyUnchanged("1a");
			UpdateFontSizeIfValid_VerifyUnchanged("a1");
			UpdateFontSizeIfValid_VerifyUnchanged("1 1");
			UpdateFontSizeIfValid_VerifyUnchanged("1 a");
			UpdateFontSizeIfValid_VerifyUnchanged("a 1");
			UpdateFontSizeIfValid_VerifyUnchanged(".");
			UpdateFontSizeIfValid_VerifyUnchanged(",");
			UpdateFontSizeIfValid_VerifyUnchanged("$");
			UpdateFontSizeIfValid_VerifyUnchanged("#");
			// Non-integer
			UpdateFontSizeIfValid_VerifyUnchanged("10.5");
		}

		/// <summary>
		/// Unit test helper
		/// </summary>
		private void UpdateFontSizeIfValid_VerifyUnchanged(string size)
		{
			var initialFontSize = 17;
			SetProperty(m_dialog, "FontSize", initialFontSize);
			int? actual;
			bool returnValue;
			returnValue = GetBoolResult(m_dialog, "UpdateFontSizeIfValid", new object[] {size});
			actual = GetProperty(m_dialog, "FontSize") as int?;
			Assert.That(actual, Is.EqualTo(initialFontSize), "Should not have changed font size.");
			Assert.That(returnValue, Is.False);
		}

		/// <summary></summary>
		[Test]
		public void UpdateFontSizeIfValid_SameNumber_Unchanged()
		{
			// (It gets initialized to 17 in helper method)
			UpdateFontSizeIfValid_VerifyUnchanged("17");
		}

		private void ApplyNewFontSizeIfValid_Helper_SetupInsertionPoint(int initialFontSize, out TextBox dialog_fontSizeTextBox)
		{
			// Don't let OnFontSizeTextChanged be called.
			SetField(m_dialog, "m_fInSelectedIndexChangedHandler", true);
			dialog_fontSizeTextBox = GetField(m_dialog, "m_tbFontSize") as TextBox;
			dialog_fontSizeTextBox.Text = initialFontSize.ToString();
			SetProperty(m_dialog, "FontSize", initialFontSize);
			// Put insertion point at end.
			dialog_fontSizeTextBox.Select(dialog_fontSizeTextBox.Text.Length, 0);
			Assert.That(dialog_fontSizeTextBox.SelectionStart, Is.EqualTo(dialog_fontSizeTextBox.Text.Length), "Unit test error.");
		}

		/// <summary>Expecting insertion point to move ahead without being pulled back</summary>
		[Test]
		public void ApplyNewFontSizeIfValid_ValidChar_InsertionPointChanged()
		{
			TextBox dialog_fontSizeTextBox;
			int initialFontSize = 17;
			ApplyNewFontSizeIfValid_Helper_SetupInsertionPoint(initialFontSize, out dialog_fontSizeTextBox);

			// User types "8"
			dialog_fontSizeTextBox.AppendText("8");
			CallMethod(m_dialog, "ApplyNewFontSizeIfValid", new[] {"178"});

			Assert.That(dialog_fontSizeTextBox.SelectionStart, Is.EqualTo(3), "Insertion point should have advanced");
		}

		/// <summary></summary>
		[Test]
		public void ApplyNewFontSizeIfValid_AppendInvalidChar_InsertionPointNotChanged()
		{
			TextBox dialog_fontSizeTextBox;
			int initialFontSize = 17;
			ApplyNewFontSizeIfValid_Helper_SetupInsertionPoint(initialFontSize, out dialog_fontSizeTextBox);

			// User types "k"
			dialog_fontSizeTextBox.AppendText("k");
			CallMethod(m_dialog, "ApplyNewFontSizeIfValid", new[] {"17k"});

			Assert.That(dialog_fontSizeTextBox.SelectionStart, Is.EqualTo(2), "Insertion point should have been put  back where it was.");
		}

		/// <summary></summary>
		[Test]
		public void ApplyNewFontSizeIfValid_PrependInvalidChar_InsertionPointNotChanged()
		{
			TextBox dialog_fontSizeTextBox;
			int initialFontSize = 17;
			ApplyNewFontSizeIfValid_Helper_SetupInsertionPoint(initialFontSize, out dialog_fontSizeTextBox);

			// User clicks at beginning and types "k"
			dialog_fontSizeTextBox.Select(0, 0);
			dialog_fontSizeTextBox.Text = "k17";
			dialog_fontSizeTextBox.Select(1, 0);
			CallMethod(m_dialog, "ApplyNewFontSizeIfValid", new[] {"k17"});

			Assert.That(dialog_fontSizeTextBox.SelectionStart, Is.EqualTo(0), "Insertion point should have been put back where it was.");
		}

		/// <summary></summary>
		[Test]
		public void ApplyNewFontSizeIfValid_InsertInvalidCharInMiddle_InsertionPointNotChanged()
		{
			TextBox dialog_fontSizeTextBox;
			int initialFontSize = 17;
			ApplyNewFontSizeIfValid_Helper_SetupInsertionPoint(initialFontSize, out dialog_fontSizeTextBox);

			// User clicks in middle and types "k"
			dialog_fontSizeTextBox.Select(1, 0);
			dialog_fontSizeTextBox.Text = "1k7";
			dialog_fontSizeTextBox.Select(2, 0);
			CallMethod(m_dialog, "ApplyNewFontSizeIfValid", new[] {"1k7"});

			Assert.That(dialog_fontSizeTextBox.SelectionStart, Is.EqualTo(1), "Insertion point should have been put back where it was.");
		}

		/// <summary></summary>
		[Test]
		public void ApplyNewFontSizeIfValid_Append4thDigit_InsertionPointNotChanged()
		{
			TextBox dialog_fontSizeTextBox;
			int initialFontSize = 178;
			ApplyNewFontSizeIfValid_Helper_SetupInsertionPoint(initialFontSize, out dialog_fontSizeTextBox);

			// User clicks at end and types 9
			dialog_fontSizeTextBox.Select(3, 0);
			dialog_fontSizeTextBox.Text = "1789";
			dialog_fontSizeTextBox.Select(4, 0);
			CallMethod(m_dialog, "ApplyNewFontSizeIfValid", new[] {"1789"});

			Assert.That(dialog_fontSizeTextBox.SelectionStart, Is.EqualTo(3), "Insertion point should have been put back where it was.");
		}

		/// <remarks>
		/// Not as immediately obvious what should happen if the user
		/// deletes or backspaces the only digit. Possibly moving insertion
		/// point to end is best, but just leaving insertion point at beginning
		/// is probably fine too.
		/// </remarks>
		[Test]
		public void ApplyNewFontSizeIfValid_UserDeletesOnlyDigit()
		{
			TextBox dialog_fontSizeTextBox;
			int initialFontSize = 9;
			ApplyNewFontSizeIfValid_Helper_SetupInsertionPoint(initialFontSize, out dialog_fontSizeTextBox);

			// User clicks at end and types backspace
			dialog_fontSizeTextBox.Select(1, 0);
			dialog_fontSizeTextBox.Text = "";
			dialog_fontSizeTextBox.Select(0, 0);
			CallMethod(m_dialog, "ApplyNewFontSizeIfValid", new[] {""});

			Assert.That(dialog_fontSizeTextBox.SelectionStart, Is.EqualTo(0));
		}

		/// <summary/>
		[Test]
		public void ApplyNewFontSizeIfValid_UserTypesJunkOverSelection_TextUnchanged()
		{
			TextBox dialog_fontSizeTextBox;
			int initialFontSize = 123;
			ApplyNewFontSizeIfValid_Helper_SetupInsertionPoint(initialFontSize, out dialog_fontSizeTextBox);

			// User clicks between 1 and 2, drags to between 2 and 3, and types "a".
			dialog_fontSizeTextBox.Select(1, 1);
			dialog_fontSizeTextBox.Text = "1a3";
			dialog_fontSizeTextBox.Select(2, 0);
			CallMethod(m_dialog, "ApplyNewFontSizeIfValid", new[] {"1a3"});

			int? resultingFontSize = GetProperty(m_dialog, "FontSize") as int?;
			Assert.That(resultingFontSize, Is.EqualTo(initialFontSize), "Should not have changed font size.");

			Assert.That(dialog_fontSizeTextBox.SelectionStart, Is.EqualTo(1));
		}

		/// <summary/>
		[Test]
		public void OnSelectedFontSizesIndexChanged_UpdatesFontSizeAndTextBox()
		{
			var dialog_fontSizeTextBox = GetField(m_dialog, "m_tbFontSize") as TextBox;
			var dialog_lbFontSizes = GetField(m_dialog, "m_lbFontSizes") as ListBox;
			var fourthSize = dialog_lbFontSizes.Items[4];
			var fifthSize = dialog_lbFontSizes.Items[5];
			Assert.That(fourthSize, Is.Not.EqualTo(fifthSize), "Not a good unit test.");

			dialog_lbFontSizes.SelectedIndex = 4;
			CallMethod(m_dialog, "OnSelectedFontSizesIndexChanged", new object[] {null, null});
			Assert.That(dialog_fontSizeTextBox.Text, Is.EqualTo(fourthSize));
			Assert.That(GetProperty(m_dialog, "FontSize").ToString(), Is.EqualTo(fourthSize));

			dialog_lbFontSizes.SelectedIndex = 5;
			CallMethod(m_dialog, "OnSelectedFontSizesIndexChanged", new object[] {null, null});
			Assert.That(dialog_fontSizeTextBox.Text, Is.EqualTo(fifthSize));
			Assert.That(GetProperty(m_dialog, "FontSize").ToString(), Is.EqualTo(fifthSize));
		}
	}
}
