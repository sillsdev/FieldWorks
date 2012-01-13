// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2003' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ScrPassageControlTest.cs
// --------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;

using NUnit.Framework;
using SILUBS.SharedScrUtils;

namespace SILUBS.SharedScrControls
{
	#region Dummy test classes for accessing protected properties/methods
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Dummy test class for testing <see cref="ScrPassageControl"/>
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DummyScrPassageControl: ScrPassageControl
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DummyScrPassageControl"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DummyScrPassageControl(ScrReference initialRef) : base(initialRef, null, ScrVers.English)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new <see cref="ScrPassageDropDown"/> object
		/// </summary>
		/// <param name="owner">The owner</param>
		/// <returns>A new object</returns>
		/// <remarks>Added this method to allow test class create it's own derived control
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected override ScrPassageDropDown CreateScrPassageDropDown(ScrPassageControl owner)
		{
			return new DummyScrPassageDropDown(owner, ScrVers.English);
		}

// Method is never used and produces a warning when compiled on Mono.
#if false
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the component.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			this.SuspendLayout();

			// DummyScrPassageControl
			//
			this.Controls.AddRange(new System.Windows.Forms.Control[] {	  this.btnScrPsgDropDown,
																		  this.txtScrRef});
			this.Name = "DummyScrPassageControl";
			this.ResumeLayout(false);
		}
#endif

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulates sending a keypress to the text box portion of the control.
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		public void PerformKeyDown(KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Return)
				txtScrRef_KeyPress(null, new KeyPressEventArgs('\r'));
			else
				txtScrRef_KeyDown(null, e);
		}

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the textbox for the scripture reference
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal TextBox ReferenceTextBox
		{
			get { return txtScrRef; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulate a mouse down on the DropDown button
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void SimulateDropDownButtonClick()
		{
			btnScrPsgDropDown_MouseDown(null, new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the drop-down button portion of the control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal Control DropDownButton
		{
			get {return btnScrPsgDropDown;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the <see cref="ScrPassageDropDown"/>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal DummyScrPassageDropDown DropDownWindow
		{
			get {return (DummyScrPassageDropDown)m_dropdownForm;}
		}
		#endregion

		#region DummyScrPassageDropDown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Dummy test class for testing <see cref="ScrPassageDropDown"/>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal class DummyScrPassageDropDown : ScrPassageDropDown
		{
			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new object
			/// </summary>
			/// <param name="owner">The owner.</param>
			/// <param name="versification">The current versification to use when creating
			/// instances of ScrReference</param>
			/// --------------------------------------------------------------------------------
			public DummyScrPassageDropDown(ScrPassageControl owner,
				ScrVers versification) : base(owner, false, versification)
			{
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			///
			/// </summary>
			/// <param name="e"></param>
			/// --------------------------------------------------------------------------------
			internal void PerformKeyDown(KeyEventArgs e)
			{
				OnKeyDown(e);
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			///
			/// </summary>
			/// <param name="e"></param>
			/// --------------------------------------------------------------------------------
			protected override void OnDeactivate(EventArgs e)
			{
			}

			///  --------------------------------------------------------------------------------
			/// <summary>
			/// Sets the current button whose BCVValue property is the same as that specified.
			/// </summary>
			/// <param name="bcv">The Book, Chapter, or Verse of the button to make current.
			/// </param>
			/// ---------------------------------------------------------------------------------
			internal void SetCurrentButton(short bcv)
			{
				foreach (ScrDropDownButton button in m_buttons)
				{
					if (button.BCVValue == bcv)
					{
						m_currButton = button.Index;
						break;
					}
				}
			}

			///  --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the number of LabelButtons in the drop-down's control collection.
			/// </summary>
			/// ---------------------------------------------------------------------------------
			internal int ButtonsShowing
			{
				get
				{
					int count = 0;

					foreach (Control ctrl in this.Controls)
					{
						if (ctrl is LabelButton)
							count++;
					}

					return count;
				}
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets a value indicating whether the window will be activated when it is shown.
			/// </summary>
			/// <value></value>
			/// <returns>Always <c>true</c>.</returns>
			/// --------------------------------------------------------------------------------
			protected override bool ShowWithoutActivation
			{
				get { return true; }
			}
		}
		#endregion
	}
	#endregion

	/// <summary>
	/// Tests the Scripture Passage Control
	/// </summary>
	[TestFixture]
	public class ScrPassageControlTest
	{
		private Form m_ctrlOwner;
		private DummyScrPassageControl m_scp;
		private DummyScrPassageControl m_filteredScp;

		#region Setup methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialization called once.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public void FixtureSetup()
		{
			ScrReferenceTests.InitializeScrReferenceForTests();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public void TestSetup()
		{
			m_ctrlOwner = new Form();

			m_scp = new DummyScrPassageControl(new ScrReference(01001001, ScrVers.English));
			m_filteredScp = new DummyScrPassageControl(new ScrReference(01001001, ScrVers.English));
			m_filteredScp.Initialize(m_filteredScp.ScReference, new [] {57, 59, 65});

			m_ctrlOwner.Controls.Add(m_scp);
			m_ctrlOwner.Controls.Add(m_filteredScp);
			m_ctrlOwner.CreateControl();

			if (m_scp.DropDownWindow != null)
				m_scp.DropDownWindow.Close();

			if (m_filteredScp.DropDownWindow != null)
				m_filteredScp.DropDownWindow.Close();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// End of a test
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public void TestTearDown()
		{
#if !__MonoCS__
			// m_dbScp.SimulateDropDownButtonClick(); can cause this form to hang on close on mono.
			m_ctrlOwner.Close();
#endif
			m_ctrlOwner.Dispose();
		}
		#endregion

		#region Test methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test parsing textual references and getting the text after setting references
		/// programmatically
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ValidateReferenceText()
		{
			m_scp.Reference = "Gen 1:10";
			Assert.IsTrue(m_scp.Valid);
			m_scp.Reference = "Gen 1:31";
			Assert.IsTrue(m_scp.Valid);
			m_scp.Reference = "Gen 1:0";
			Assert.IsTrue(m_scp.Valid);

			// set to James 3:5
			m_scp.ScReference = new ScrReference(59, 3, 5, ScrVers.English);
			Assert.AreEqual("JAS 3:5", m_scp.ReferenceTextBox.Text);

			// Set to Exodus 8:20
			m_scp.ScReference = new ScrReference(2, 8, 20, ScrVers.English);
			Assert.AreEqual("EXO 8:20", m_scp.ReferenceTextBox.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that the drop-down window opens and closes properly
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ScrPassageDropDownBehaviorTests()
		{
			if (m_scp.DropDownWindow != null)
				m_scp.DropDownWindow.Close();

			Assert.IsNull(m_scp.DropDownWindow);
			m_scp.SimulateDropDownButtonClick();
			Assert.IsTrue(m_scp.DropDownWindow.Visible);
			m_scp.DropDownWindow.PerformKeyDown(new KeyEventArgs(Keys.Escape));
			Assert.IsNull(m_scp.DropDownWindow);

			// Verify that Alt-Down shows the list.
			m_scp.PerformKeyDown(new KeyEventArgs(Keys.Down | Keys.Alt));
			Assert.IsNotNull(m_scp.DropDownWindow);
			Assert.IsTrue(m_scp.DropDownWindow.Visible);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test parsing textual reference typed into control
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SettingReferenceByTypingTextTest()
		{
			m_scp.ReferenceTextBox.Text = "GEN 2:5";
			m_scp.PerformKeyDown(new KeyEventArgs(Keys.Enter));
			Assert.AreEqual(1, m_scp.ScReference.Book);
			Assert.AreEqual(2, m_scp.ScReference.Chapter);
			Assert.AreEqual(5, m_scp.ScReference.Verse);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that the reference gets resolved properly when pressing enter when the
		/// text box has focus.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ResolveReferenceOnEnter()
		{
			m_scp.ReferenceTextBox.Focus();
			m_scp.ReferenceTextBox.Text = "gen";
			m_scp.PerformKeyDown(new KeyEventArgs(Keys.Return));
			Assert.AreEqual("GEN 1:1", m_scp.ReferenceTextBox.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Resolves an incomplete reference when the user types "j" (Joshua is the first book
		/// that starts with "j")
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ResolveReference_IncompleteMultilingScrBooks()
		{
			m_scp.ReferenceTextBox.Focus();
			m_scp.ReferenceTextBox.Text = "j";
			m_scp.PerformKeyDown(new KeyEventArgs(Keys.Enter));
			Assert.AreEqual("JOS 1:1", m_scp.ReferenceTextBox.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Resolves an incomplete reference for James when the user types "j" with
		/// DBMultilingScrBooks. It is not Joshua, Judges, Job, Jeremiah, Joel, etc because these
		/// books are not in the list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ResolveReference_IncompleteInFilteredList()
		{
			m_filteredScp.ReferenceTextBox.Focus();
			m_filteredScp.ReferenceTextBox.Text = "j";
			m_filteredScp.PerformKeyDown(new KeyEventArgs(Keys.Enter));
			Assert.AreEqual("JAS 1:1", m_filteredScp.ReferenceTextBox.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test resolving an incomplete reference when the user types "q". Since no book begins
		/// with "q", it should return the first book in our project, which is Philemon.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ResolveReference_InvalidBook()
		{
			m_filteredScp.ReferenceTextBox.Focus();
			m_filteredScp.ReferenceTextBox.Text = "q";
			m_filteredScp.PerformKeyDown(new KeyEventArgs(Keys.Enter));
			Assert.AreEqual("PHM 1:1", m_filteredScp.ReferenceTextBox.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Attempts to resolve an incomplete reference when the user types "p" in the filtered
		/// control. Even though there are books in the Bible that begin with "p", since no
		/// books in the filtered list do, the first book in the list should be returned.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ResolveReference_IncompleteNotInProject()
		{
			m_filteredScp.ReferenceTextBox.Focus();
			m_filteredScp.ReferenceTextBox.Text = "p";
			m_filteredScp.PerformKeyDown(new KeyEventArgs(Keys.Enter));
			Assert.AreEqual("PHM 1:1", m_filteredScp.ReferenceTextBox.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that the reference gets resolved properly when the text box loses focus.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ResolveReferenceOnLoseFocus()
		{
			m_ctrlOwner.Visible = true;
			m_scp.ReferenceTextBox.Focus();
			m_scp.ReferenceTextBox.Text = "rev";
			m_scp.DropDownButton.Focus();
			Assert.AreEqual("REV 1:1", m_scp.ReferenceTextBox.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that the text portion is all selected when the text box gains focus.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TextAllSelectedOnFocus()
		{
			m_ctrlOwner.Visible = true;
			m_scp.DropDownButton.Focus();
			m_scp.ReferenceTextBox.Text = "REV 1:1";
			m_scp.ReferenceTextBox.Focus();
			Assert.AreEqual(0, m_scp.ReferenceTextBox.SelectionStart);
			Assert.AreEqual(7, m_scp.ReferenceTextBox.SelectionLength);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that all the books that are in the database are shown in the drop down list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerifyBookCountForFilteredList()
		{
			m_filteredScp.SimulateDropDownButtonClick();
			Assert.AreEqual(3, m_filteredScp.DropDownWindow.ButtonsShowing, "Incorrect number of books showing");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that all the books that are in the database are shown in the drop down list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FilteredListHasBooksInCanonicalOrder()
		{
			m_filteredScp.Initialize(new ScrReference(3, 3, 3, ScrVers.English), new [] {4, 5, 3, 2});
			Assert.AreEqual(4, m_filteredScp.BookLabels.Length);
			Assert.AreEqual(2, m_filteredScp.BookLabels[0].BookNum);
			Assert.AreEqual(3, m_filteredScp.BookLabels[1].BookNum);
			Assert.AreEqual(4, m_filteredScp.BookLabels[2].BookNum);
			Assert.AreEqual(5, m_filteredScp.BookLabels[3].BookNum);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that all the books that are in the database are shown in the drop down list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FilteredListPreventsDuplicateBooks()
		{
			m_filteredScp.Initialize(new ScrReference(3, 3, 3, ScrVers.English), new[] { 4, 3, 3, 4 });
			Assert.AreEqual(2, m_filteredScp.BookLabels.Length);
			Assert.AreEqual(3, m_filteredScp.BookLabels[0].BookNum);
			Assert.AreEqual(4, m_filteredScp.BookLabels[1].BookNum);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests populating drop down control - this doesn't work well on build machine, so
		/// test has been marked as being "ByHand".
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Category("ByHand")]
		public void VerifyDropDownContentWithInvalidDefault()
		{
			// Set control to really invalid reference.
			m_scp.Reference = "DAVID 100:100";
			m_scp.SimulateDropDownButtonClick();

			WaitForDropDownWindow(m_scp, 66);

			// Verify Genesis is the current and default book.
			Assert.AreEqual(1, m_scp.DropDownWindow.CurrentButtonValue,
				"Incorrect Current Book Button");
			Assert.AreEqual(1, m_scp.DropDownWindow.CurrentBook, "Incorrect Current Book");
			Assert.AreEqual(ScrPassageDropDown.ListTypes.Books,
				m_scp.DropDownWindow.CurrentListType, "Incorrect List is showing");

			Assert.AreEqual(66, m_scp.DropDownWindow.ButtonsShowing,
				"Incorrect number of books showing");

			// Choose Deuteronomy and move to the chapter list.
			m_scp.DropDownWindow.SetCurrentButton(5);
			m_scp.DropDownWindow.PerformKeyDown(new KeyEventArgs(Keys.Enter));

			// Verify the contents of the passage control's text box.
			Assert.AreEqual("DEU 1:1", m_scp.ReferenceTextBox.Text.ToUpper());

			// Verify that chapter 1 is current and default chapter.
			Assert.AreEqual(1, m_scp.DropDownWindow.CurrentButtonValue,
				"Incorrect Current Chapter Button");
			Assert.AreEqual(1, m_scp.DropDownWindow.CurrentChapter, "Incorrect Current Chapter");
			Assert.AreEqual(ScrPassageDropDown.ListTypes.Chapters,
				m_scp.DropDownWindow.CurrentListType, "Incorrect List is showing");

			// Should be 34 chapters showing
			Assert.AreEqual(34, m_scp.DropDownWindow.ButtonsShowing,
				"Incorrect number of chapters showing");

			// Choose Chapter 17 and move to the verse list.
			m_scp.DropDownWindow.SetCurrentButton(17);
			m_scp.DropDownWindow.PerformKeyDown(new KeyEventArgs(Keys.Enter));

			// Verify the contents of the passage control's text box.
			Assert.AreEqual("DEU 17:1", m_scp.ReferenceTextBox.Text.ToUpper());

			// Verify that verse 1 is current and default verse.
			Assert.AreEqual(1, m_scp.DropDownWindow.CurrentButtonValue,
				"Incorrect Current Verse Button");
			Assert.AreEqual(1, m_scp.DropDownWindow.CurrentVerse, "Incorrect Current Verse");
			Assert.AreEqual(ScrPassageDropDown.ListTypes.Verses,
				m_scp.DropDownWindow.CurrentListType, "Incorrect List is showing");

			// Should be 20 verses showing
			Assert.AreEqual(20, m_scp.DropDownWindow.ButtonsShowing,
				"Incorrect number of verses showing");

			// Choose verse 13, press enter and verify the drop-down disappears.
			m_scp.DropDownWindow.SetCurrentButton(13);
			m_scp.DropDownWindow.PerformKeyDown(new KeyEventArgs(Keys.Enter));
			Assert.IsNull(m_scp.DropDownWindow, "Drop-down should not be visible");

			// Verify the contents of the passage control's text box and it's reference object.
			Assert.AreEqual("DEU 17:13", m_scp.ReferenceTextBox.Text.ToUpper());
			Assert.AreEqual("DEU 17:13", m_scp.Reference);
			Assert.AreEqual("DEU 17:13", m_scp.ScReference.AsString.ToUpper());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests populating drop down control - this doesn't work well on build machine, so
		/// test has been marked as being "ByHand".
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Category("ByHand")]
		public void VerifyEscapeBehavior()
		{
			// Set control to really invalid reference.
			m_scp.Reference = "DAVID 100:100";
			m_scp.SimulateDropDownButtonClick();
			WaitForDropDownWindow(m_scp, 66);

			// Move to chapter list and verify content in the passage control's text box.
			m_scp.DropDownWindow.PerformKeyDown(new KeyEventArgs(Keys.Enter));
			Assert.AreEqual("GEN 1:1", m_scp.ReferenceTextBox.Text.ToUpper());

			// Move to verse list and verify content in the passage control's text box.
			m_scp.DropDownWindow.PerformKeyDown(new KeyEventArgs(Keys.Enter));
			Assert.AreEqual("GEN 1:1", m_scp.ReferenceTextBox.Text.ToUpper());

			// Escape from the drop-down and verify that the drop-down goes away.
			m_scp.DropDownWindow.PerformKeyDown(new KeyEventArgs(Keys.Escape));
			Assert.IsNull(m_scp.DropDownWindow, "Drop-down should not be visible");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests populating drop down control - this doesn't work well on build machine, so
		/// test has been marked as being "ByHand".
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Category("ByHand")]
		public void VerifyClickingOutsideDropdownBehavior()
		{
			// Set control to really invalid reference.
			m_scp.Reference = "DAVID 100:100";
			m_scp.SimulateDropDownButtonClick();

			WaitForDropDownWindow(m_scp, 66);

			// Move to chapter list and verify content in the passage control's text box.
			m_scp.DropDownWindow.PerformKeyDown(new KeyEventArgs(Keys.Enter));
			Assert.AreEqual("GEN 1:1", m_scp.ReferenceTextBox.Text.ToUpper());

			// Move to verse list and verify content in the passage control's text box.
			m_scp.DropDownWindow.PerformKeyDown(new KeyEventArgs(Keys.Enter));
			Assert.AreEqual("GEN 1:1", m_scp.ReferenceTextBox.Text.ToUpper());

			// Close the drop-down and verify the control's text box has the reference that
			// was selected so far.
			m_scp.DropDownWindow.Close();
			Assert.AreEqual("GEN 1:1", m_scp.ReferenceTextBox.Text.ToUpper());
			Assert.IsNull(m_scp.DropDownWindow, "Drop-down should not be visible");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests selecting books, chapters, and verses using the keyboard in the drop down
		/// control - this doesn't work well on build machine, so test has been marked as being
		/// "ByHand".
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Category("ByHand")]
		public void VerifyKeyboardAcceleratorDropdownBehavior()
		{
			// Set control to really invalid reference.
			m_scp.Reference = "DAVID 100:100";
			m_scp.SimulateDropDownButtonClick();

			WaitForDropDownWindow(m_scp, 66);

			// Select a book using the keyboard.
			m_scp.DropDownWindow.PerformKeyDown(new KeyEventArgs(Keys.Q));
			Assert.AreEqual(1, m_scp.DropDownWindow.CurrentButtonValue);
			m_scp.DropDownWindow.PerformKeyDown(new KeyEventArgs(Keys.J));
			Assert.AreEqual(6, m_scp.DropDownWindow.CurrentButtonValue);
			m_scp.DropDownWindow.PerformKeyDown(new KeyEventArgs(Keys.J));
			Assert.AreEqual(7, m_scp.DropDownWindow.CurrentButtonValue);
			m_scp.DropDownWindow.PerformKeyDown(new KeyEventArgs(Keys.U));
			Assert.AreEqual(7, m_scp.DropDownWindow.CurrentButtonValue);

			// Move to chapter list and verify content in the passage control's text box.
			m_scp.DropDownWindow.PerformKeyDown(new KeyEventArgs(Keys.Enter));
			Assert.AreEqual(1, m_scp.DropDownWindow.CurrentButtonValue);

			// Select a chapter using the keyboard.
			m_scp.DropDownWindow.PerformKeyDown(new KeyEventArgs(Keys.D0));
			Assert.AreEqual(1, m_scp.DropDownWindow.CurrentButtonValue);
			m_scp.DropDownWindow.PerformKeyDown(new KeyEventArgs(Keys.D1));
			Assert.AreEqual(10, m_scp.DropDownWindow.CurrentButtonValue);
			m_scp.DropDownWindow.PerformKeyDown(new KeyEventArgs(Keys.D1));
			Assert.AreEqual(11, m_scp.DropDownWindow.CurrentButtonValue);
			m_scp.DropDownWindow.PerformKeyDown(new KeyEventArgs(Keys.D1));
			Assert.AreEqual(12, m_scp.DropDownWindow.CurrentButtonValue);
			m_scp.DropDownWindow.PerformKeyDown(new KeyEventArgs(Keys.D2));
			Assert.AreEqual(20, m_scp.DropDownWindow.CurrentButtonValue);
			m_scp.DropDownWindow.PerformKeyDown(new KeyEventArgs(Keys.D2));
			Assert.AreEqual(21, m_scp.DropDownWindow.CurrentButtonValue);
			m_scp.DropDownWindow.PerformKeyDown(new KeyEventArgs(Keys.Q));
			Assert.AreEqual(21, m_scp.DropDownWindow.CurrentButtonValue);

			// Move to verse list and verify content in the passage control's text box.
			m_scp.DropDownWindow.PerformKeyDown(new KeyEventArgs(Keys.Enter));
			Assert.AreEqual(1, m_scp.DropDownWindow.CurrentButtonValue);

			// Select a verse using the keyboard.
			m_scp.DropDownWindow.PerformKeyDown(new KeyEventArgs(Keys.D3));
			Assert.AreEqual(3, m_scp.DropDownWindow.CurrentButtonValue);
			m_scp.DropDownWindow.PerformKeyDown(new KeyEventArgs(Keys.D3));
			Assert.AreEqual(3, m_scp.DropDownWindow.CurrentButtonValue);
			m_scp.DropDownWindow.PerformKeyDown(new KeyEventArgs(Keys.D1));
			Assert.AreEqual(10, m_scp.DropDownWindow.CurrentButtonValue);
			m_scp.DropDownWindow.PerformKeyDown(new KeyEventArgs(Keys.D1));
			Assert.AreEqual(11, m_scp.DropDownWindow.CurrentButtonValue);

			// Close the drop-down and verify the control's text box has the reference that
			// was selected so far.
			m_scp.DropDownWindow.Close();
			Assert.AreEqual("JDG 21:11", m_scp.ReferenceTextBox.Text.ToUpper());
			Assert.IsNull(m_scp.DropDownWindow, "Drop-down should not be visible");
		}
		#endregion

		#region helper methods

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tries the DoEvents a few times to give the DropDownWindow a chance to become active.
		/// Tests were occassionally failing due to a null DropDownWindow reference.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static void WaitForDropDownWindow(DummyScrPassageControl spc, int expectedCount)
		{
			int i = 0;
			do
			{
				Application.DoEvents();
				if (spc.DropDownWindow != null && spc.DropDownWindow.Menu != null &&
					spc.DropDownWindow.Menu.MenuItems.Count == expectedCount)
					break;
				i++;
			}
			while (i < 20);
		}

		#endregion
	}
}
