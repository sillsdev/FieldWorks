// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2004' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ExportXmlDialog.cs
// Responsibility:
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.Utils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SILUBS.SharedScrUtils;
using System.Collections.Generic;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.CoreImpl;
using XCore;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// ExportWhat provides access to the radio button selection of what to export via
	/// the ExportType property.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public enum ExportWhat
	{
		/// <summary>export all books in the project</summary>
		AllBooks,
		/// <summary>export only those books selected for the filter</summary>
		FilteredBooks,
		/// <summary>export only a single book, or maybe only part of that book</summary>
		SingleBook
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for ExportXmlDialog.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ExportXmlDialog : Form, IFWDisposable
	{
		#region Member data
		private RegistryStringSetting m_xmlFolder;
		private IScripture m_scr;
		private IHelpTopicProvider m_helpTopicProvider;
		private readonly int m_oldComboHeight;
		private readonly int m_gap;
		private Font m_fntVern;
		private int m_nBookForSections = -1;
		private string m_sRangeBookFmt;
		private bool m_fLoadingBookSections = false;
		private FileType m_eExportType = FileType.OXES;		// enum that we're exporting OXES.
		private TeImportExportFileDialog m_fileDialog;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		private System.Windows.Forms.TextBox m_txtOutputFile;
		private RadioButton m_rdoAllBooks;
		private RadioButton m_rdoFilteredBooks;
		private RadioButton m_rdoSingleBook;
		private Label m_lblFilterList;
		private ScrBookControl m_scrBook;
		private GroupBox m_grpSectionRange;
		private TextBox m_txtDescription;
		private Button m_btnBrowse;
		private Button m_btnOk;
		private Button m_btnCancel;
		private Label lblFile;
		private Label label2;
		private Label label3;
		private Label lblTo;
		private Label label8;
		private Label label9;
		private Label label10;
		private ComboBox cboFrom;
		private ComboBox cboTo;
		private Label lblFrom;
		private Button m_btnHelp;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:ExportXmlDialog"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ExportXmlDialog(FdoCache cache, FilteredScrBooks filter, ScrReference refBook,
			IVwStylesheet stylesheet, FileType exportType, IHelpTopicProvider helpTopicProvider)
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			m_helpTopicProvider = helpTopicProvider;
			m_scr = cache.LangProject.TranslatedScriptureOA;
			m_eExportType = exportType;

			string key;
			switch (m_eExportType)
			{
				case FileType.OXES:
					key = "ExportFolderForXML";
					break;
				case FileType.XHTML:
					Text = TeResourceHelper.GetResourceString("kstidExportXHTML"); // "Export XHTML"
					key = "ExportFolderForXhtml";
					break;
				case FileType.ODT:
					Text = TeResourceHelper.GetResourceString("kstidExportODT"); // "Export Open Office file"
					key = "ExportFolderForXhtml";
					break;
				case FileType.PDF:
					Text = TeResourceHelper.GetResourceString("kstidExportPDF"); // "Export Adobe Portable Document"
					key = "ExportFolderForXhtml";
					break;
				default:
					key = "ExportFolderForXML";
					break;
			}

			m_xmlFolder = new RegistryStringSetting(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
				"ExportFolderForXml", FwSubKey.TE);
			string fileName = m_xmlFolder.Value;

			m_fileDialog = new TeImportExportFileDialog(cache.ProjectId.Name, m_eExportType);

			// Append a filename if it was set to just a directory
			if (Directory.Exists(fileName))
				fileName = Path.Combine(fileName, m_fileDialog.DefaultFileName);
			m_txtOutputFile.Text = fileName;
			// Ensure that the end of the filename is visible.
			m_txtOutputFile.Select(fileName.Length, 0);

			FillFilterListLabel(filter);

			// Set the single book from the current one in use if possible, otherwise set
			// it from the first available book, if any are available.
			if (refBook == null)
			{
				refBook = new ScrReference();
				refBook.BBCCCVVV = 1001001;
				if (m_scr.ScriptureBooksOS.Count > 0)
					refBook.Book = m_scr.ScriptureBooksOS[0].CanonicalNum;
			}

			m_scrBook.Initialize(refBook, m_scr as IScripture, true);
			m_scrBook.PassageChanged += m_scrBook_PassageChanged;

			// Initialize the combo boxes, and then adjust their heights (and the locations
			// of following controls) as needed.
			m_oldComboHeight = cboFrom.Height;
			m_gap = cboTo.Top - cboFrom.Bottom;
			FwStyleSheet ss = stylesheet as FwStyleSheet;
			m_fntVern = new Font(ss.GetNormalFontFaceName(cache, cache.DefaultVernWs), 10);
			cboFrom.Font = cboTo.Font = m_fntVern;

			// Now that the sizes are fixed, load the combo box lists.
			LoadSectionsForBook(refBook.Book);

			m_nBookForSections = refBook.Book;
			m_sRangeBookFmt = m_grpSectionRange.Text;
			UpdateBookSectionGroupLabel();

			//m_scrBook.Enabled = false;
			//m_grpSectionRange.Enabled = false;
			m_grpSectionRange.Visible = true;

			// Initialize the description.
			DateTime now = DateTime.Now;
			// "{0} exported by {1} on {2} {3}, {4} at {5}"
			m_txtDescription.Text = String.Format(DlgResources.ResourceString("kstidOxesExportDescription"),
				cache.ProjectId.Name,
				System.Security.Principal.WindowsIdentity.GetCurrent().Name.Normalize(),
				now.ToString("MMMM"), now.Day, now.Year, now.ToShortTimeString());

			// TODO: Set export type from the stored registry setting.
		}
		#endregion

		#region IFWDisposable implementation
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		/// ------------------------------------------------------------------------------------
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
		protected override void Dispose( bool disposing )
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				if(components != null)
					components.Dispose();

				if (m_fntVern != null)
				{
					m_fntVern.Dispose();
					m_fntVern = null;
				}
				if (m_fileDialog != null)
				{
					m_fileDialog.Dispose();
					m_fileDialog = null;
				}
			}

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
			System.Windows.Forms.Label label4;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ExportXmlDialog));
			System.Windows.Forms.Label label5;
			this.lblFrom = new System.Windows.Forms.Label();
			this.lblFile = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.lblTo = new System.Windows.Forms.Label();
			this.label8 = new System.Windows.Forms.Label();
			this.label9 = new System.Windows.Forms.Label();
			this.label10 = new System.Windows.Forms.Label();
			this.m_btnBrowse = new System.Windows.Forms.Button();
			this.m_btnOk = new System.Windows.Forms.Button();
			this.m_btnCancel = new System.Windows.Forms.Button();
			this.m_btnHelp = new System.Windows.Forms.Button();
			this.m_txtOutputFile = new System.Windows.Forms.TextBox();
			this.m_rdoAllBooks = new System.Windows.Forms.RadioButton();
			this.m_rdoFilteredBooks = new System.Windows.Forms.RadioButton();
			this.m_rdoSingleBook = new System.Windows.Forms.RadioButton();
			this.m_lblFilterList = new System.Windows.Forms.Label();
			this.m_scrBook = new SIL.FieldWorks.Common.Controls.ScrBookControl();
			this.m_grpSectionRange = new System.Windows.Forms.GroupBox();
			this.cboTo = new System.Windows.Forms.ComboBox();
			this.cboFrom = new System.Windows.Forms.ComboBox();
			this.m_txtDescription = new System.Windows.Forms.TextBox();
			label4 = new System.Windows.Forms.Label();
			label5 = new System.Windows.Forms.Label();
			this.m_grpSectionRange.SuspendLayout();
			this.SuspendLayout();
			//
			// label4
			//
			resources.ApplyResources(label4, "label4");
			label4.Name = "label4";
			//
			// label5
			//
			resources.ApplyResources(label5, "label5");
			label5.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			label5.Name = "label5";
			//
			// lblFrom
			//
			resources.ApplyResources(this.lblFrom, "lblFrom");
			this.lblFrom.Name = "lblFrom";
			//
			// lblFile
			//
			resources.ApplyResources(this.lblFile, "lblFile");
			this.lblFile.AutoEllipsis = true;
			this.lblFile.Name = "lblFile";
			//
			// label2
			//
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			//
			// label3
			//
			resources.ApplyResources(this.label3, "label3");
			this.label3.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.label3.Name = "label3";
			//
			// lblTo
			//
			resources.ApplyResources(this.lblTo, "lblTo");
			this.lblTo.Name = "lblTo";
			//
			// label8
			//
			resources.ApplyResources(this.label8, "label8");
			this.label8.Name = "label8";
			//
			// label9
			//
			resources.ApplyResources(this.label9, "label9");
			this.label9.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.label9.Name = "label9";
			//
			// label10
			//
			resources.ApplyResources(this.label10, "label10");
			this.label10.Name = "label10";
			//
			// m_btnBrowse
			//
			resources.ApplyResources(this.m_btnBrowse, "m_btnBrowse");
			this.m_btnBrowse.Name = "m_btnBrowse";
			this.m_btnBrowse.Click += new System.EventHandler(this.m_btnBrowse_Click);
			//
			// m_btnOk
			//
			resources.ApplyResources(this.m_btnOk, "m_btnOk");
			this.m_btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btnOk.Name = "m_btnOk";
			this.m_btnOk.Click += new System.EventHandler(this.m_btnOk_Click);
			//
			// m_btnCancel
			//
			resources.ApplyResources(this.m_btnCancel, "m_btnCancel");
			this.m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btnCancel.Name = "m_btnCancel";
			//
			// m_btnHelp
			//
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			this.m_btnHelp.Name = "m_btnHelp";
			this.m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// m_txtOutputFile
			//
			resources.ApplyResources(this.m_txtOutputFile, "m_txtOutputFile");
			this.m_txtOutputFile.Name = "m_txtOutputFile";
			//
			// m_rdoAllBooks
			//
			resources.ApplyResources(this.m_rdoAllBooks, "m_rdoAllBooks");
			this.m_rdoAllBooks.Checked = true;
			this.m_rdoAllBooks.Name = "m_rdoAllBooks";
			this.m_rdoAllBooks.TabStop = true;
			this.m_rdoAllBooks.UseVisualStyleBackColor = true;
			//
			// m_rdoFilteredBooks
			//
			resources.ApplyResources(this.m_rdoFilteredBooks, "m_rdoFilteredBooks");
			this.m_rdoFilteredBooks.Name = "m_rdoFilteredBooks";
			this.m_rdoFilteredBooks.TabStop = true;
			this.m_rdoFilteredBooks.UseVisualStyleBackColor = true;
			//
			// m_rdoSingleBook
			//
			resources.ApplyResources(this.m_rdoSingleBook, "m_rdoSingleBook");
			this.m_rdoSingleBook.Name = "m_rdoSingleBook";
			this.m_rdoSingleBook.TabStop = true;
			this.m_rdoSingleBook.UseVisualStyleBackColor = true;
			//
			// m_lblFilterList
			//
			resources.ApplyResources(this.m_lblFilterList, "m_lblFilterList");
			this.m_lblFilterList.AutoEllipsis = true;
			this.m_lblFilterList.Name = "m_lblFilterList";
			//
			// m_scrBook
			//
			this.m_scrBook.BackColor = System.Drawing.SystemColors.Window;
			resources.ApplyResources(this.m_scrBook, "m_scrBook");
			this.m_scrBook.Name = "m_scrBook";
			this.m_scrBook.Reference = "";
			//
			// m_grpSectionRange
			//
			resources.ApplyResources(this.m_grpSectionRange, "m_grpSectionRange");
			this.m_grpSectionRange.Controls.Add(this.cboTo);
			this.m_grpSectionRange.Controls.Add(this.cboFrom);
			this.m_grpSectionRange.Controls.Add(this.lblFrom);
			this.m_grpSectionRange.Controls.Add(this.lblTo);
			this.m_grpSectionRange.Name = "m_grpSectionRange";
			this.m_grpSectionRange.TabStop = false;
			//
			// cboTo
			//
			resources.ApplyResources(this.cboTo, "cboTo");
			this.cboTo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cboTo.FormattingEnabled = true;
			this.cboTo.Name = "cboTo";
			this.cboTo.SelectedIndexChanged += new System.EventHandler(this.cboTo_SelectedIndexChanged);
			//
			// cboFrom
			//
			resources.ApplyResources(this.cboFrom, "cboFrom");
			this.cboFrom.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cboFrom.FormattingEnabled = true;
			this.cboFrom.Name = "cboFrom";
			this.cboFrom.SelectedIndexChanged += new System.EventHandler(this.cboFrom_SelectedIndexChanged);
			//
			// m_txtDescription
			//
			resources.ApplyResources(this.m_txtDescription, "m_txtDescription");
			this.m_txtDescription.Name = "m_txtDescription";
			//
			// ExportXmlDialog
			//
			this.AcceptButton = this.m_btnOk;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.m_btnCancel;
			this.Controls.Add(this.m_txtDescription);
			this.Controls.Add(this.label10);
			this.Controls.Add(this.label8);
			this.Controls.Add(this.m_grpSectionRange);
			this.Controls.Add(this.m_scrBook);
			this.Controls.Add(this.m_lblFilterList);
			this.Controls.Add(this.m_rdoSingleBook);
			this.Controls.Add(this.m_rdoFilteredBooks);
			this.Controls.Add(this.m_rdoAllBooks);
			this.Controls.Add(label4);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.m_btnHelp);
			this.Controls.Add(this.m_btnCancel);
			this.Controls.Add(this.m_btnOk);
			this.Controls.Add(this.m_btnBrowse);
			this.Controls.Add(this.m_txtOutputFile);
			this.Controls.Add(this.lblFile);
			this.Controls.Add(this.label3);
			this.Controls.Add(label5);
			this.Controls.Add(this.label9);
			this.DoubleBuffered = true;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ExportXmlDialog";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.m_grpSectionRange.ResumeLayout(false);
			this.m_grpSectionRange.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		#region Event Handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnShown(EventArgs e)
		{
			if (cboFrom.Height != m_oldComboHeight)
			{
				int dy = cboFrom.Height - m_oldComboHeight;
				m_grpSectionRange.Height += (dy * 2);
				Height += (dy * 2);
				cboTo.Top = cboFrom.Bottom + m_gap;
				lblTo.Top = cboTo.Top + ((cboTo.Height - lblTo.Height) / 2);
				lblFrom.Top = cboFrom.Top + ((cboFrom.Height - lblFrom.Height) / 2);
			}

			base.OnShown(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the browse button to locate a file to write to.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_btnBrowse_Click(object sender, System.EventArgs e)
		{
			if (m_fileDialog.ShowSaveDialog(m_txtOutputFile.Text, false, this) == DialogResult.OK)
			{
				m_txtOutputFile.Text = m_fileDialog.FileName;
				// Ensure that the end of the filename is visible, and that the beginning
				// is also visible if the whole filename fits.
				m_txtOutputFile.Select(0, 0);
				m_txtOutputFile.Select(m_txtOutputFile.Text.Length, 0);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a click on the Ok button
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_btnOk_Click(object sender, System.EventArgs e)
		{
			// Save default export folder location to registry, if it exists.
			string directoryName = MiscUtils.GetFolderName(m_txtOutputFile.Text);
			if (directoryName != string.Empty)
				m_xmlFolder.Value = directoryName;

			// If all that was typed in was a directory, then add a filename to it.
			if (directoryName == m_txtOutputFile.Text)
				m_txtOutputFile.Text = Path.Combine(directoryName,
					m_scr.Cache.ProjectId.Name + m_fileDialog.DefaultExtension);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a click on the Help button
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_btnHelp_Click(object sender, System.EventArgs e)
		{
			switch (m_eExportType)
			{
				case FileType.OXES:
					ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpExportXML");
					break;
				case FileType.XHTML:
					ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpExportXHTML");
					break;
				case FileType.ODT:
					// TODO: Write ODT help
					ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpExportODT");
					break;
				case FileType.PDF:
					// TODO: Write PDF help
					ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpExportPDF");
					break;
				default:
					ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpExportXML");
					break;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the event when the passage to export has changed.
		/// </summary>
		/// <param name="newReference">The new reference.</param>
		/// ------------------------------------------------------------------------------------
		void m_scrBook_PassageChanged(ScrReference newReference)
		{
			int nBook = newReference.Book;

			// See TE-6945 (and ScrPassageControl.ResolveReference() invoked from LostFocus())
			if (nBook > 0 && m_nBookForSections != nBook)
			{
				m_rdoSingleBook.Checked = true;
				LoadSectionsForBook(nBook);
				m_nBookForSections = nBook;
				UpdateBookSectionGroupLabel();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the SelectedIndexChanged event of the m_cboFromSection control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void cboFrom_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (cboFrom.SelectedIndex < 0 || cboTo.SelectedIndex < 0)
				return;

			SectionCboItem item = (SectionCboItem)cboFrom.SelectedItem;
			if (item.m_indexInOtherList > cboTo.SelectedIndex)
				cboTo.SelectedIndex = item.m_indexInOtherList;
			if (!m_fLoadingBookSections)
				m_rdoSingleBook.Checked = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the SelectedIndexChanged event of the m_cboToSection control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void cboTo_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (cboTo.SelectedIndex < 0 || cboFrom.SelectedIndex < 0)
				return;

			SectionCboItem item = (SectionCboItem)cboTo.SelectedItem;
			if (item.m_indexInOtherList < cboFrom.SelectedIndex)
				cboFrom.SelectedIndex = item.m_indexInOtherList;
			if (!m_fLoadingBookSections)
				m_rdoSingleBook.Checked = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ensure that the end of the filename is visible whenever the size changes, and
		/// that the beginning is visible if the whole filename fits.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"></see> that contains the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			m_txtOutputFile.Select(0, 0);
			m_txtOutputFile.Select(m_txtOutputFile.Text.Length, 0);
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the output file name
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string FileName
		{
			get
			{
				CheckDisposed();
				return m_txtOutputFile.Text;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve what to export: everything, selected books, or (part of) a single book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ExportWhat ExportWhat
		{
			get
			{
				CheckDisposed();

				if (m_rdoAllBooks.Checked)
					return ExportWhat.AllBooks;

				return (m_rdoFilteredBooks.Checked ?
					ExportWhat.FilteredBooks : ExportWhat.SingleBook);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the book number (1-based index) to export.  (This is meaningful only if
		/// this.ExportWhat == ExportWhat.SingleBook).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int BookNumber
		{
			get
			{
				CheckDisposed();
				return m_nBookForSections;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the index of the first section to export.  (This is meaningful only if
		/// this.ExportWhat == ExportWhat.SingleBook).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int FirstSection
		{
			get
			{
				CheckDisposed();
				SectionCboItem item = cboFrom.SelectedItem as SectionCboItem;
				return (item != null) ? item.m_sectionIndex : -1;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the index of the last section to export.  (This is meaningful only if
		/// this.ExportWhat == ExportWhat.SingleBook).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int LastSection
		{
			get
			{
				CheckDisposed();
				return cboTo.SelectedIndex;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the description of this export.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Description
		{
			get
			{
				CheckDisposed();
				return m_txtDescription.Text;
			}
		}
		#endregion

		#region Private methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the book section group label.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UpdateBookSectionGroupLabel()
		{
			IScrBook book = m_scr.FindBook(m_nBookForSections);
			m_grpSectionRange.Text = string.Format(m_sRangeBookFmt,
				book.Name.BestAnalysisVernacularAlternative.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Build a string containing a comma delimited list of book names, and display it as
		/// the list of books in the filter.
		/// </summary>
		/// <param name="filter"></param>
		/// ------------------------------------------------------------------------------------
		private void FillFilterListLabel(FilteredScrBooks filter)
		{
			StringBuilder sb = new StringBuilder();
			if (filter != null)
			{
				for (int i = 0; i < filter.BookCount; ++i)
				{
					IScrBook book = m_scr.FindBook(filter.BookIds[i]);
					if (i > 0)
						sb.Append(", ");
					sb.Append(book.Name.UserDefaultWritingSystem.Text);
				}
			}
			m_lblFilterList.Text = sb.ToString();
			if (sb.Length == 0)
				m_rdoFilteredBooks.Enabled = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load a list of sections into the beginning and ending combo boxes for export.
		/// TEMPORARY: If it is the first (i.e., starting) section to export, only include
		/// sections that begin with a chapter. See TE-7287 for story to revert this.
		/// </summary>
		/// <param name="nBook"></param>
		/// ------------------------------------------------------------------------------------
		private void LoadSectionsForBook(int nBook)
		{
			if (nBook == m_nBookForSections)
				return;

			m_fLoadingBookSections = true;
			using (new WaitCursor(this, true))
			{
				cboFrom.Items.Clear();
				cboTo.Items.Clear();
				cboFrom.SelectedIndex = -1;
				cboTo.SelectedIndex = -1;

				IScrBook book = m_scr.FindBook(nBook);
				Debug.Assert(book != null);
				string sRef;
				// "{0} Intro: {1}"
				string sRefIntro = DlgResources.ResourceString("kstidOxesExportIntro");
				// "{0}: {1}"
				string sRefPassage = DlgResources.ResourceString("kstidOxesExportRef");
				ITsString tssHeading = null;
				// "(No text in section heading)"
				ITsString tssEmpty = StringUtils.MakeTss(DlgResources.ResourceString("kstidOxesExportEmptyHeading"),
					book.Cache.DefaultVernWs);
				BCVRef startRef;
				BCVRef endRef;
				IScrSection sect;
				IScrSection prevSect = null;
				int iSectionFrom = 0;
				for (int iSection = 0; iSection < book.SectionsOS.Count; ++iSection, prevSect = sect)
				{
					sect = book.SectionsOS[iSection] as IScrSection;
					sect.GetDisplayRefs(out startRef, out endRef);

					if (sect.HeadingOA != null)
					{
						// Get the first non-empty heading paragraph content.
						for (int j = 0; j < sect.HeadingOA.ParagraphsOS.Count; ++j)
						{
							IStTxtPara para = sect.HeadingOA.ParagraphsOS[j] as IStTxtPara;
							if (para != null && para.Contents.Length > 0)
							{
								tssHeading = para.Contents;
								break;
							}
						}
					}
					if (tssHeading == null || tssHeading.Length == 0)
						tssHeading = tssEmpty;
					else
					{
						// Replace embedded line separator characters with spaces.
						ITsStrBldr tsbFix = tssHeading.GetBldr();
						for (int idx = tsbFix.Text.IndexOf(StringUtils.kChHardLB);
							idx >= 0;
							idx = tsbFix.Text.IndexOf(StringUtils.kChHardLB))
						{
							tsbFix.Replace(idx, idx + 1, " ", null);
						}
						tssHeading = tsbFix.GetString();
					}

					sRef = BCVRef.MakeReferenceString(startRef, endRef, m_scr.ChapterVerseSepr, m_scr.Bridge);
					sRef = string.Format(sect.IsIntro ? sRefIntro : sRefPassage, sRef, tssHeading.Text);

					// Only include intro section or sections that begin with a chapter
					// number run in the list of starting sections. This is currently
					// required because of the way the ScrSection.AdjustReferences relies
					// upon chapter numbers.
					ITsString tss = StringUtils.MakeTss(sRef, m_scr.Cache.DefaultVernWs);

					if (ValidStartingSection(sect, prevSect))
						iSectionFrom = cboFrom.Items.Add(new SectionCboItem(tss, iSection, iSection));

					cboTo.Items.Add(new SectionCboItem(tss, iSection, iSectionFrom));
				}

				cboFrom.SelectedIndex = 0;
				cboTo.SelectedIndex = book.SectionsOS.Count - 1;
			}

			m_fLoadingBookSections = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check to see if section can be used as the starting section of the export. Must either
		/// be an intro section or a section that begins with a chapter number run.
		/// </summary>
		/// <param name="sect">section to be checked</param>
		/// <param name="prevSect">section previous to current section</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private bool ValidStartingSection(IScrSection sect, IScrSection prevSect)
		{
			if (sect.IsIntro || prevSect == null || prevSect.IsIntro)
				return true;

			if (sect.ContentOA.ParagraphsOS.Count > 0)
			{
				IScrTxtPara para = (IScrTxtPara)sect.ContentOA.ParagraphsOS[0];
				ITsString tss = para.Contents;
				if (tss.RunCount > 0)
				{
					if (tss.Style(0) == ScrStyleNames.ChapterNumber)
						return true;
				}
			}

			return false;
		}

		#endregion
	}

	#region SectionCboItem
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Class to represent sections in the To/From combo boxes
	/// </summary>
	/// ------------------------------------------------------------------------------------
	internal class SectionCboItem
	{
		internal ITsString m_title;
		internal int m_sectionIndex;
		internal int m_indexInOtherList;

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:SectionCboItem"/> class.
		/// </summary>
		/// <param name="title">The description of the section (reference + section heading
		/// text).</param>
		/// <param name="sectionIndex">Index of the section in the book.</param>
		/// <param name="indexInOtherList">The index of the corresponding item in the other
		/// combo box list (used for making sure that a proper range is set, e.g. the From
		/// section is not greater than the To section).</param>
		/// --------------------------------------------------------------------------------
		public SectionCboItem(ITsString title, int sectionIndex, int indexInOtherList)
		{
			m_title = title;
			m_sectionIndex = sectionIndex;
			m_indexInOtherList = indexInOtherList;
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Returns a <see cref="T:System.String"></see> that represents the current
		/// <see cref="T:System.Object"></see>.
		/// </summary>
		/// --------------------------------------------------------------------------------
		public override string ToString()
		{
			return m_title.Text;
		}
	}

	#endregion
}
