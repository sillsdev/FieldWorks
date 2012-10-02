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
// File: ScrPassageDropDown.cs
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using SILUBS.SharedScrUtils;

namespace SILUBS.SharedScrControls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ScrPassageDropDown : DropDownContainer
	{
		#region Data members
		/// <summary>Delegate for BookSelected event.</summary>
		public delegate void BookSelectedHandler(int bookNumber);

		/// <summary>Delegate for ChapterSelected event.</summary>
		public delegate void ChapterSelectedHandler(int bookNumber, int chapterNumber);

		/// <summary>Delegate for VerseSelected event.</summary>
		public delegate void VerseSelectedHandler(int bookNumber, int chapterNumber,
			int verseNumber);

		/// <summary>Event that occurs when a book is selected.</summary>
		public event BookSelectedHandler BookSelected;

		/// <summary>Event that occurs when a chapter is selected.</summary>
		public event ChapterSelectedHandler ChapterSelected;

		/// <summary>Event that occurs when a verse is selected.</summary>
		public event VerseSelectedHandler VerseSelected;

		private byte[] m_rowsInBookDropDown = new byte[]
		{
			1,  2,  3,  4,  5,  6,  7,  8,  9,  5,  6,  6,  7,  7,  5,	//  1 - 15
			8,  6,  9, 10, 10,  7, 11,  8,  8,  9,  9,  9, 10, 10, 10,	// 16 - 30
			11, 11, 11, 12, 12, 12, 13, 13, 13, 14, 14, 14, 15, 15, 15,	// 31 - 45
			16, 16, 16, 17, 17, 17, 18, 18, 18, 19, 19, 19, 20, 20, 20,	// 46 - 60
			21, 21, 21, 22, 22, 22										// 61 - 66
		};

		private byte[] m_rowsInCVDropDown = new byte[]
		{
			1,  2,  3,  4,  5,  3,  7,  4,  3,  5, 6,   4,  7,  7,  3,	//  1 - 15
			4,  6,  6,  5,  5,  7,  6,  6,  6,  5, 7,   9,  7,  6,  6,	// 16 - 30
			8,  8, 11,  7,  7,  6, 10,  8,  8,  8, 7,   7,  9,  9,  9,	// 31 - 45
			8,  8,  8,  7, 10,  9,  9,  9,  9, 11, 8,  10, 10, 10, 10,	// 46 - 60
			9,  9,  9,  8, 11, 11, 10, 10, 10, 10, 9,   9, 11, 11, 15,	// 61 - 75
			11, 11, 13, 10, 10,  9, 12, 12, 12, 11, 11, 11, 11, 15, 10,	// 76 - 90
			13, 12, 12, 12, 12, 12, 14, 14, 11, 10, 13, 13, 13, 13, 15,	// 91 - 105
			12, 12, 12, 11, 11, 14, 14, 13, 13, 13, 13, 13, 15, 15, 15,	// 106 - 120
			11, 14, 14, 14, 14, 14, 13, 13, 13, 13, 15, 15, 15, 15, 15,	// 121 - 135
			14, 14, 14, 14, 14, 16, 16, 16, 16, 15, 15, 15, 15, 15, 15,	// 136 - 150
			14, 14, 17, 14, 16, 16, 16, 16, 16, 16, 18, 18, 15, 15, 15,	// 151 - 165
			14, 14, 14, 17, 17, 19, 16, 16, 16, 16, 16					// 166 - 176
		};

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The types of information shown in the Scripture passage drop-down.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public enum ListTypes
		{
			/// <summary>Drop-down is showing scripture books.</summary>
			Books,
			/// <summary>Drop-down is showing chapter numbers.</summary>
			Chapters,
			/// <summary>Drop-down is showing verse numbers.</summary>
			Verses
		};

		/// <summary></summary>
		protected ScrDropDownButton[] m_buttons = new ScrDropDownButton[176];
		/// <summary></summary>
		protected int m_currButton = 0;

		private Point m_saveMousePos = Point.Empty;
		private ListTypes m_nowShowing;
		private System.ComponentModel.IContainer components;
		private ScrReference m_scRef;
		private bool m_fBooksOnly;
		private ToolTip tipBook;
		private bool m_canceled = true; // Make this the default, so that simply closing control will cancel changes
		private ScrVers m_versification;
		#endregion

		#region Contructor and initialization
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ScrPassageDropDown"/> class.
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="fBooksOnly">If true, show only books without chapter and verse</param>
		/// <param name="versification">The current versification to use when creating
		/// instances of ScrReference</param>
		/// -----------------------------------------------------------------------------------
		public ScrPassageDropDown(Control owner, bool fBooksOnly, ScrVers versification)
		{
			SnapToDefaultButton = false;
			CVButtonPreferredWidth = 30;
			BookButtonPreferredWidth = 100;
			ButtonHeight = 18;
			m_versification = versification;
			InitializeComponent();
			InitializeButtons();

			AttachedControl = owner;
			m_fBooksOnly = fBooksOnly;

			// Get reference from the main control
			m_scRef = ScrPassageControl.ScReference;

			LoadBooksButtons();
			int initialBook = ScrPassageControl.ScReference.Book;

			// Verify that the book displayed in the text box portion of the scripture
			// passage control is valid. If it is, then find what button it corresponds to
			// and make that button current.
			if (ScrPassageControl.MulScrBooks.IsBookValid(initialBook) && Controls.Count > 0)
			{
				foreach (ScrDropDownButton button in m_buttons)
				{
					if (button.BCVValue == initialBook)
					{
						m_currButton = button.Index;
						button.State = ButtonState.Pushed;
						break;
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ScrPassageDropDown));
			this.tipBook = new System.Windows.Forms.ToolTip(this.components);
			this.SuspendLayout();
			//
			// ScrPassageDropDown
			//
			resources.ApplyResources(this, "$this");
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Name = "ScrPassageDropDown";
			this.ResumeLayout(false);

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitializeButtons()
		{
			for (int i = 0; i < m_buttons.Length; i++)
			{
				m_buttons[i] = new ScrDropDownButton();
				m_buttons[i].CanToggle = true;
				m_buttons[i].Visible = true;
				m_buttons[i].Index = i;
				m_buttons[i].Selected += new ScrDropDownButton.SelectedHandler(ButtonSelected);
				m_buttons[i].MouseHover += new EventHandler(ButtonHover);
				m_buttons[i].MouseMove += new MouseEventHandler(ButtonMouseMove);
				m_buttons[i].MouseEnter += new EventHandler(ButtonEnter);
			}
		}

		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the height applied to each book, chapter and verse button.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int ButtonHeight { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the preferred width of the scripture book buttons. (Note: when only
		/// one column of books is displayed, this value is ignored if it's less than 120
		/// pixels.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int BookButtonPreferredWidth { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the preferred width of the chapter and verse buttons. (Note: when 3 or
		/// fewer columns of chapters or verses are displayed, this value is ignored if the
		/// sum of button column width's is less than 120 pixels.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int CVButtonPreferredWidth { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not the mouse pointer snaps to the
		/// default button for each drop-down (i.e. Books, Chapters and Verses) that's
		/// displayed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool SnapToDefaultButton { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the ScrPassageControl associated with this drop-down.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected ScrPassageControl ScrPassageControl
		{
			get {return (ScrPassageControl)AttachedControl;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the one-based book number the cursor is over or, if the books aren't
		/// showing, the book that was already chosen is returned.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int CurrentBook
		{
			get
			{
				if (m_nowShowing == ListTypes.Books)
					return CurrentButton.BCVValue;

				return m_scRef.Book;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the chapter the cursor is over or, if the chapters aren't showing, the chapter
		/// that was already chosen is returned.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int CurrentChapter
		{
			get
			{
				if (m_nowShowing == ListTypes.Chapters)
					return CurrentButton.BCVValue;

				return m_scRef.Chapter;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the verse the cursor is over or, if the verses aren't showing, the verse that
		/// was already chosen is returned.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int CurrentVerse
		{
			get
			{
				if (m_nowShowing == ListTypes.Verses)
					return CurrentButton.BCVValue;

				return m_scRef.Verse;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not the user escaped out of the drop-down.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new bool Canceled
		{
			get
			{
				return (m_canceled);
			}
			set
			{
				m_canceled = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the drop-down's ScReference object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ScrReference CurrentScRef
		{
			get { return new ScrReference(m_scRef); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the current button's book, chapter or verse value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private ScrDropDownButton CurrentButton
		{
			get { return m_buttons[m_currButton]; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the current button's book, chapter or verse value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int CurrentButtonValue
		{
			get { return CurrentButton.BCVValue; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the current type of list the drop-down is showing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ListTypes CurrentListType
		{
			get { return m_nowShowing; }
			private set { m_nowShowing = value; }
		}

		#endregion

		#region Overrides
		///  ------------------------------------------------------------------------------------
		/// <summary>
		/// When the mouse is supposed to snap to the current button, setting the mouse's
		/// position has to be done when the form is actually visible. Therefore, this is
		/// done so it will snap to the current scripture book button.
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnActivated(EventArgs e)
		{
			base.OnActivated(e);

			if (SnapToDefaultButton)
				MoveMouseCursorToDefaultButton();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnDeactivate(EventArgs e)
		{
			base.OnDeactivate(e);
			Close();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnKeyDown(KeyEventArgs e)
		{
			int buttonToGoTo = -1;

			switch(e.KeyCode)
			{
				case Keys.Up:    buttonToGoTo = CurrentButton.ButtonAbove; break;
				case Keys.Left:  buttonToGoTo = CurrentButton.ButtonLeft;  break;
				case Keys.Right: buttonToGoTo = CurrentButton.ButtonRight; break;
				case Keys.Down:
					if ((e.Modifiers & Keys.Alt) > 0)
					{
						bool fCancel = (m_nowShowing == ListTypes.Books);
						if (fCancel)
							m_scRef = ScrReference.Empty;
						Close(fCancel);
					}
					else
						buttonToGoTo = CurrentButton.ButtonBelow;
					break;

				case Keys.Enter:
					if (m_nowShowing == ListTypes.Verses || m_fBooksOnly)
					{
						m_scRef.Verse = m_fBooksOnly ? 1 : CurrentButton.BCVValue;
						ScrPassageControl.ScReference = m_scRef;
						Close(false);
						return;
					}

					ButtonSelected(CurrentButton);
					break;

				case Keys.Escape:
					m_scRef = ScrReference.Empty;
					Close();
					break;

				default:
					if ((e.Modifiers & Keys.Alt) != 0 && (e.Modifiers & Keys.Control) != 0)
					{
						base.OnKeyDown(e);
						return;
					}

					string charPressed = ((char)e.KeyValue).ToString();
					for (int iButton = m_currButton < Controls.Count - 1 ? m_currButton + 1 : 0; iButton != m_currButton; iButton++)
					{
						if (m_buttons[iButton].Text.StartsWith(charPressed))
						{
							buttonToGoTo = iButton;
							break;
						}
						if (iButton == Controls.Count - 1)
							iButton = -1; // Keep looking from the start of the list
					}
					break;
			}

			if (buttonToGoTo > -1)
			{
				CurrentButton.ShadeWhenMouseOver = false;
				m_buttons[buttonToGoTo].ShadeWhenMouseOver = false;
				ButtonEnter(m_buttons[buttonToGoTo], null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Closes the DropDown
		/// </summary>
		/// <param name="fCancel">if set to <c>true</c> the DropDown was canceled.</param>
		/// ------------------------------------------------------------------------------------
		private void Close(bool fCancel)
		{
			m_canceled = fCancel;
			Close();
		}

		#endregion

		#region Button handling methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void LoadBooksButtons()
		{
			int numberOfButtons = ScrPassageControl.BookLabels.Length;
			if (numberOfButtons == 0)
				return;
			int buttonWidth;
			int numberRows;

			SuspendLayout();
			Controls.Clear();
			ResetPointersToSurroundingButtons();

			CurrentListType = ListTypes.Books;
			PrepareLayout(numberOfButtons, BookButtonPreferredWidth, out numberRows,
				out buttonWidth);

			Point pt = new Point(1, 1);
			int row = 0;

			for (int i = 0; i < numberOfButtons; i++)
			{
				m_buttons[i].Text = ScrPassageControl.BookLabels[i].ToString();
				m_buttons[i].Size = new Size(buttonWidth, ButtonHeight);
				m_buttons[i].Location = pt;
				m_buttons[i].State = ButtonState.Normal;
				m_buttons[i].BCVValue = ScrPassageControl.BookLabels[i].BookNum;
				m_buttons[i].TextFormat.Alignment = StringAlignment.Near;
				m_buttons[i].TextLeadingMargin = 3;
				Controls.Add(m_buttons[i]);
				pt.Y += ButtonHeight + 1;
				row++;

				if (i > 0)
					m_buttons[i].ButtonAbove = i - 1;
				if (i < numberOfButtons - 1)
					m_buttons[i].ButtonBelow = i + 1;
				if (i - numberRows >= 0)
					m_buttons[i].ButtonLeft = i - numberRows;
				if (i + numberRows < numberOfButtons)
					m_buttons[i].ButtonRight = i + numberRows;

				if (row == numberRows)
				{
					pt.Y = 1;
					pt.X += buttonWidth + 1;
					row = 0;
				}
			}

			m_saveMousePos = Point.Empty;
			ResumeLayout(false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void LoadCVButtons(List<int> list, int initialCV)
		{
			int numberOfButtons = list.Count;
			if (numberOfButtons == 0)
				return;

			int buttonWidth;
			int numberRows;

			SuspendLayout();
			Controls.Clear();
			ResetPointersToSurroundingButtons();

			PrepareLayout(numberOfButtons, CVButtonPreferredWidth, out numberRows,
				out buttonWidth);

			Point pt = new Point(1, 1);
			int row = 0;

			for (int i = 0; i < numberOfButtons; i++)
			{
				int chapNumber = list[i];
				m_buttons[i].Text = chapNumber.ToString();
				m_buttons[i].BCVValue = chapNumber;
				m_buttons[i].Size = new Size(buttonWidth, ButtonHeight);
				m_buttons[i].Location = pt;
				m_buttons[i].State = ButtonState.Normal;
				m_buttons[i].TextFormat.Alignment = StringAlignment.Center;
				m_buttons[i].TextLeadingMargin = 0;
				Controls.Add(m_buttons[i]);
				pt.Y += ButtonHeight + 1;
				row++;

				// Determine the button to make current when the list is first shown.
				if (chapNumber == initialCV)
					m_currButton = i;

				if (i > 0)
					m_buttons[i].ButtonAbove = i - 1;
				if (i < numberOfButtons - 1)
					m_buttons[i].ButtonBelow = i + 1;
				if (i - numberRows >= 0)
					m_buttons[i].ButtonLeft = i - numberRows;
				if (i + numberRows < numberOfButtons)
					m_buttons[i].ButtonRight = i + numberRows;

				if (row == numberRows)
				{
					pt.Y = 1;
					pt.X += buttonWidth + 1;
					row = 0;
				}
			}

			ButtonEnter(CurrentButton, null);
			m_saveMousePos = Point.Empty;
			ResumeLayout(false);

			if (SnapToDefaultButton)
				MoveMouseCursorToDefaultButton();
		}

		///  ------------------------------------------------------------------------------------
		/// <summary>
		/// Resets all the buttons so they aren't aware of any adjacent buttons.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ResetPointersToSurroundingButtons()
		{
			for (int i = 0; i < m_buttons.Length; i++)
			{
				m_buttons[i].ButtonAbove = -1;
				m_buttons[i].ButtonBelow = -1;
				m_buttons[i].ButtonLeft = -1;
				m_buttons[i].ButtonRight = -1;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="numberOfButtons"></param>
		/// <param name="preferredButtonWidth"></param>
		/// <param name="numberRows"></param>
		/// <param name="buttonWidth"></param>
		/// ------------------------------------------------------------------------------------
		private void PrepareLayout(int numberOfButtons, int preferredButtonWidth,
			out int numberRows, out int buttonWidth)
		{
			System.Diagnostics.Debug.Assert(numberOfButtons > 0);

			if (m_nowShowing == ListTypes.Books)
			{
				System.Diagnostics.Debug.Assert(numberOfButtons <= m_rowsInBookDropDown.Length);
				numberRows = m_rowsInBookDropDown[numberOfButtons - 1];
			}
			else
			{
				System.Diagnostics.Debug.Assert(numberOfButtons <= m_rowsInCVDropDown.Length);
				numberRows = m_rowsInCVDropDown[numberOfButtons - 1];
			}

			// Calculate the number of columns that will display.
			int numberCols = numberOfButtons / numberRows;
			if (Decimal.Remainder(numberOfButtons, numberRows) > 0)
				numberCols++;

			// Because .Net or Windows restricts the minimun width of a form (in this case the
			// the drop-down form), adjust the width of the buttons when there are fewer than
			// four columns.
			if (numberCols == 1 && preferredButtonWidth < 120)
				buttonWidth = 120;
			else if (numberCols == 2 && preferredButtonWidth < 60)
				buttonWidth = 60;
			else if (numberCols == 3 && preferredButtonWidth < 40)
				buttonWidth = 40;
			else
				buttonWidth = preferredButtonWidth;

			SetSizeAndLocationOfPopupDropDown(
				(buttonWidth * numberCols) + numberCols + 3 +
				(SystemInformation.Border3DSize.Width * 2),
				(ButtonHeight * numberRows) + numberRows + 3 +
				(SystemInformation.Border3DSize.Width * 2));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the size and location of the drop-down control.
		/// </summary>
		/// <param name="width">Width of the drop-down control.</param>
		/// <param name="height">Height of the drop-down control.</param>
		/// ------------------------------------------------------------------------------------
		private void SetSizeAndLocationOfPopupDropDown(int width, int height)
		{
			Size dropDownSize = new Size(width, height);

			if (AttachedControl == null && AttachedControl.Width < dropDownSize.Width)
			{
				Size = dropDownSize;
				return;
			}

			Point dropDownLocation =
				AttachedControl.PointToScreen(new Point(0, AttachedControl.Height));

			// Get the working area (i.e. the screen's rectangle) in which the attached
			// control lies.
			Rectangle rcScreen = Screen.GetWorkingArea(AttachedControl);

			// Check if the right edge of the drop-down goes off the screen. If so,
			// align its right edge with that of the attached control's right edge.
			// The assumption is the right edge of the control isn't off the right
			// edge of the screen. If it is, tough luck, I guess.
			if (dropDownLocation.X + dropDownSize.Width > rcScreen.Right)
				dropDownLocation.X -= (dropDownSize.Width - AttachedControl.Width);

			// Check if the bottom edge of the drop-down goes off the screen. If so,
			// align its bottom edge with that of the attached control's top edge.
			if (dropDownLocation.Y + dropDownSize.Height > rcScreen.Bottom)
				dropDownLocation.Y -= (dropDownSize.Height + AttachedControl.Height);

			Size = dropDownSize;
			Location = dropDownLocation;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Move the mouse cursor to the center of the current button.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void MoveMouseCursorToDefaultButton()
		{
			Point pt = new Point(CurrentButton.Width / 2,
				CurrentButton.Height / 2);

			Cursor.Position = CurrentButton.PointToScreen(pt);
		}
		#endregion

		#region Delegates for Button Events
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether or not a book's title is clipped. If it is, a tool-tip is
		/// activated so the user can see the entire book name.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void ButtonHover(object sender, EventArgs e)
		{
			ScrDropDownButton button = (ScrDropDownButton)sender;

			// If we're not currently showing the list of books or if the text isn't clipped
			// don't activate the tooltip.
			if (m_nowShowing != ListTypes.Books || !button.TextIsClipped)
				tipBook.Active = false;
			else
			{
				tipBook.SetToolTip(button, button.Text);
				tipBook.Active = true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void ButtonMouseMove(object sender, MouseEventArgs e)
		{
			Point pt = new Point(e.X, e.Y);

			// if the mouse didn't move to another point, then ignore this message.
			// ignore this mouse move message.
			if (m_saveMousePos == pt)
				return;

			m_saveMousePos = pt;
			ScrDropDownButton button = (ScrDropDownButton)sender;

			// Make sure the button the mouse is over will look shaded, now that the
			// mouse is over it.
			if (!button.ShadeWhenMouseOver)
				button.ShadeWhenMouseOver = true;

			// If the button the mouse is over is not the current then make sure the formerly
			// current button looks normal.
			if (button.Index != m_currButton && m_currButton > -1)
				CurrentButton.State = ButtonState.Normal;

			// Now, make the button the mouse is over the current button.
			m_currButton = button.Index;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle making the button the mouse just moved over the current button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void ButtonEnter(object sender, EventArgs e)
		{
			// Make sure the button the mouse used to be over has it's state restored.
			if (m_currButton > -1)
				CurrentButton.State = ButtonState.Normal;

			// Now make the button the mouse is over the current by making it look pushed.
			int i = ((ScrDropDownButton)sender).Index;
			m_buttons[i].State = ButtonState.Pushed;
			m_currButton = i;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the selection of a button.
		/// </summary>
		/// <param name="button">The button the user selected.</param>
		/// ------------------------------------------------------------------------------------
		protected void ButtonSelected(ScrDropDownButton button)
		{
			if (m_nowShowing == ListTypes.Books)
				InternalBookSelected(button);
			else if (m_nowShowing == ListTypes.Chapters)
				InternalChapterSelected(button.BCVValue);
			else
				InternalVerseSelected(button);
		}

		#endregion

		#region Methods called when Book, Chapter, or Verse is selected
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InternalBookSelected(ScrDropDownButton button)
		{

			// If the user picked a different book, then set the chapter and verse to 1 and
			// reparse the reference object to track with the user's selection.
			if (m_scRef.Book != button.BCVValue)
			{
				m_scRef = new ScrReference(button.BCVValue, 1, 1, m_versification);
				ScrPassageControl.Reference = m_scRef.AsString;
			}

			if (BookSelected != null)
				BookSelected(m_scRef.Book);

			if (m_fBooksOnly)
			{
				OnKeyDown(new KeyEventArgs(Keys.Return));
				return;
			}

			List<int> chapterList = CurrentChapterList;

			// If there is only one chapter then there's no sense showing the chapter list so
			// go right to the verse list. This will be the case when the user picks a book
			// like Jude.
			if (chapterList.Count == 1)
				InternalChapterSelected(chapterList[0]);
			else
			{
				// Show the list of chapters.
				CurrentListType = ListTypes.Chapters;
				LoadCVButtons(chapterList, m_scRef.Chapter);
			}
		}

		///  ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a list of all the chapters for the book specified by m_scRef.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual List<int> CurrentChapterList
		{
			get
			{
				// Get the number of chapters based on the versification scheme.
				List<int> chapterList = new List<int>();
				for (int i = 1; i <= m_scRef.LastChapter; i++)
					chapterList.Add(i);
				return chapterList;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="newChapter">The chapter</param>
		/// ------------------------------------------------------------------------------------
		private void InternalChapterSelected(int newChapter)
		{
			// If the user picked a different chapter then set the verse to 1.
			if (m_scRef.Chapter != newChapter)
			{
				m_scRef.Chapter = newChapter;
				m_scRef.Verse = 1;
				ScrPassageControl.Reference = m_scRef.AsString;
			}

			List<int> verseList = new List<int>();
			for (int i = 1; i <= m_scRef.LastVerse; i++)
				verseList.Add(i);

			if (ChapterSelected != null)
				ChapterSelected(m_scRef.Book, m_scRef.Chapter);

			CurrentListType = ListTypes.Verses;

			LoadCVButtons(verseList, m_scRef.Verse);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="button"></param>
		/// ------------------------------------------------------------------------------------
		private void InternalVerseSelected(ScrDropDownButton button)
		{
			m_currButton = (button == null ? 0 : button.Index);

			if (VerseSelected != null)
				VerseSelected(m_scRef.Book, m_scRef.Chapter, CurrentButton.BCVValue);

			OnKeyDown(new KeyEventArgs(Keys.Return));
		}
		#endregion

		#region ScrDropDownButton
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Subclass the label button for our scripture passage drop-down buttons in order to
		/// provide an index, properties for other label buttons that surround an instance
		/// of a label button, and finally, override the OnMouseUp event and provide delegates
		/// for when a button is selected.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected class ScrDropDownButton : LabelButton
		{
			internal delegate void SelectedHandler(ScrDropDownButton button);
			internal event SelectedHandler Selected;

			private int m_index = -1;
			private int m_buttonAbove = -1;
			private int m_buttonBelow = -1;
			private int m_buttonLeft = -1;
			private int m_buttonRight = -1;
			private int m_bcvValue = 0;

			#region Properties
			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets or sets the index of the button within the array of buttons in the
			/// drop-down.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public int Index
			{
				get {return m_index;}
				set {m_index = value;}
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets or sets a value that indicates what book, chapter or verse the button
			/// corresponds to. Because the buttons in the ScrPassageDropDown are reused for
			/// each list type (i.e. book, chapter and verse), the name of this property
			/// reflects that. However, the "BCV" shouldn't be confused with BCV references.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public int BCVValue
			{
				get {return m_bcvValue;}
				set {m_bcvValue = value;}
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets or sets the index of the button that directly preceeds this button.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public int ButtonAbove
			{
				get {return m_buttonAbove;}
				set {m_buttonAbove = value;}
			}
			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets or sets the index of the button that directly follows this button.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public int ButtonBelow
			{
				get {return m_buttonBelow;}
				set {m_buttonBelow = value;}
			}
			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets or sets the index of the button that's directly to the left of this
			/// button.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public int ButtonLeft
			{
				get {return m_buttonLeft;}
				set {m_buttonLeft = value;}
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets or sets the index of the button that's directly to the right of this
			/// button.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public int ButtonRight
			{
				get {return m_buttonRight;}
				set {m_buttonRight = value;}
			}
			#endregion

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Subclasses the LabelButton control for the ScrPassageDropDown class.
			/// </summary>
			/// <remarks>
			/// I used to subscribe to the label button's Click event (in the
			/// ScrPassageDropDown class) but after I did all kinds of processing due to a
			/// click event, the label button's base class OnMouseUp event occured and undid
			/// some of those things. Therefore, I override this in order to get the base
			/// classes implementation of OnMouseUp out of the way before I do what I need to
			/// in the ScrPassageDropDown class (where a bunch of these buttons are
			/// instantiated).
			/// </remarks>
			/// <param name="e"></param>
			/// --------------------------------------------------------------------------------
			protected override void OnMouseUp(MouseEventArgs e)
			{
				base.OnMouseUp(e);

				if (Selected != null)
					Selected(this);
			}
		}

		#endregion
	}
}