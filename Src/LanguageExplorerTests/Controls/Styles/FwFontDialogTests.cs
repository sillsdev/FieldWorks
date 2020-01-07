// Copyright (c) 2010-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using LanguageExplorer.Controls.Styles;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorerTests.Controls.Styles
{
	/// <summary />
	[TestFixture]
	public class FwFontDialogTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		/// <summary>
		/// Object to test
		/// </summary>
		private FwFontDialog m_dialog;

		#region Setup
		/// <summary />
		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();
			m_dialog = new FakeFwFontDialog(null);
		}

		/// <summary />
		[TearDown]
		public override void TestTearDown()
		{
			try
			{
				m_dialog.Dispose();
			}
			catch (Exception err)
			{
				throw new Exception($"Error in running {GetType().Name} TestTearDown method.", err);
			}
			finally
			{
				m_dialog = null;
				base.TestTearDown();
			}
		}
		#endregion

		/// <summary>
		/// Make sure font names are alphabetically sorted.
		/// Related to FWNX-273: Fonts not in alphabetical order
		/// </summary>
		[Test]
		public void FillFontList_IsAlphabeticallySorted()
		{
			const int firstFontInListLocation = 3;
			m_dialog.FillFontList();
			var fontNames = m_dialog.FontNamesListBox.Items;
			for (var i = firstFontInListLocation; i + 1 < fontNames.Count; i++)
			{
				// Check that each font in the list is alphabetically before the next font in the list
				Assert.LessOrEqual(fontNames[i] as string, fontNames[i + 1] as string, "Font names not alphabetically sorted.");
			}
		}

		/// <summary />
		[Test]
		public void UpdateFontSizeIfValid_Valid_True()
		{
			var returnValue = m_dialog.UpdateFontSizeIfValid("11");
			var actual = m_dialog.FontSize;
			Assert.That(actual, Is.EqualTo(11));
			Assert.That(returnValue, Is.True);

			returnValue = m_dialog.UpdateFontSizeIfValid("42");
			actual = m_dialog.FontSize;
			Assert.That(actual, Is.EqualTo(42));
			Assert.That(returnValue, Is.True);
		}

		/// <summary />
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

		/// <summary />
		[Test]
		public void UpdateFontSizeIfValid_NegativeNumber_Unchanged()
		{
			UpdateFontSizeIfValid_VerifyUnchanged("-23");
		}

		/// <summary />
		[Test]
		public void UpdateFontSizeIfValid_3DigitNumber_Works()
		{
			var returnValue = m_dialog.UpdateFontSizeIfValid("321");
			var actual = m_dialog.FontSize;
			Assert.That(actual, Is.EqualTo(321));
			Assert.That(returnValue, Is.True);
		}

		/// <summary />
		[Test]
		public void UpdateFontSizeIfValid_BigNumber_Unchanged()
		{
			UpdateFontSizeIfValid_VerifyUnchanged("4321");
		}

		/// <summary />
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
			const int initialFontSize = 17;
			m_dialog.FontSize = initialFontSize;
			var returnValue = m_dialog.UpdateFontSizeIfValid(size);
			var actual = m_dialog.FontSize;
			Assert.That(actual, Is.EqualTo(initialFontSize), "Should not have changed font size.");
			Assert.That(returnValue, Is.False);
		}

		/// <summary />
		[Test]
		public void UpdateFontSizeIfValid_SameNumber_Unchanged()
		{
			// (It gets initialized to 17 in helper method)
			UpdateFontSizeIfValid_VerifyUnchanged("17");
		}

		private void ApplyNewFontSizeIfValid_Helper_SetupInsertionPoint(int initialFontSize, out TextBox fontSizeTextBox)
		{
			// Don't let OnFontSizeTextChanged be called.
			m_dialog.InSelectedIndexChangedHandler = true;

			fontSizeTextBox = m_dialog.FontSizeTextBox;
			fontSizeTextBox.Text = initialFontSize.ToString();
			m_dialog.FontSize = initialFontSize;
			// Put insertion point at end.
			fontSizeTextBox.Select(fontSizeTextBox.Text.Length, 0);
			Assert.That(fontSizeTextBox.SelectionStart, Is.EqualTo(fontSizeTextBox.Text.Length), "Unit test error.");
		}

		/// <summary>Expecting insertion point to move ahead without being pulled back</summary>
		[Test]
		public void ApplyNewFontSizeIfValid_ValidChar_InsertionPointChanged()
		{
			TextBox fontSizeTextBox;
			const int initialFontSize = 17;
			ApplyNewFontSizeIfValid_Helper_SetupInsertionPoint(initialFontSize, out fontSizeTextBox);

			// User types "8"
			fontSizeTextBox.AppendText("8");
			m_dialog.ApplyNewFontSizeIfValid("178");

			Assert.That(fontSizeTextBox.SelectionStart, Is.EqualTo(3), "Insertion point should have advanced");
		}

		/// <summary />
		[Test]
		public void ApplyNewFontSizeIfValid_AppendInvalidChar_InsertionPointNotChanged()
		{
			TextBox fontSizeTextBox;
			const int initialFontSize = 17;
			ApplyNewFontSizeIfValid_Helper_SetupInsertionPoint(initialFontSize, out fontSizeTextBox);

			// User types "k"
			fontSizeTextBox.AppendText("k");
			m_dialog.ApplyNewFontSizeIfValid("17k");

			Assert.That(fontSizeTextBox.SelectionStart, Is.EqualTo(2), "Insertion point should have been put  back where it was.");
		}

		/// <summary />
		[Test]
		public void ApplyNewFontSizeIfValid_PrependInvalidChar_InsertionPointNotChanged()
		{
			TextBox fontSizeTextBox;
			const int initialFontSize = 17;
			ApplyNewFontSizeIfValid_Helper_SetupInsertionPoint(initialFontSize, out fontSizeTextBox);

			// User clicks at beginning and types "k"
			fontSizeTextBox.Select(0, 0);
			fontSizeTextBox.Text = "k17";
			fontSizeTextBox.Select(1, 0);

			m_dialog.ApplyNewFontSizeIfValid("k17");

			Assert.That(fontSizeTextBox.SelectionStart, Is.EqualTo(0), "Insertion point should have been put back where it was.");
		}

		/// <summary />
		[Test]
		public void ApplyNewFontSizeIfValid_InsertInvalidCharInMiddle_InsertionPointNotChanged()
		{
			TextBox fontSizeTextBox;
			const int initialFontSize = 17;
			ApplyNewFontSizeIfValid_Helper_SetupInsertionPoint(initialFontSize, out fontSizeTextBox);

			// User clicks in middle and types "k"
			fontSizeTextBox.Select(1, 0);
			fontSizeTextBox.Text = "1k7";
			fontSizeTextBox.Select(2, 0);
			m_dialog.ApplyNewFontSizeIfValid("1k7");

			Assert.That(fontSizeTextBox.SelectionStart, Is.EqualTo(1), "Insertion point should have been put back where it was.");
		}

		/// <summary />
		[Test]
		public void ApplyNewFontSizeIfValid_Append4thDigit_InsertionPointNotChanged()
		{
			TextBox fontSizeTextBox;
			const int initialFontSize = 178;
			ApplyNewFontSizeIfValid_Helper_SetupInsertionPoint(initialFontSize, out fontSizeTextBox);

			// User clicks at end and types 9
			fontSizeTextBox.Select(3, 0);
			fontSizeTextBox.Text = "1789";
			fontSizeTextBox.Select(4, 0);
			m_dialog.ApplyNewFontSizeIfValid("1789");

			Assert.That(fontSizeTextBox.SelectionStart, Is.EqualTo(3), "Insertion point should have been put back where it was.");
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
			TextBox fontSizeTextBox;
			const int initialFontSize = 9;
			ApplyNewFontSizeIfValid_Helper_SetupInsertionPoint(initialFontSize, out fontSizeTextBox);

			// User clicks at end and types backspace
			fontSizeTextBox.Select(1, 0);
			fontSizeTextBox.Text = "";
			fontSizeTextBox.Select(0, 0);
			m_dialog.ApplyNewFontSizeIfValid("");

			Assert.That(fontSizeTextBox.SelectionStart, Is.EqualTo(0));
		}

		/// <summary />
		[Test]
		public void ApplyNewFontSizeIfValid_UserTypesJunkOverSelection_TextUnchanged()
		{
			TextBox fontSizeTextBox;
			const int initialFontSize = 123;
			ApplyNewFontSizeIfValid_Helper_SetupInsertionPoint(initialFontSize, out fontSizeTextBox);

			// User clicks between 1 and 2, drags to between 2 and 3, and types "a".
			fontSizeTextBox.Select(1, 1);
			fontSizeTextBox.Text = "1a3";
			fontSizeTextBox.Select(2, 0);
			m_dialog.ApplyNewFontSizeIfValid("1a3");

			var resultingFontSize = m_dialog.FontSize;
			Assert.That(resultingFontSize, Is.EqualTo(initialFontSize), "Should not have changed font size.");

			Assert.That(fontSizeTextBox.SelectionStart, Is.EqualTo(1));
		}

		/// <summary />
		[Test]
		public void OnSelectedFontSizesIndexChanged_UpdatesFontSizeAndTextBox()
		{
			var fontSizeTextBox = m_dialog.FontSizeTextBox;
			var lbFontSizes = m_dialog.FontSizesListBox;
			var fourthSize = lbFontSizes.Items[4];
			var fifthSize = lbFontSizes.Items[5];
			Assert.That(fourthSize, Is.Not.EqualTo(fifthSize), "Not a good unit test.");

			lbFontSizes.SelectedIndex = 4;
			m_dialog.OnSelectedFontSizesIndexChanged(null, null);
			Assert.That(fontSizeTextBox.Text, Is.EqualTo(fourthSize));
			Assert.That(m_dialog.FontSize.ToString(), Is.EqualTo(fourthSize));

			lbFontSizes.SelectedIndex = 5;
			m_dialog.OnSelectedFontSizesIndexChanged(null, null);
			Assert.That(fontSizeTextBox.Text, Is.EqualTo(fifthSize));
			Assert.That(m_dialog.FontSize.ToString(), Is.EqualTo(fifthSize));
		}

		private sealed class FakeFwFontDialog : FwFontDialog
		{
			internal FakeFwFontDialog(IHelpTopicProvider helpTopicProvider) : base(helpTopicProvider)
			{
			}

			/// <summary>
			/// No-op
			/// </summary>
			protected override void UpdatePreview()
			{
			}
		}
	}
}