using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Resources;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// Summary description for ScrPassageControl.
	/// </summary>
	public class ScrPassageControl : System.Windows.Forms.UserControl, IMessageFilter
	{
		private const int kButtonWidthOnToolstrip = 11;

		#region Data members
		private System.ComponentModel.IContainer components;

		private bool m_mouseDown = false;
		private bool m_buttonHot = false;
		private bool m_textBoxHot = false;

		//TODO: Review to maybe make these private
		/// <summary></summary>
		protected IMultilingScrBooks m_mulScrBooks;
		/// <summary></summary>
		protected IScripture m_scripture;
		VersificationTable m_versTable;
		/// <summary></summary>
		protected System.Windows.Forms.Form m_dropdownForm;

		//	bool fDeuteroCanonical {set;} // true if Deutero-canonical ref's are to be processed

		private BookLabel[] m_rgblBookNames;
		private int[] m_rgnAvailableBooks; //array of available book nums
		private ScrReference m_scRef;
		private int[] m_rgnEncodings;
		private bool m_fParentIsToolstrip = false;

		/// <summary></summary>
		protected ILgWritingSystemFactory m_wsf;
		/// <summary></summary>
		protected System.Windows.Forms.TextBox txtScrRef;
		private ToolTip toolTip1;
		/// <summary></summary>
		protected System.Windows.Forms.Panel btnScrPsgDropDown;
		/// <summary></summary>
		public event PassageChangedHandler PassageChanged;
		/// <summary></summary>
		public delegate void PassageChangedHandler(ScrReference newReference);
		#endregion

		#region Construction, Destruction, and initialization
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Default constructor, needed for Designer (really?)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ScrPassageControl() : this(null, null, false)
		{
		//    Debug.Assert(DesignMode);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Non-default constructor
		/// </summary>
		/// <param name="reference">Initial reference</param>
		/// <param name="scr">Scripture project</param>
		/// <param name="useBooksFromDB">Flag indicating whether to display the list of books
		/// available in the database or show all books in the Canon of Scripture.</param>
		/// ------------------------------------------------------------------------------------
		public ScrPassageControl(ScrReference reference, IScripture scr, bool useBooksFromDB)
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.DoubleBuffer |
				ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);

			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			if (DesignMode)
				return;

			if (scr != null)
				Initialize(reference, scr, useBooksFromDB);

			// TODO: Add any initialization after the InitForm call
			m_dropdownForm = null;

#if __MonoCS__ // Setting MinumumSize allows mono's buggy ToolStrip layout of ToolStripControlHost's to work.
			MinimumSize = new Size(100, 20);
#endif
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialization, either from constructor or elsewhere following the default constructor.
		/// </summary>
		/// <param name="reference">Initial reference</param>
		/// <param name="scr">Scripture project</param>
		/// <param name="useBooksFromDB">Flag indicating whether to display the list of books
		/// available in the database or show all books in the Canon of Scripture.</param>
		/// ------------------------------------------------------------------------------------
		public void Initialize(ScrReference reference, IScripture scr, bool useBooksFromDB)
		{
			Debug.Assert(scr != null);
			m_scripture = scr;
			m_wsf = scr.Cache.WritingSystemFactory;

			if (useBooksFromDB)
			{
				m_mulScrBooks = new DBMultilingScrBooks(scr);
				scr.BooksChanged += BooksChanged;
			}
			else
				m_mulScrBooks = new MultilingScrBooks(scr.ScrProjMetaDataProvider);

			m_mulScrBooks.InitializeWritingSystems(m_wsf);

			if (m_rgnAvailableBooks != null)
				InitializeBookLabels();
			else
			{
				BookLabels = m_mulScrBooks.GetBookNames(true);
				m_rgnAvailableBooks = new int[m_mulScrBooks.ScriptureBooksLim];
			}

			if (reference != null && !reference.IsEmpty)
				ScReference = reference;
			else if (m_rgblBookNames != null && m_rgblBookNames.Length > 0)
				ScReference = new ScrReference(m_rgblBookNames[0].BookNum, 1, 1, Versification);
			else
				ScReference = new ScrReference(0, 0, 0, Versification);

			Reference = m_mulScrBooks.GetRefString(ScReference);

			// Use a default versification scheme if one is not available.
			m_versTable = VersificationTable.Get(Versification);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Forces the control to reload the book names based on the current Scripture project.
		/// This is a no-op for a control that is not displaying a list of books available in
		/// the DB.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void BooksChanged(ICmObject sender)
		{
			BookLabels = m_mulScrBooks.GetBookNames(true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialization, either from constructor or elsewhere following the default
		/// constructor.
		/// </summary>
		/// <param name="reference">Initial reference</param>
		/// <param name="scr">Scripture project</param>
		/// <param name="availableBooks">array of book numbers indicating those books available
		/// in the control.</param>
		/// ------------------------------------------------------------------------------------
		public void Initialize(ScrReference reference, IScripture scr, int[] availableBooks)
		{
			m_rgnAvailableBooks = availableBooks;
			Initialize(reference, scr, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Just in case we didn't remove our message filter (which would now be invalid
				// since we would be disposed) when losing focus, we remove it here (TE-8297)
				Application.RemoveMessageFilter(this);

				if (components != null)
					components.Dispose();
				if (m_scripture != null)
					m_scripture.BooksChanged -= BooksChanged;
				if (m_dropdownForm != null)
					m_dropdownForm.Dispose();
			}
			m_scripture = null;
			m_mulScrBooks = null;
			m_wsf = null;
			m_versTable = null;
			m_rgnEncodings = null;
			m_rgnAvailableBooks = null;
			m_rgblBookNames = null;
			m_dropdownForm = null;
			base.Dispose( disposing );
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitializeBookLabels()
		{
			// Get list of books in import files
			BookLabel[] bookNames = new BookLabel[m_rgnAvailableBooks.Length];
			int iName = 0;
			foreach (int bookOrd in m_rgnAvailableBooks)
				bookNames[iName++] = new BookLabel(m_mulScrBooks.GetBookName(bookOrd), bookOrd);

			BookLabels = bookNames;
		}
		#endregion

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ScrPassageControl));
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.txtScrRef = new System.Windows.Forms.TextBox();
			this.btnScrPsgDropDown = new System.Windows.Forms.Panel();
			this.SuspendLayout();
			//
			// txtScrRef
			//
			resources.ApplyResources(this.txtScrRef, "txtScrRef");
			this.txtScrRef.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.txtScrRef.Name = "txtScrRef";
			this.txtScrRef.MouseLeave += new System.EventHandler(this.txtScrRef_MouseLeave);
			this.txtScrRef.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtScrRef_KeyDown);
			this.txtScrRef.Leave += new System.EventHandler(this.txtScrRef_LostFocus);
			this.txtScrRef.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtScrRef_KeyPress);
			this.txtScrRef.Enter += new System.EventHandler(this.txtScrRef_GotFocus);
			this.txtScrRef.MouseEnter += new System.EventHandler(this.txtScrRef_MouseEnter);
			//
			// btnScrPsgDropDown
			//
			this.btnScrPsgDropDown.BackColor = System.Drawing.SystemColors.Highlight;
			resources.ApplyResources(this.btnScrPsgDropDown, "btnScrPsgDropDown");
			this.btnScrPsgDropDown.Name = "btnScrPsgDropDown";
			this.btnScrPsgDropDown.MouseLeave += new System.EventHandler(this.btnScrPsgDropDown_MouseLeave);
			this.btnScrPsgDropDown.Paint += new System.Windows.Forms.PaintEventHandler(this.btnScrPsgDropDown_Paint);
			this.btnScrPsgDropDown.MouseDown += new System.Windows.Forms.MouseEventHandler(this.btnScrPsgDropDown_MouseDown);
			this.btnScrPsgDropDown.MouseUp += new System.Windows.Forms.MouseEventHandler(this.btnScrPsgDropDown_MouseUp);
			this.btnScrPsgDropDown.MouseEnter += new System.EventHandler(this.btnScrPsgDropDown_MouseEnter);
			//
			// ScrPassageControl
			//
			this.BackColor = System.Drawing.SystemColors.Window;
			this.Controls.Add(this.btnScrPsgDropDown);
			this.Controls.Add(this.txtScrRef);
			this.Name = "ScrPassageControl";
			resources.ApplyResources(this, "$this");
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the versification.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected Paratext.ScrVers Versification
		{
			get
			{
				return (m_scripture != null) ? m_scripture.Versification :
					(m_scRef != null && m_scRef.Versification != Paratext.ScrVers.Unknown ?
					m_scRef.Versification : Paratext.ScrVers.English);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the contents of the text portion of the control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Description("The contents of the text portion of the control.")]
		public virtual string Reference
		{
			get {return txtScrRef.Text;}
			set {txtScrRef.Text = value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the array containing the primary and secondary encodings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Description("The array containing the primary & secondary encodings.")]
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int[] WritingSystems
		{
			get {return m_rgnEncodings;}
			set {m_rgnEncodings = value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public IMultilingScrBooks MulScrBooks
		{
			get {return m_mulScrBooks;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the Scripture object
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public IScripture ScriptureObject
		{
			get { return m_scripture; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the Scripture Reference object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ScrReference ScReference
		{
			get {return (m_scRef == null) ? null : new ScrReference(m_scRef);}
			set
			{
				m_scRef = new ScrReference(value);
				if (m_scRef.Valid)
					Reference = m_scRef.AsString;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the Scripture Reference object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public BookLabel[] BookLabels
		{
			get	{return m_rgblBookNames;}
			set {m_rgblBookNames = value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return a value indicating whether or not the text in the control represents a
		/// valid Scripture reference.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool Valid
		{
			get
			{
				try
				{
					return m_mulScrBooks.ParseRefString(Reference).Valid;
				}
				catch (Exception e)
				{
					throw new InstallationException(e);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the string that separates chapter numbers from verse numbers.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string ChapterVerseSepr
		{
			get {return ScriptureObject != null ? ScriptureObject.ChapterVerseSepr : ":";}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the tooltip for the scripture passage control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string ToolTip
		{
			get	{return toolTip1.GetToolTip(this);}
			set
			{
				if (value != null)
				{
					toolTip1.SetToolTip(this, value);
					toolTip1.SetToolTip(txtScrRef, value);
					toolTip1.SetToolTip(btnScrPsgDropDown, value);
				}
			}
		}

		#endregion

		#region IMessageFilter Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Because Del and Ctrl+A are menu item shortcuts, if our ref. text box has focus,
		/// then we need to trap them before the menu system gets a crack at them.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool PreFilterMessage(ref Message m)
		{
			if (m.Msg == (int)Win32.WinMsgs.WM_KEYDOWN)
			{
				if (m.WParam.ToInt32() == (int)Keys.Delete)
				{
					// There's probably a better way of passing this on to the
					// text box, but I'm hard-pressed to figure it out now.
					Win32.SendMessage(txtScrRef.Handle, m.Msg, m.WParam.ToInt32(), m.LParam.ToInt32());
					return true;
				}

				if (m.WParam.ToInt32() == (int)Keys.A && Control.ModifierKeys == Keys.Control)
				{
					txtScrRef.SelectAll();
					return true;
				}
			}

			return false;
		}

		#endregion

		#region Overriden methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the panel control behind the text box takes on the same back color as the
		/// text box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnEnabledChanged(EventArgs e)
		{
			base.OnEnabledChanged(e);
			BackColor = (this.Enabled ?	txtScrRef.BackColor : SystemColors.Control);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine whether or not this control has been placed on a toolstrip control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnParentChanged(EventArgs e)
		{
			base.OnParentChanged(e);

			Control parent = Parent;

			// Determine whether or not the control is hosted on a toolstrip.
			while (parent != null)
			{
				if (parent is ToolStrip)
				{
					m_fParentIsToolstrip = true;
					DockPadding.All = 1;
					btnScrPsgDropDown.Width = kButtonWidthOnToolstrip;
					MouseEnter += txtScrRef_MouseEnter;
					MouseLeave += txtScrRef_MouseLeave;
					SizeChanged += HandleSizeChanged;
					return;
				}

				parent = parent.Parent;
			}

			m_fParentIsToolstrip = false;
			using (TextBox txtTmp = new TextBox())
				txtScrRef.Font = txtTmp.Font.Clone() as Font;

			DockPadding.All = (Application.RenderWithVisualStyles ?
				SystemInformation.BorderSize.Width : SystemInformation.Border3DSize.Width);

			btnScrPsgDropDown.Dock = DockStyle.Right;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void HandleSizeChanged(object sender, EventArgs e)
		{
			if (m_fParentIsToolstrip)
			{
				// Make sure the height of the control when it's on a toolstrip
				// doesn't exceed the height of a normal toolstrip combo. box.
				SizeChanged -= HandleSizeChanged;
				using (ToolStripComboBox cboTmp = new ToolStripComboBox())
				{
					txtScrRef.Font = cboTmp.Font.Clone() as Font;
					Height = cboTmp.Height;
				}
				SizeChanged += HandleSizeChanged;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the dropdown control isn't left hanging around after this control goes
		/// away.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnHandleDestroyed(EventArgs e)
		{
			if (m_dropdownForm != null && m_dropdownForm.Visible)
				((ScrPassageDropDown)m_dropdownForm).Close(true);
		}

		#endregion

		#region Misc. Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///	Owners of this control may use this method to display a message box telling
		///	the user his specified Scripture reference is invalid. This is a default
		///	message and is available for convenience. Alternatively, the owner may choose
		///	to display a different message when the Scripture reference is invalid.
		///	(Note: use the Valid property to determine whether or not the specified
		///	Scripture reference is valid.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void DisplayErrorMessage()
		{
			ResourceManager resources =
				new ResourceManager("SIL.FieldWorks.Common.Controls.FwControls",
				System.Reflection.Assembly.GetExecutingAssembly());

			MessageBox.Show(this.FindForm(),
				string.Format(resources.GetString("kstidInvalidScrRefEntered"),
				Reference), "", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Resolve the reference specified in the text box
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ResolveReference()
		{
			// Trim leading and trailing spaces from the text box
			Reference = Reference.Trim();

			// Get the first reference that matches.
			// (If m_mulScrBooks is a DBMultilingScrBooks, the parse method attempts to find
			// a reference within the Scripture project.)
			ScrReference newScRef = m_mulScrBooks.ParseRefString(Reference);

			// make sure that the book is valid
			if (newScRef.Book == 0)
			{
				int spacePos = Reference.IndexOf(" ");
				if (spacePos != -1)
					txtScrRef.Select(0, spacePos);
				MiscUtils.ErrorBeep();
				return;
			}

			// Use the versification scheme to make sure the chapter number is valid.
			int lastChapter = m_versTable.LastChapter(newScRef.Book);
			if (newScRef.Chapter > lastChapter)
			{
				newScRef.Chapter = lastChapter;
				newScRef.Verse = 1;
				newScRef.Verse = m_versTable.LastVerse(newScRef.Book, lastChapter);
			}
			else
			{
				int lastVerse = m_versTable.LastVerse(newScRef.Book, newScRef.Chapter);
				// Make sure the verse number is valid
				if (newScRef.Verse > lastVerse)
					newScRef.Verse = lastVerse;
			}

			// set the text of the control to the resolved reference
			Reference = newScRef.AsString;

			if (newScRef.BBCCCVVV != ScReference.BBCCCVVV)
			{
				ScReference = newScRef;
				InvokePassageChanged(newScRef);
			}
			else
			{
				// Since the user has pressed enter we have to set the focus back to the
				// text, even if the passage didn't change.

				// HACK (EberhardB): This is a little bit of a hack. The passage hasn't actually
				// changed, but this is the easiest way to put the focus back to where it belongs.
				InvokePassageChanged(ScrReference.Empty);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified ScrReference is in the list of available books.
		/// </summary>
		/// <param name="scrRef">The given ScrReference</param>
		/// <returns><c>true</c> if the book reference is in the list of available books;
		/// otherwise, <c>false</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public bool IsReferenceValid(ScrReference scrRef)
		{
			if (BookLabels == null)
				return false;
			foreach (BookLabel bookLabel in BookLabels)
			{
				if (bookLabel.BookNum == scrRef.Book)
					return true;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Invoke the PassageChanged event
		/// </summary>
		/// <param name="reference">The reference.</param>
		/// ------------------------------------------------------------------------------------
		private void InvokePassageChanged(ScrReference reference)
		{
			Logger.WriteEvent(string.Format("New reference is {0}", reference.AsString));
			if (PassageChanged != null)
				PassageChanged(new ScrReference(reference));
		}
		#endregion

		#region Drop-down handling methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new <see cref="ScrPassageDropDown"/> object
		/// </summary>
		/// <param name="owner">The owner</param>
		/// <returns>A new object</returns>
		/// <remarks>Added this method to allow test class create it's own derived control
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected virtual ScrPassageDropDown CreateScrPassageDropDown(ScrPassageControl owner)
		{
			return new ScrPassageDropDown(owner, false, Versification);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void DisplayDropDown()
		{
			// Create, position, and display the drop-down form.
			ScrPassageDropDown spDropDown = CreateScrPassageDropDown(this);
			PositionDropDown(spDropDown);
			spDropDown.Closed += new System.EventHandler(DropDownClosed);
			spDropDown.Deactivate += new System.EventHandler(DropDownDeactivated);

			spDropDown.BookSelected +=
				new ScrPassageDropDown.BookSelectedHandler(DropDownBookSelected);

			spDropDown.ChapterSelected +=
				new ScrPassageDropDown.ChapterSelectedHandler(DropDownChapterSelected);

			m_dropdownForm = spDropDown;
			m_dropdownForm.Show();

			// Select the book portion of the reference in the text box.
			txtScrRef.HideSelection = false;
			int i = Reference.LastIndexOf(' ');
			if (i >= 0)
			{
				txtScrRef.SelectionStart = 0;
				txtScrRef.SelectionLength = i;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Position the drop down control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void PositionDropDown(ScrPassageDropDown dropDown)
		{
			Point screenPoint = PointToScreen(new Point(0, 0));

			// If there is no room below the ScrPassageControl for the drop down then
			// position above, otherwise position below.
			if (DropDownShouldGoUp(screenPoint, dropDown))
				screenPoint.Y -= dropDown.Height;
			else
				screenPoint.Y += Height;
			dropDown.DesktopLocation = screenPoint;

			// Make sure that the drop down fits on the screen.
			Rectangle rect = new Rectangle(dropDown.DesktopLocation,
				new Size(dropDown.Width, dropDown.Height));
			ScreenUtils.EnsureVisibleRect(ref rect);
			dropDown.DesktopLocation = new Point(rect.Left, rect.Top);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the drop down should go above the ScrPassageControl or
		/// below it.
		/// </summary>
		/// <param name="screenPoint">point on the screen of the</param>
		/// <param name="dropDown">drop down control</param>
		/// <returns>true to go above, false to go below</returns>
		/// ------------------------------------------------------------------------------------
		private bool DropDownShouldGoUp(Point screenPoint, ScrPassageDropDown dropDown)
		{
			// determine the usable space on the screen that contains the top left
			// corner of the ScrPassageControl.
			Rectangle rcAllowable = ScreenUtils.AdjustedWorkingArea(Screen.FromPoint(screenPoint));

			// If there is not enough space to go down, then go up
			return (rcAllowable.Height - screenPoint.Y - Height - dropDown.Height < 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void DropDownBookSelected(int book)
		{
			// If the book has changed, parse the new reference and display it in the text box.
			if (ScReference.Book != book)
			{
				Reference = m_mulScrBooks.GetRefString(new ScrReference(book, 1, 1,
					Versification));
			}

			// Select the chapter portion of the reference in the text box.
			int space = Reference.LastIndexOf(' ');
			int sepr = Reference.LastIndexOf(ChapterVerseSepr);

			if (space >= 0 && sepr >= 0 && sepr > space)
			{
				txtScrRef.SelectionStart = space + 1;
				txtScrRef.SelectionLength = sepr - space - 1;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void DropDownChapterSelected(int book, int chapter)
		{
			// If the book or chapter have changed, parse the new reference and display it
			// in the text box.
			if (ScReference.Book != book || ScReference.Chapter != chapter)
			{
				Reference = m_mulScrBooks.GetRefString(
					new ScrReference(book, chapter, 1, Versification));
			}

			// Select the verse portion of the reference in the text box.
			int sepr = Reference.LastIndexOf(ChapterVerseSepr);

			if (sepr >= 0)
			{
				txtScrRef.SelectionStart = sepr + 1;
				txtScrRef.SelectionLength = Reference.Length - sepr;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is the method that the dropdown will call to notify me that it is deactivated.
		/// Deactivating the dropdown should cause it to close.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void DropDownDeactivated(object sender, EventArgs e)
		{
			if (m_dropdownForm != null)
				((ScrPassageDropDown)m_dropdownForm).Close(true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is the method that the dropdown will call to notify me that it is closed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void DropDownClosed(object sender, EventArgs e)
		{
			if (m_dropdownForm == null)
				return;

			bool fCanceled = ((ScrPassageDropDown)m_dropdownForm).Canceled;
			ScrReference curRef = ((ScrPassageDropDown)m_dropdownForm).CurrentScRef;
			m_dropdownForm = null;

			// If the drop-down wasn't canceled, then save the reference chosen from it.
			// Otherwise, restore what the reference was before showing the drop-down.
			if (fCanceled)
				Reference = ScReference.AsString;
			else
				InvokePassageChanged(curRef);

			if (m_scripture == null)
				txtScrRef.Focus();

			// If the user canceled the drop down we want to leave the focus in the combo box
			// - similar to a real combo box
			if (fCanceled)
			{
				txtScrRef.Focus();
				Focus();
			}

			txtScrRef.HideSelection = true;
			Invalidate(true);
		}

		#endregion

		#region Delegate methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void txtScrRef_MouseEnter(object sender, EventArgs e)
		{
			m_textBoxHot = true;
			Invalidate(true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void txtScrRef_MouseLeave(object sender, EventArgs e)
		{
			m_textBoxHot = false;
			Invalidate(true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void txtScrRef_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Down && (e.Modifiers & Keys.Alt) > 0)
			{
				// Show the drop-down
				e.Handled = true;
				HandleDropDown();
			}
			else if (e.KeyCode == Keys.Escape)
			{
				e.Handled = true;
				Reference = ScReference.AsString;
				// HACK (EberhardB): This is a little bit of a hack. The passage hasn't actually
				// changed, but this is the easiest way to put the focus back to where it belongs.
				InvokePassageChanged(ScrReference.Empty);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the key press to look for Enter keys.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void txtScrRef_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == (char)Keys.Enter)
			{
				e.Handled = true;
				ResolveReference();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the text box losing focus.  Resolve the reference that has been typed in.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void txtScrRef_LostFocus(object sender, System.EventArgs e)
		{
			if (!m_fParentIsToolstrip)
				ResolveReference();
			else
			{
				Application.RemoveMessageFilter(this);
				Reference = ScReference.AsString;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When gaining the focus, highlight the entire text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void txtScrRef_GotFocus(object sender, System.EventArgs e)
		{
			if (m_fParentIsToolstrip)
				Application.AddMessageFilter(this);

			txtScrRef.SelectAll();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This will center the text box vertically within the control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			int newTop = (Height - txtScrRef.Height) / 2;
			txtScrRef.Top = (newTop < 0 ? 0 : newTop);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaintBackground(e);

			if (m_fParentIsToolstrip)
			{
				PaintOnToolbar(e);
				return;
			}

			if (!Application.RenderWithVisualStyles)
				ControlPaint.DrawBorder3D(e.Graphics, ClientRectangle, Border3DStyle.Sunken);
			else
			{
				VisualStyleRenderer renderer;

				renderer = new VisualStyleRenderer(Enabled ?
					VisualStyleElement.TextBox.TextEdit.Normal :
					VisualStyleElement.TextBox.TextEdit.Disabled);

				renderer.DrawBackground(e.Graphics, ClientRectangle, e.ClipRectangle);

				// When the textbox background is drawn in normal mode (at least when the
				// theme is one of the standard XP themes), it's drawn with a white background
				// and not the System Window background color. Therefore, we need to create
				// a rectangle that doesn't include the border. Then fill it with the text
				// box's background color.
				Rectangle rc = renderer.GetBackgroundExtent(e.Graphics, ClientRectangle);
				int dx = (rc.Width - ClientRectangle.Width) / 2;
				int dy = (rc.Height - ClientRectangle.Height) / 2;
				rc = ClientRectangle;
				rc.Inflate(-dx, -dy);

				using (SolidBrush br = new SolidBrush(txtScrRef.BackColor))
					e.Graphics.FillRectangle(br, rc);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void PaintOnToolbar(PaintEventArgs e)
		{
			if ((!m_textBoxHot && m_dropdownForm == null) || !Enabled)
				return;

			Rectangle rc = ClientRectangle;
			rc.Width--;
			rc.Height--;

			using (Pen pen = new Pen(ProfessionalColors.ButtonSelectedHighlightBorder))
			{
				e.Graphics.DrawRectangle(pen, rc);

				Point pt1 = new Point(btnScrPsgDropDown.Left - 1, 0);
				Point pt2 = new Point(btnScrPsgDropDown.Left - 1, Height);
				e.Graphics.DrawLine(pen, pt1, pt2);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void btnScrPsgDropDown_Paint(object sender, PaintEventArgs e)
		{
			if (m_fParentIsToolstrip)
			{
				btnScrPsgDropDown_PaintOnToolstrip(e);
				return;
			}

			ButtonState state = ButtonState.Normal;

			VisualStyleElement element = VisualStyleElement.ComboBox.DropDownButton.Normal;
			if (!Enabled)
			{
				state = ButtonState.Inactive;
				element = VisualStyleElement.ComboBox.DropDownButton.Disabled;
			}
			else if (m_mouseDown)
			{
				state = ButtonState.Pushed;
				element = VisualStyleElement.ComboBox.DropDownButton.Pressed;
			}
			else if (m_buttonHot)
				element = VisualStyleElement.ComboBox.DropDownButton.Hot;

			if (!Application.RenderWithVisualStyles)
				PaintNonThemeButton(e.Graphics, state);
			else
			{
				VisualStyleRenderer renderer = new VisualStyleRenderer(element);
				renderer.DrawBackground(e.Graphics, btnScrPsgDropDown.ClientRectangle,
					e.ClipRectangle);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void btnScrPsgDropDown_PaintOnToolstrip(PaintEventArgs e)
		{
			Color clr1 = (Application.RenderWithVisualStyles ?
				ProfessionalColors.ToolStripGradientBegin : SystemColors.Control);

			Color clr2 = (Application.RenderWithVisualStyles ?
				ProfessionalColors.ToolStripGradientEnd : SystemColors.Control);

			if (!Enabled)
			{
				clr1 = SystemColors.Control;
				clr2 = SystemColors.Control;
			}
			else if (m_mouseDown || m_dropdownForm != null)
			{
				clr1 = ProfessionalColors.ButtonPressedGradientBegin;
				clr2 = ProfessionalColors.ButtonPressedGradientEnd;
			}
			else if (m_textBoxHot && Application.RenderWithVisualStyles)
			{
				clr1 = ProfessionalColors.ButtonSelectedGradientBegin;
				clr2 = ProfessionalColors.ButtonSelectedGradientEnd;
			}

			using (LinearGradientBrush br = new LinearGradientBrush(
				btnScrPsgDropDown.ClientRectangle, clr1, clr2,
				LinearGradientMode.Vertical))
			{
				e.Graphics.FillRectangle(br, btnScrPsgDropDown.ClientRectangle);
			}

			e.Graphics.DrawImageUnscaledAndClipped(Properties.Resources.kimidDropDownArrow,
				btnScrPsgDropDown.ClientRectangle);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void PaintNonThemeButton(Graphics graphics, ButtonState state)
		{
			ControlPaint.DrawButton(graphics, btnScrPsgDropDown.ClientRectangle, state);

			var arrow = Resources.Images.arrowdown;
			var x = btnScrPsgDropDown.ClientRectangle.Size.Width / 2 - arrow.Width / 2;
			var y = btnScrPsgDropDown.ClientRectangle.Size.Height / 2 - arrow.Height / 2;

			if (Enabled)
				graphics.DrawImage(arrow, x, y, arrow.Width, arrow.Height);
			else
				ControlPaint.DrawImageDisabled(graphics, arrow, x, y, Color.DarkGray);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void btnScrPsgDropDown_MouseEnter(object sender, System.EventArgs e)
		{
			m_buttonHot = true;

			if (!m_fParentIsToolstrip)
				btnScrPsgDropDown.Invalidate();
			else
			{
				m_textBoxHot = true;
				Invalidate(true);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void btnScrPsgDropDown_MouseLeave(object sender, System.EventArgs e)
		{
			m_buttonHot = false;

			if (!m_fParentIsToolstrip)
				btnScrPsgDropDown.Invalidate();
			else
			{
				m_textBoxHot = false;
				Invalidate(true);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected void btnScrPsgDropDown_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			// Repaint the drop down button so that it displays pressed
			if (e.Button == MouseButtons.Left)
			{
				m_mouseDown = true;
				btnScrPsgDropDown.Invalidate();
			}

			// Strangely enough - Windows drops the DropDown of a combo box in the mouse down,
			// not the click event as almost anywhere else in Windows.
			HandleDropDown();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Display the drop down if it doesn't show, or close it if it does show.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void HandleDropDown()
		{
			if (m_dropdownForm != null)
			{
				// Close the drop-down form since it is already open.
				((ScrPassageDropDown)m_dropdownForm).Close(true);
				m_dropdownForm = null;
			}
			else
			{
				// Save what's in the text box.
				txtScrRef.Tag = Reference;

				// Parse what the user has entered
				ScReference = m_mulScrBooks.ParseRefString(Reference);
				Reference = ScReference.AsString;
				DisplayDropDown();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void btnScrPsgDropDown_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			// Repaint the drop down button so that it displays normal instead of pressed
			if (m_mouseDown)
			{
				m_mouseDown = false;
				btnScrPsgDropDown.Invalidate();
			}
		}

		#endregion
	}
}
