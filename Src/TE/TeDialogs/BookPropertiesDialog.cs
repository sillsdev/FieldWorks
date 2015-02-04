// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: BookPropertiesDialog.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

using System;
using System.Windows.Forms;
using System.Linq;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.TE
{
	#region BookPropertiesDialog class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for BookPropertiesDialog.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class BookPropertiesDialog : Form, IFWDisposable
	{
		#region Member Data

		private IScrBook m_currentBook;
		private IHelpTopicProvider m_helpTopicProvider;
		private System.Windows.Forms.TextBox m_txtScrBookIdText;

		// Listbox control which allows user to edit Book name and abbreviation.
		private FwMultilingualPropView m_listBookInfo;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="BookPropertiesDialog"/> class.
		/// </summary>
		/// <param name="book">the current book</param>
		/// <param name="stylesheet">The stylesheet.</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// ------------------------------------------------------------------------------------
		public BookPropertiesDialog(IScrBook book, IVwStylesheet stylesheet, IHelpTopicProvider helpTopicProvider)
		{
			m_currentBook = book;
			m_helpTopicProvider = helpTopicProvider;
			// TE-5663: make sure the book's name and abbreviation are updated if some were added
			IScrRefSystem scrRefSystem = book.Cache.ServiceLocator.GetInstance<IScrRefSystemRepository>().AllInstances().FirstOrDefault();
			book.Name.MergeAlternatives(scrRefSystem.BooksOS[book.CanonicalNum - 1].BookName);
			book.Abbrev.MergeAlternatives(scrRefSystem.BooksOS[book.CanonicalNum - 1].BookAbbrev);

			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			// Put the book name in the dialog caption
			Text = string.Format(Text, book.Name.UserDefaultWritingSystem.Text);

			m_listBookInfo.Cache = book.Cache;
			m_listBookInfo.FieldsToDisplay.Add(new FwMultilingualPropView.ColumnInfo(
				ScrBookTags.kflidName, TeResourceHelper.GetResourceString("kstidBookNameColHeader"), 60));
			m_listBookInfo.FieldsToDisplay.Add(new FwMultilingualPropView.ColumnInfo(
				ScrBookTags.kflidAbbrev, TeResourceHelper.GetResourceString("kstidBookAbbrevColHeader"), 40));
			m_listBookInfo.RootObject = book.Hvo;

			foreach (WritingSystem ws in book.Cache.ServiceLocator.WritingSystems.AllWritingSystems)
				m_listBookInfo.WritingSystemsToDisplay.Add(ws.Handle);

			// Initialize the ID textbox.
			m_txtScrBookIdText.Text = m_currentBook.IdText;
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

#if DEBUG
		/// <summary/>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			base.Dispose(disposing);
		}
#endif
		#endregion

		#region InitializeComponent
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Forms designer method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			System.Windows.Forms.Label m_lblScrBookIdText;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BookPropertiesDialog));
			System.Windows.Forms.Button m_btnCancel;
			System.Windows.Forms.Button m_btnOk;
			System.Windows.Forms.Button m_btnHelp;
			System.Windows.Forms.Label label1;
//			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
			this.m_txtScrBookIdText = new System.Windows.Forms.TextBox();
			this.m_listBookInfo = new SIL.FieldWorks.Common.Widgets.FwMultilingualPropView();
			m_lblScrBookIdText = new System.Windows.Forms.Label();
			m_btnCancel = new System.Windows.Forms.Button();
			m_btnOk = new System.Windows.Forms.Button();
			m_btnHelp = new System.Windows.Forms.Button();
			label1 = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.m_listBookInfo)).BeginInit();
			this.SuspendLayout();
			//
			// m_lblScrBookIdText
			//
			resources.ApplyResources(m_lblScrBookIdText, "m_lblScrBookIdText");
			m_lblScrBookIdText.Name = "m_lblScrBookIdText";
			//
			// m_btnCancel
			//
			resources.ApplyResources(m_btnCancel, "m_btnCancel");
			m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			m_btnCancel.Name = "m_btnCancel";
			//
			// m_btnOk
			//
			resources.ApplyResources(m_btnOk, "m_btnOk");
			m_btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			m_btnOk.Name = "m_btnOk";
			m_btnOk.Click += new System.EventHandler(this.m_btnOk_Click);
			//
			// m_btnHelp
			//
			resources.ApplyResources(m_btnHelp, "m_btnHelp");
			m_btnHelp.Name = "m_btnHelp";
			m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// label1
			//
			resources.ApplyResources(label1, "label1");
			label1.Name = "label1";
			//
			// m_txtScrBookIdText
			//
			resources.ApplyResources(this.m_txtScrBookIdText, "m_txtScrBookIdText");
			this.m_txtScrBookIdText.Name = "m_txtScrBookIdText";
			//
			// m_listBookInfo
			//
			this.m_listBookInfo.AllowUserToAddRows = false;
			resources.ApplyResources(this.m_listBookInfo, "m_listBookInfo");
			this.m_listBookInfo.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.m_listBookInfo.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
			this.m_listBookInfo.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.m_listBookInfo.CausesValidation = false;
			this.m_listBookInfo.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
			this.m_listBookInfo.Name = "m_listBookInfo";
			this.m_listBookInfo.RowHeadersVisible = false;
			//
			// BookPropertiesDialog
			//
			this.AcceptButton = m_btnOk;
			resources.ApplyResources(this, "$this");
			this.CancelButton = m_btnCancel;
			this.Controls.Add(this.m_listBookInfo);
			this.Controls.Add(label1);
			this.Controls.Add(m_btnHelp);
			this.Controls.Add(m_btnOk);
			this.Controls.Add(m_btnCancel);
			this.Controls.Add(this.m_txtScrBookIdText);
			this.Controls.Add(m_lblScrBookIdText);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "BookPropertiesDialog";
			this.ShowInTaskbar = false;
			((System.ComponentModel.ISupportInitialize)(this.m_listBookInfo)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the book properties on the current ScrBook and ScrBookRef.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void UpdateBookProperties()
		{
			// Save book names and abbreviations to the current ScrBook
			m_listBookInfo.SaveMultiLingualStrings();

			// All new settings in the book should now be written to the ScrBookRef
			m_currentBook.BookIdRA.BookName.CopyAlternatives(m_currentBook.Name);
			m_currentBook.BookIdRA.BookAbbrev.CopyAlternatives(m_currentBook.Abbrev);

			IScrRefSystem scrRefSystem = m_currentBook.Cache.ServiceLocator.GetInstance<IScrRefSystemRepository>().AllInstances().FirstOrDefault();
			IScrBookRef scrBookRef = scrRefSystem.BooksOS[m_currentBook.CanonicalNum - 1];
			scrBookRef.BookName.CopyAlternatives(m_currentBook.Name);
			scrBookRef.BookAbbrev.CopyAlternatives(m_currentBook.Abbrev);
		}
		#endregion

		#region Events
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the OK button click
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_btnOk_Click(object sender, System.EventArgs e)
		{
			//m_currentBook.Name.VernacularDefaultWritingSystem = m_fweditBookName.Text;
			//m_currentBook.Abbrev.VernacularDefaultWritingSystem = m_fweditBookAbbrev.Text;
			m_currentBook.IdText = m_txtScrBookIdText.Text;
			UpdateBookProperties();
			Close();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Open the help window when the help button is pressed.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_btnHelp_Click(object sender, System.EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khptBookProperties");
		}
		#endregion
	}
	#endregion
}
