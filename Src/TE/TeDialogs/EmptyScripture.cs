// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: EmptyScripture.cs
// Responsibility:
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Windows.Forms;

using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.UIAdapters;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.TE
{

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Dialog box to help user when they open or start a Scripture that has no books
	/// in it yet.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class EmptyScripture : Form, IFWDisposable, IxCoreColleague
	{
		#region Option enumeration
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Options the user might choose in this dialog box that the creator of the dialog box
		/// may need to check.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public enum Option
		{
			/// <summary>An option that need not be handled by the caller of this dialog.</summary>
			Other,
			/// <summary>insert a book</summary>
			Book,
			/// <summary>import data</summary>
			Import,
			/// <summary>exit TE</summary>
			Exit
		}
		#endregion

		#region Member variables

		private int m_bookChosen = -1;
		private IHelpTopicProvider m_helpTopicProvider;
		private Option m_Option = Option.Other;
		private ITMAdapter m_tmAdapter;
		private Mediator m_savMsgMediator;
		private Form m_savAdaptersParentForm;
		private Label lblTopLabel;
		private Button btnBook;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		#endregion

		#region Construction/Deconstruction
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="EmptyScripture"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public EmptyScripture(ITMAdapter adapter, FdoCache fdoCache, IHelpTopicProvider helpTopicProvider)
		{
			InitializeComponent();

			m_helpTopicProvider = helpTopicProvider;

			if (adapter == null || adapter.MessageMediator == null)
				btnBook.Enabled = false;
			else
			{
				m_tmAdapter = adapter;

				// Save the adapter's message mediator so it can be restored when the
				// dialog closes.
				m_savMsgMediator = adapter.MessageMediator;

				// Create a new mediator for this dialog and set
				// the adapter's mediator to it.
				Mediator mediator = new Mediator();
				mediator.AddColleague(this);
				m_tmAdapter.MessageMediator = mediator;
			}

			string projectName = fdoCache.ProjectId.Name;
			lblTopLabel.Text = string.Format(lblTopLabel.Text,projectName);
		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.
		/// </param>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
				// No. The OnClosing method has already done this,
				// as well as reset the original mediator.
				/*
				if (m_menuAdapter != null && m_menuAdapter.MessageMediator != null)
					m_menuAdapter.MessageMediator.RemoveColleague(this);
				*/
			}
			m_savMsgMediator = null;

			base.Dispose( disposing );
		}
		#endregion

		#region Windows Form Designer generated code
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			System.Windows.Forms.Label label2;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EmptyScripture));
			System.Windows.Forms.Label label3;
			System.Windows.Forms.Label label4;
			System.Windows.Forms.Button btnImport;
			System.Windows.Forms.Label label5;
			System.Windows.Forms.Button btnBlank;
			System.Windows.Forms.Label label6;
			System.Windows.Forms.Button btnExit;
			System.Windows.Forms.Button btnHelp;
			this.lblTopLabel = new System.Windows.Forms.Label();
			this.btnBook = new System.Windows.Forms.Button();
			label2 = new System.Windows.Forms.Label();
			label3 = new System.Windows.Forms.Label();
			label4 = new System.Windows.Forms.Label();
			btnImport = new System.Windows.Forms.Button();
			label5 = new System.Windows.Forms.Label();
			btnBlank = new System.Windows.Forms.Button();
			label6 = new System.Windows.Forms.Label();
			btnExit = new System.Windows.Forms.Button();
			btnHelp = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// label2
			//
			resources.ApplyResources(label2, "label2");
			label2.Name = "label2";
			//
			// label3
			//
			resources.ApplyResources(label3, "label3");
			label3.Name = "label3";
			//
			// label4
			//
			resources.ApplyResources(label4, "label4");
			label4.Name = "label4";
			//
			// btnImport
			//
			resources.ApplyResources(btnImport, "btnImport");
			btnImport.Name = "btnImport";
			btnImport.Click += new System.EventHandler(this.btnImport_Click);
			//
			// label5
			//
			resources.ApplyResources(label5, "label5");
			label5.Name = "label5";
			//
			// btnBlank
			//
			resources.ApplyResources(btnBlank, "btnBlank");
			btnBlank.Name = "btnBlank";
			btnBlank.Click += new System.EventHandler(this.btnBlank_Click);
			//
			// label6
			//
			resources.ApplyResources(label6, "label6");
			label6.Name = "label6";
			//
			// btnExit
			//
			btnExit.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(btnExit, "btnExit");
			btnExit.Name = "btnExit";
			btnExit.Click += new System.EventHandler(this.btnExit_Click);
			//
			// btnHelp
			//
			resources.ApplyResources(btnHelp, "btnHelp");
			btnHelp.Name = "btnHelp";
			btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
			//
			// lblTopLabel
			//
			resources.ApplyResources(this.lblTopLabel, "lblTopLabel");
			this.lblTopLabel.Name = "lblTopLabel";
			//
			// btnBook
			//
			resources.ApplyResources(this.btnBook, "btnBook");
			this.btnBook.Name = "btnBook";
			this.btnBook.Click += new System.EventHandler(this.btnBook_Click);
			//
			// EmptyScripture
			//
			resources.ApplyResources(this, "$this");
			this.CancelButton = btnExit;
			this.ControlBox = false;
			this.Controls.Add(btnHelp);
			this.Controls.Add(btnExit);
			this.Controls.Add(label6);
			this.Controls.Add(btnBlank);
			this.Controls.Add(label5);
			this.Controls.Add(btnImport);
			this.Controls.Add(label4);
			this.Controls.Add(this.btnBook);
			this.Controls.Add(label3);
			this.Controls.Add(label2);
			this.Controls.Add(this.lblTopLabel);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "EmptyScripture";
			this.ShowInTaskbar = false;
			this.ResumeLayout(false);

		}
		#endregion

		#region IxCoreColleague Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Not used
		/// </summary>
		/// <param name="mediator"></param>
		/// <param name="configurationParameters"></param>
		/// ------------------------------------------------------------------------------------
		public void Init(Mediator mediator, System.Xml.XmlNode configurationParameters)
		{
			CheckDisposed();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the possible message targets, i.e. the view(s) we are showing
		/// </summary>
		/// <returns>Message targets</returns>
		/// ------------------------------------------------------------------------------------
		public IxCoreColleague[] GetMessageTargets()
		{
			CheckDisposed();

			// return list of view windows with focused window being the first one
			List<IxCoreColleague> targets = new List<IxCoreColleague>();
			targets.Add(this);
			return targets.ToArray();
		}

		/// <summary>
		/// Should not be called if disposed.
		/// </summary>
		public bool ShouldNotCall
		{
			get { return IsDisposed; }
		}

		/// <summary>
		/// Mediator message handling Priority
		/// </summary>
		public int Priority
		{
			get { return (int)ColleaguePriority.Medium; }
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the option that the user has chosen.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Option OptionChosen
		{
			get
			{
				CheckDisposed();
				return m_Option;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the book that the user chose to insert, if they chose to do so. -1 if they
		/// did not chose a book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int BookChosen
		{
			get
			{
				CheckDisposed();
				return m_bookChosen;
			}
		}

		#endregion

		#region Command handler for the insert book menus
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the click on the book in the book list.
		/// </summary>
		/// <param name="args"></param>
		/// <returns><c>true</c> if we handle this</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnInsertBook(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps != null && itemProps.Tag.GetType() == typeof(int))
			{
				m_bookChosen = (int)itemProps.Tag;
				m_Option = Option.Book;
				Close();
			}

			return true;
		}

		#endregion

		#region Event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// draw a horizontal line to look pretty
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			SIL.FieldWorks.Common.Drawing.LineDrawing.DrawDialogControlSeparator(
				e.Graphics, ClientRectangle, lblTopLabel.Bottom + 5);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure we remove ourselves from the message mediator.
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="SetParentForm() returns a reference")]
		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);

			if (m_tmAdapter.MessageMediator != null)
			{
				// We created this mediator, so we have to get rid of it.
				m_tmAdapter.MessageMediator.RemoveColleague(this);
				m_tmAdapter.MessageMediator.Dispose();
				m_tmAdapter.MessageMediator = null;
			}

			// Restore the menu adapter's original message mediator.
			if (m_savMsgMediator != null)
				m_tmAdapter.MessageMediator = m_savMsgMediator;

			m_tmAdapter.SetParentForm(m_savAdaptersParentForm);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When we're shown, we need to change the parent form for the toolbar/menu adapter.
		/// The reason is that this dialog is modal and the adapter belongs to TeMainWnd (i.e.
		/// it is the adapter's normal parent form). If we didn't change the adapter's parent
		/// form, then it will not allow the book menu to popup when clicking on the book
		/// button, because the adapter doesn't allow menus to popup when its parent form is
		/// not the current form and the current form is modal (in this case, this form).
		/// See TE-6793 and TE-6553
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnShown(EventArgs e)
		{
			m_savAdaptersParentForm = m_tmAdapter.SetParentForm(this);
			base.OnShown(e);
		}

//		/// ------------------------------------------------------------------------------------
//		/// <summary>
//		/// Fires when a user clicks a book in the Insert-Book menu of this
//		/// dialog box. Close the dialog box.
//		/// </summary>
//		/// <param name="sender"></param>
//		/// <param name="e"></param>`
//		/// ------------------------------------------------------------------------------------
//		private void book_Click(object sender, EventArgs e)
//		{
//			MenuItem bookMenuItem = (MenuItem)sender;
//			m_bookChosen = ((MenuItem)bookMenuItem.Parent).Index == 0 ?
//				bookMenuItem.Index + 1 : bookMenuItem.Index + 40;
//			m_Option = Option.Book;
//			Close();
//		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles a click on btnBlank (Show Blank Screen). Closes the dialog box.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void btnBlank_Click(object sender, System.EventArgs e)
		{
			Close();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles a click on btnExit. Sets the more-or-less return-value to exit.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void btnExit_Click(object sender, System.EventArgs e)
		{
			m_Option = Option.Exit;
			Close();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles a click on btnImport. Sets the more-or-less return-value to import.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void btnImport_Click(object sender, System.EventArgs e)
		{
			m_Option = Option.Import;
			Close();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles a click on btnBook. Shows a pop-up menu allowing the user to select a
		/// book of the Bible to insert.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void btnBook_Click(object sender, System.EventArgs e)
		{
			Point pt = PointToScreen(new Point(btnBook.Left, btnBook.Bottom));
			m_tmAdapter.PopupMenu("cmnuInsertBooks", pt.X, pt.Y);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void btnHelp_Click(object sender, System.EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpEmptyScripture");
		}
		#endregion
	}
}
