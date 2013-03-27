// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2002, SIL International. All Rights Reserved.
// <copyright from='2002' to='2002' company='SIL International'>
//		Copyright (c) 2002, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: SFFileListBuilder.cs
// Responsibility: DavidO
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using SIL.Utils.FileDialog;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.Common.ScriptureUtils;
using Microsoft.Win32;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.TE;
using XCore;
using SIL.FieldWorks.Common.RootSites;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for SFFileListBuilder.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class SFFileListBuilder : UserControl, IFWDisposable
	{
		/// <summary>
		/// Handler for files changing.
		/// </summary>
		public delegate void FilesChangedHandler();
		/// <summary>
		/// Fires when files are added or removed.
		/// </summary>
		public event FilesChangedHandler FilesChanged;

		#region Data members
		private FdoCache m_cache;
		private IHelpTopicProvider m_helpTopicProvider;
		private IApp m_app;
		private ScrImportFileEventHandler m_fileRemovedHandler;

		/// <summary>
		/// Holds standard format project settings
		/// </summary>
		protected IScrImportSet m_ImportSettings;

		private OpenFileDialogAdapter m_OpenFileDlg;
		/// <summary>
		/// Indicates whether an item is being removed from the file list
		/// </summary>
		protected bool m_fRemovingLVItem = false;
		/// <summary>
		/// Maintains a list of file reference maps for each file in the list
		/// </summary>
		protected ListView m_currentListView;
		/// <summary>The current remove button (scripture, BT, or notes)</summary>
		protected Button m_currentRemoveButton;
		private ImportDomain m_domain = ImportDomain.Main;
		private string m_wsId = null;
		private ICmAnnotationDefn m_noteType = null;

		/// <summary>
		/// Properties button
		/// </summary>
		protected Button btnProperties;
		/// <summary>
		/// Controls contained by the SFFileListBuilder control
		/// </summary>
		protected IContainer components = null;
		/// <summary></summary>
		protected TabControl tabFileGroups;
		private TabPage tabScripture;
		private TabPage tabBackTranslation;
		private TabPage tabNotes;
		/// <summary></summary>
		protected FwOverrideComboBox cboShowNoteTypes;
		private Button btnRemoveScr;
		private Button btnAddScr;
		private FwListView scrFileList;
		private FwListView notesFileList;
		private Button btnAddBT;
		private Button btnAddNotes;
		private Button btnRemoveBT;
		private Button btnRemoveNotes;
		private FwListView btFileList;
		/// <summary></summary>
		protected Label lblBtShowWritingSystems;
		/// <summary></summary>
		protected FwOverrideComboBox cboShowBtWritingSystem;
		/// <summary></summary>
		protected FwOverrideComboBox cboShowNotesWritingSystem;
		#endregion

		#region Construction & disposal
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SFFileListBuilder()
		{
			SetStyle(ControlStyles.UserPaint, true);

			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			m_OpenFileDlg = new OpenFileDialogAdapter();
			m_OpenFileDlg.Filter = ResourceHelper.BuildFileFilter(FileFilterType.AllScriptureStandardFormat,
				FileFilterType.AllFiles);

			m_OpenFileDlg.FilterIndex = 0;
			m_OpenFileDlg.CheckFileExists = true;
			m_OpenFileDlg.RestoreDirectory = false;
			m_OpenFileDlg.ShowHelp = false;
			m_OpenFileDlg.Title = TeResourceHelper.GetResourceString("kstidOFDTitle");
			m_OpenFileDlg.Multiselect = true;

			m_OpenFileDlg.InitialDirectory =
				Environment.GetFolderPath(Environment.SpecialFolder.Personal);
			m_currentListView = scrFileList;
			m_currentRemoveButton = btnRemoveScr;

			m_fileRemovedHandler = new ScrImportFileEventHandler(HandleFileRemoval);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes this control with the specified help topic provider.
		/// </summary>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// <param name="app">The application</param>
		public void Initialize(IHelpTopicProvider helpTopicProvider, IApp app)
		{
			m_helpTopicProvider = helpTopicProvider;
			m_app = app;
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

		/// -------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// -------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");

			if (disposing && !IsDisposed)
			{
				if (components != null)
				{
					components.Dispose();
				}
				m_OpenFileDlg.Dispose();
			}
			base.Dispose(disposing);
		}
		#endregion

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SFFileListBuilder));
			System.Windows.Forms.ColumnHeader columnHeader1;
			System.Windows.Forms.ColumnHeader columnHeader2;
			System.Windows.Forms.ColumnHeader columnHeader3;
			System.Windows.Forms.Label label1;
			System.Windows.Forms.ColumnHeader columnHeader4;
			System.Windows.Forms.ColumnHeader columnHeader5;
			System.Windows.Forms.ColumnHeader columnHeader7;
			System.Windows.Forms.Label label2;
			System.Windows.Forms.ColumnHeader columnHeader8;
			System.Windows.Forms.ColumnHeader columnHeader6;
			System.Windows.Forms.ColumnHeader columnHeader9;
			System.Windows.Forms.Label label3;
			this.tabFileGroups = new System.Windows.Forms.TabControl();
			this.tabScripture = new System.Windows.Forms.TabPage();
			this.scrFileList = new SIL.FieldWorks.Common.Controls.FwListView();
			this.btnRemoveScr = new System.Windows.Forms.Button();
			this.btnAddScr = new System.Windows.Forms.Button();
			this.tabBackTranslation = new System.Windows.Forms.TabPage();
			this.btnRemoveBT = new System.Windows.Forms.Button();
			this.btnAddBT = new System.Windows.Forms.Button();
			this.btFileList = new SIL.FieldWorks.Common.Controls.FwListView();
			this.cboShowBtWritingSystem = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.lblBtShowWritingSystems = new System.Windows.Forms.Label();
			this.tabNotes = new System.Windows.Forms.TabPage();
			this.cboShowNotesWritingSystem = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.btnRemoveNotes = new System.Windows.Forms.Button();
			this.btnAddNotes = new System.Windows.Forms.Button();
			this.notesFileList = new SIL.FieldWorks.Common.Controls.FwListView();
			this.cboShowNoteTypes = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			columnHeader1 = new System.Windows.Forms.ColumnHeader();
			columnHeader2 = new System.Windows.Forms.ColumnHeader();
			columnHeader3 = new System.Windows.Forms.ColumnHeader();
			label1 = new System.Windows.Forms.Label();
			columnHeader4 = new System.Windows.Forms.ColumnHeader();
			columnHeader5 = new System.Windows.Forms.ColumnHeader();
			columnHeader7 = new System.Windows.Forms.ColumnHeader();
			label2 = new System.Windows.Forms.Label();
			columnHeader8 = new System.Windows.Forms.ColumnHeader();
			columnHeader6 = new System.Windows.Forms.ColumnHeader();
			columnHeader9 = new System.Windows.Forms.ColumnHeader();
			label3 = new System.Windows.Forms.Label();
			this.tabFileGroups.SuspendLayout();
			this.tabScripture.SuspendLayout();
			this.tabBackTranslation.SuspendLayout();
			this.tabNotes.SuspendLayout();
			this.SuspendLayout();
			//
			// tabFileGroups
			//
			this.tabFileGroups.Controls.Add(this.tabScripture);
			this.tabFileGroups.Controls.Add(this.tabBackTranslation);
			this.tabFileGroups.Controls.Add(this.tabNotes);
			resources.ApplyResources(this.tabFileGroups, "tabFileGroups");
			this.tabFileGroups.Name = "tabFileGroups";
			this.tabFileGroups.SelectedIndex = 0;
			this.tabFileGroups.SelectedIndexChanged += new System.EventHandler(this.tabFileGroups_SelectedIndexChanged);
			//
			// tabScripture
			//
			this.tabScripture.Controls.Add(this.scrFileList);
			this.tabScripture.Controls.Add(label1);
			this.tabScripture.Controls.Add(this.btnRemoveScr);
			this.tabScripture.Controls.Add(this.btnAddScr);
			resources.ApplyResources(this.tabScripture, "tabScripture");
			this.tabScripture.Name = "tabScripture";
			//
			// scrFileList
			//
			resources.ApplyResources(this.scrFileList, "scrFileList");
			this.scrFileList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
			columnHeader1,
			columnHeader2,
			columnHeader3});
			this.scrFileList.FullRowSelect = true;
			this.scrFileList.HideSelection = false;
			this.scrFileList.Name = "scrFileList";
			this.scrFileList.OwnerDraw = true;
			this.scrFileList.UseCompatibleStateImageBehavior = false;
			this.scrFileList.View = System.Windows.Forms.View.Details;
			this.scrFileList.SelectedIndexChanged += new System.EventHandler(this.fwlv_SelectedIndexChanged);
			this.scrFileList.DrawSubItem += new System.Windows.Forms.DrawListViewSubItemEventHandler(this.fwlv_DrawSubItem);
			//
			// columnHeader1
			//
			resources.ApplyResources(columnHeader1, "columnHeader1");
			//
			// columnHeader2
			//
			resources.ApplyResources(columnHeader2, "columnHeader2");
			//
			// columnHeader3
			//
			resources.ApplyResources(columnHeader3, "columnHeader3");
			//
			// label1
			//
			resources.ApplyResources(label1, "label1");
			label1.Name = "label1";
			//
			// btnRemoveScr
			//
			resources.ApplyResources(this.btnRemoveScr, "btnRemoveScr");
			this.btnRemoveScr.Name = "btnRemoveScr";
			this.btnRemoveScr.Click += new System.EventHandler(this.btnRemove_Click);
			//
			// btnAddScr
			//
			resources.ApplyResources(this.btnAddScr, "btnAddScr");
			this.btnAddScr.Name = "btnAddScr";
			this.btnAddScr.Click += new System.EventHandler(this.btnAdd_Click);
			//
			// tabBackTranslation
			//
			this.tabBackTranslation.Controls.Add(this.btnRemoveBT);
			this.tabBackTranslation.Controls.Add(this.btnAddBT);
			this.tabBackTranslation.Controls.Add(this.btFileList);
			this.tabBackTranslation.Controls.Add(this.cboShowBtWritingSystem);
			this.tabBackTranslation.Controls.Add(this.lblBtShowWritingSystems);
			this.tabBackTranslation.Controls.Add(label2);
			resources.ApplyResources(this.tabBackTranslation, "tabBackTranslation");
			this.tabBackTranslation.Name = "tabBackTranslation";
			//
			// btnRemoveBT
			//
			resources.ApplyResources(this.btnRemoveBT, "btnRemoveBT");
			this.btnRemoveBT.Name = "btnRemoveBT";
			this.btnRemoveBT.Click += new System.EventHandler(this.btnRemove_Click);
			//
			// btnAddBT
			//
			resources.ApplyResources(this.btnAddBT, "btnAddBT");
			this.btnAddBT.Name = "btnAddBT";
			this.btnAddBT.Click += new System.EventHandler(this.btnAdd_Click);
			//
			// btFileList
			//
			resources.ApplyResources(this.btFileList, "btFileList");
			this.btFileList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
			columnHeader4,
			columnHeader5,
			columnHeader7});
			this.btFileList.FullRowSelect = true;
			this.btFileList.HideSelection = false;
			this.btFileList.Name = "btFileList";
			this.btFileList.OwnerDraw = true;
			this.btFileList.UseCompatibleStateImageBehavior = false;
			this.btFileList.View = System.Windows.Forms.View.Details;
			this.btFileList.SelectedIndexChanged += new System.EventHandler(this.fwlv_SelectedIndexChanged);
			this.btFileList.DrawSubItem += new System.Windows.Forms.DrawListViewSubItemEventHandler(this.fwlv_DrawSubItem);
			//
			// columnHeader4
			//
			resources.ApplyResources(columnHeader4, "columnHeader4");
			//
			// columnHeader5
			//
			resources.ApplyResources(columnHeader5, "columnHeader5");
			//
			// columnHeader7
			//
			resources.ApplyResources(columnHeader7, "columnHeader7");
			//
			// cboShowBtWritingSystem
			//
			resources.ApplyResources(this.cboShowBtWritingSystem, "cboShowBtWritingSystem");
			this.cboShowBtWritingSystem.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cboShowBtWritingSystem.Name = "cboShowBtWritingSystem";
			this.cboShowBtWritingSystem.SelectedIndexChanged += new System.EventHandler(this.cboShowWritingSystem_SelectedIndexChanged);
			//
			// lblBtShowWritingSystems
			//
			resources.ApplyResources(this.lblBtShowWritingSystems, "lblBtShowWritingSystems");
			this.lblBtShowWritingSystems.Name = "lblBtShowWritingSystems";
			//
			// label2
			//
			resources.ApplyResources(label2, "label2");
			label2.Name = "label2";
			//
			// tabNotes
			//
			this.tabNotes.Controls.Add(this.cboShowNotesWritingSystem);
			this.tabNotes.Controls.Add(this.btnRemoveNotes);
			this.tabNotes.Controls.Add(this.btnAddNotes);
			this.tabNotes.Controls.Add(this.notesFileList);
			this.tabNotes.Controls.Add(this.cboShowNoteTypes);
			this.tabNotes.Controls.Add(label3);
			resources.ApplyResources(this.tabNotes, "tabNotes");
			this.tabNotes.Name = "tabNotes";
			//
			// cboShowNotesWritingSystem
			//
			resources.ApplyResources(this.cboShowNotesWritingSystem, "cboShowNotesWritingSystem");
			this.cboShowNotesWritingSystem.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cboShowNotesWritingSystem.Name = "cboShowNotesWritingSystem";
			this.cboShowNotesWritingSystem.SelectedIndexChanged += new System.EventHandler(this.cboShowWritingSystem_SelectedIndexChanged);
			//
			// btnRemoveNotes
			//
			resources.ApplyResources(this.btnRemoveNotes, "btnRemoveNotes");
			this.btnRemoveNotes.Name = "btnRemoveNotes";
			this.btnRemoveNotes.Click += new System.EventHandler(this.btnRemove_Click);
			//
			// btnAddNotes
			//
			resources.ApplyResources(this.btnAddNotes, "btnAddNotes");
			this.btnAddNotes.Name = "btnAddNotes";
			this.btnAddNotes.Click += new System.EventHandler(this.btnAdd_Click);
			//
			// notesFileList
			//
			resources.ApplyResources(this.notesFileList, "notesFileList");
			this.notesFileList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
			columnHeader8,
			columnHeader6,
			columnHeader9});
			this.notesFileList.FullRowSelect = true;
			this.notesFileList.HideSelection = false;
			this.notesFileList.Name = "notesFileList";
			this.notesFileList.OwnerDraw = true;
			this.notesFileList.UseCompatibleStateImageBehavior = false;
			this.notesFileList.View = System.Windows.Forms.View.Details;
			this.notesFileList.SelectedIndexChanged += new System.EventHandler(this.fwlv_SelectedIndexChanged);
			this.notesFileList.DrawSubItem += new System.Windows.Forms.DrawListViewSubItemEventHandler(this.fwlv_DrawSubItem);
			//
			// columnHeader8
			//
			resources.ApplyResources(columnHeader8, "columnHeader8");
			//
			// columnHeader6
			//
			resources.ApplyResources(columnHeader6, "columnHeader6");
			//
			// columnHeader9
			//
			resources.ApplyResources(columnHeader9, "columnHeader9");
			//
			// cboShowNoteTypes
			//
			resources.ApplyResources(this.cboShowNoteTypes, "cboShowNoteTypes");
			this.cboShowNoteTypes.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cboShowNoteTypes.Name = "cboShowNoteTypes";
			this.cboShowNoteTypes.SelectedIndexChanged += new System.EventHandler(this.cboShowNoteTypes_SelectedIndexChanged);
			//
			// label3
			//
			resources.ApplyResources(label3, "label3");
			label3.Name = "label3";
			//
			// SFFileListBuilder
			//
			this.Controls.Add(this.tabFileGroups);
			this.Name = "SFFileListBuilder";
			resources.ApplyResources(this, "$this");
			this.tabFileGroups.ResumeLayout(false);
			this.tabScripture.ResumeLayout(false);
			this.tabBackTranslation.ResumeLayout(false);
			this.tabBackTranslation.PerformLayout();
			this.tabNotes.ResumeLayout(false);
			this.tabNotes.PerformLayout();
			this.ResumeLayout(false);

		}
		#endregion

		#region Miscellaneous methods
		/// -------------------------------------------------------------------------------
		/// <summary>
		/// Add the given SF scripture files to the list and select the first one if none
		/// is selected.
		/// </summary>
		/// <param name="listView">The list view to add the files to</param>
		/// <param name="files">An IEnumerable of ScrImportFileInfo objects to be added
		/// </param>
		/// <param name="wsId">The writing system identifier to filter on (or null)</param>
		/// <param name="noteType">The note type to filter on (or 0)</param>
		/// -------------------------------------------------------------------------------
		protected void AddFilesToListView(ListView listView, IEnumerable files,
			string wsId, ICmAnnotationDefn noteType)
		{
			listView.Items.Clear();

			if (files == null)
				return;

			try
			{
				// Add all files to the list view
				foreach (ScrImportFileInfo fileInfo in files)
				{
					if (fileInfo.WsId == wsId && fileInfo.NoteType == noteType)
						AddFileToListView(listView, fileInfo, false);
				}
			}
			catch (CancelException)
			{
				// Stop adding files
			}

			// If no item has been selected yet, select the first item in the list.
			if (listView.SelectedItems.Count == 0 && listView.Items.Count > 0)
				listView.Items[0].Selected = true;

			if (FilesChanged != null)
				FilesChanged();
		}

		/// -------------------------------------------------------------------------------
		/// <summary>
		/// Add the given SF scripture file to the list and select the first one if none
		/// is selected.
		/// </summary>
		/// <param name="listView">The list view to add the files to</param>
		/// <param name="fileInfo">list of files to be added</param>
		/// <param name="warnOnError">display an error if a file is not valid and
		/// do not add it to the list.  If this is false then invalid files will
		/// be added to the list anyway.</param>
		/// -------------------------------------------------------------------------------
		protected void AddFileToListView(ListView listView, IScrImportFileInfo fileInfo,
			bool warnOnError)
		{
			Debug.Assert(listView != null);
			Debug.Assert(fileInfo != null);

			string filename = fileInfo.FileName;
					ListViewItem lvi = new ListViewItem(filename);

			StringBuilder booksInFile = new StringBuilder();

					// If the file does not exist then keep the name but do not try to read it.
					// It will be displayed in gray with <not found> for the list of books.
			if (fileInfo.IsReadable)
					{
						// Build a comma-delimited string of the 3 letter book abbreviations to go
						// into the second column in the listview.
				if (fileInfo != null)
				{
					foreach (int bookID in fileInfo.BooksInFile)
						{
							if (bookID >= 0 && bookID <= 66)
							{
								if (booksInFile.Length > 0)
								booksInFile.Append(", ");

							booksInFile.Append(ScrReference.NumberToBookCode(bookID));
						}
							}
						}

						// Add the book list as a sub item of the file name. Then add the filename
						// as the item in the listview.
				lvi.SubItems.Add(booksInFile.ToString());

//				if (listView != scrFileList)
//				{
//					lvi.SubItems.Add(GetWritingSystemNameForLocale(fileInfo.IcuLocale));
//				}

						// Add file encoding
				lvi.SubItems.Add(fileInfo == null ?
					Encoding.ASCII.EncodingName : fileInfo.FileEncoding.EncodingName);
					}
			else
					{
						// If we want to issue an error on missing files.
						if (warnOnError)
						{
							ShowBadFileMessage(DriveUtil.FileNameOnly(filename));
					return;
						}

						lvi.SubItems.Add(TeResourceHelper.GetResourceString("kstidNoBooks"));
						lvi.SubItems.Add(TeResourceHelper.GetResourceString("kstidFileMissing"));
					}

			// Store the file info in the list view item tag
			lvi.Tag = fileInfo;

					// Check to make sure the file is not already in the list
					bool found = false;
					ListViewItem item = null;
					int i;
			for (i = 0; i < listView.Items.Count; i++)
					{
				item = listView.Items[i];
						if (item.SubItems[0].Text.ToLower() == lvi.Text.ToLower())
						{
							found = true;
							break;
						}
					}

					if (!found)
			{
				// If the file is new, then find the place to insert it based on the
				// starting reference of the file
				bool inserted = false;
				for (i = 0; i < listView.Items.Count; i++)
				{
					ScrImportFileInfo listItemInfo = (ScrImportFileInfo)listView.Items[i].Tag;
					if (fileInfo.StartRef < listItemInfo.StartRef)
					{
						listView.Items.Insert(i, lvi);
						inserted = true;
						break;
					}
						}
				if (!inserted)
					listView.Items.Add(lvi);
					}
			else
			{
				if (i < listView.Items.Count)
				{
					lvi.Selected = listView.Items[i].Selected;
					listView.Items[i] = lvi;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Attempt to add a file to the project. If an error occurs, then show the message
		/// </summary>
		/// <param name="fileName"></param>
		/// <param name="info">returns a ScrImportFileInfo object representing the added
		/// file.</param>
		/// <returns>true if the file was added to the project</returns>
		/// ------------------------------------------------------------------------------------
		private bool AddFileToProject(string fileName, out IScrImportFileInfo info)
		{
			try
			{
				info = m_ImportSettings.AddFile(fileName, m_domain, m_wsId, m_noteType,
					m_fileRemovedHandler);
				return (info != null);
			}
			catch (ScriptureUtilsException e)
			{
				Form parentWindow = FindForm();
				if (parentWindow != null)
				{
					MessageBoxUtils.Show(parentWindow, e.Message,
						ScriptureUtilsException.GetResourceString("kstidImportErrorCaption"),
						MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, 0,
						m_helpTopicProvider.HelpFile, HelpNavigator.Topic, e.HelpTopic);
				}

				info = null;
				return false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether there is at least one Scripture file accesible.
		/// </summary>
		/// <returns>true if so</returns>
		/// ------------------------------------------------------------------------------------
		public bool AtLeastOneScrFileAccessible
		{
			get
			{
				CheckDisposed();

				StringCollection ignoreThisJunk;
				return m_ImportSettings.BasicSettingsExist &&
					m_ImportSettings.ImportProjectIsAccessible(out ignoreThisJunk);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure there is at least one Scripture file is accesible to import and that all
		/// the files have valid encodings
		/// </summary>
		/// <returns>true if it's okay to go forward; false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public bool Valid()
		{
			CheckDisposed();

			return AtLeastOneScrFileAccessible && FileEncodingsAreValid();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Show a message to indicate that a file that was added does not have any books.
		/// </summary>
		/// <param name="filename"></param>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification="See TODO-Linux comment")]
		protected virtual void ShowBadFileMessage(string filename)
		{
			string message = string.Format(TeResourceHelper.GetResourceString("kstidImportFileNoBooks"),
				filename);
			// TODO-Linux: Help is not implemented in Mono
			MessageBox.Show(message, ResourceHelper.GetResourceString("kstidImportFileNoBookCaption"),
				MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, 0, m_helpTopicProvider.HelpFile,
				HelpNavigator.Topic, ResourceHelper.GetHelpString("kstidImportFileNoBookTopic"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save settings (column widths)
		/// </summary>
		/// <param name="key">Base registry key</param>
		/// ------------------------------------------------------------------------------------
		public void SaveSettings(RegistryKey key)
		{
			CheckDisposed();

			for (int i = 0; i < ScrListView.Columns.Count; i++)
				key.SetValue("ScrFileListCol" + i, ScrListView.Columns[i].Width);

			for (int i = 0; i < BtListView.Columns.Count; i++)
				key.SetValue("BtFileListCol" + i, BtListView.Columns[i].Width);

			for (int i = 0; i < NotesListView.Columns.Count; i++)
				key.SetValue("NotesFileListCol" + i, NotesListView.Columns[i].Width);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the column widths for each file list
		/// </summary>
		/// <param name="key">Base registry key</param>
		/// ------------------------------------------------------------------------------------
		public void LoadSettings(RegistryKey key)
		{
			CheckDisposed();

			for (int i = 0; i < ScrListView.Columns.Count; i++)
			{
				ScrListView.Columns[i].Width =
					(int)key.GetValue("ScrFileListCol" + i, ScrListView.Columns[i].Width);
			}

			for (int i = 0; i < BtListView.Columns.Count; i++)
			{
				BtListView.Columns[i].Width =
					(int)key.GetValue("BtFileListCol" + i, BtListView.Columns[i].Width);
			}

			for (int i = 0; i < NotesListView.Columns.Count; i++)
			{
				NotesListView.Columns[i].Width =
					(int)key.GetValue("NotesFileListCol" + i, NotesListView.Columns[i].Width);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check to make sure all of the file encodings are valid for importing.
		/// </summary>
		/// <returns><c>true</c> if all files have valid encodings<c>false</c> otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public bool FileEncodingsAreValid()
		{
			CheckDisposed();

			if (ScrListView.Items.Count == 0)
				return false;

			// Verify that the files all have supported encodings
			List<Encoding> supportedEncodings = new List<Encoding>(4);
			supportedEncodings.Add(Encoding.ASCII);
			supportedEncodings.Add(Encoding.UTF8);
			supportedEncodings.Add(Encoding.BigEndianUnicode);
			supportedEncodings.Add(Encoding.Unicode);

			foreach (ListView lv in new ListView[] {ScrListView, BtListView, NotesListView})
			{
				foreach(ListViewItem lvi in lv.Items)
				{
					ScrImportFileInfo fileInfo = (ScrImportFileInfo)lvi.Tag;

					if (fileInfo.IsReadable && !supportedEncodings.Contains(fileInfo.FileEncoding))
					{
						string message = string.Format(TeResourceHelper.GetResourceString("kstidUnsupportedEncoding"),
							fileInfo.FileName, fileInfo.FileEncoding.EncodingName);

						DisplayMessageBox(message);
						return false;
					}
				}
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Virtual to allow overriding for tests
		/// </summary>
		/// <param name="message">Message to display</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void DisplayMessageBox(string message)
			{
			MessageBox.Show(this, message, m_app.ApplicationName, MessageBoxButtons.OK,
				MessageBoxIcon.Information);
		}
		#endregion

		#region Public Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		private FdoCache Cache
		{
			set
			{
				m_cache = value;

				cboShowBtWritingSystem.Items.Clear();
				cboShowNotesWritingSystem.Items.Clear();
				foreach (IWritingSystem ws in m_cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems)
				{
					cboShowBtWritingSystem.Items.Add(ws);
					cboShowNotesWritingSystem.Items.Add(ws);
				}
				cboShowBtWritingSystem.SelectedIndex = 0;
				cboShowNotesWritingSystem.SelectedIndex = 0;

				// Add all of the annotation types to the combo box
				cboShowNoteTypes.Items.Clear();
				foreach (ICmAnnotationDefn noteType in m_cache.LanguageProject.ScriptureAnnotationDfns)
				{
					if (noteType.UserCanCreate)
						cboShowNoteTypes.Items.Add(new DisplayAnnotationDefn(noteType));
				}
				cboShowNoteTypes.SelectedIndex = 0;
				tabFileGroups_SelectedIndexChanged(null, null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the ScrImportSet object that contains the initial list of
		/// files (if any) and to which files can be added and from which they can be removed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public IScrImportSet ImportSettings
		{
			get
			{
				CheckDisposed();
				return m_ImportSettings;
			}
			set
			{
				CheckDisposed();

				Debug.Assert(value != null);
				m_ImportSettings = value;
				Cache = m_ImportSettings.Cache;
				PopulateFileListsFromSettings();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load all the file lists with the files in the Import settings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void PopulateFileListsFromSettings()
		{
			// Populate all three file lists from ScrImportSet
			AddFilesToListView(scrFileList, m_ImportSettings.GetImportFiles(ImportDomain.Main), null, null);
			AddFilesToListView(btFileList, m_ImportSettings.GetImportFiles(ImportDomain.BackTrans),
				((IWritingSystem)cboShowBtWritingSystem.SelectedItem).Id, null);
			AddFilesToListView(notesFileList, m_ImportSettings.GetImportFiles(ImportDomain.Annotations),
				((IWritingSystem)cboShowBtWritingSystem.SelectedItem).Id,
				((DisplayAnnotationDefn)cboShowNoteTypes.SelectedItem).Definition);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an array of all files in the list of "Scripture" files. Returns an empty list
		/// if no files have been added.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public List<string> ScriptureFiles
		{
			get
			{
				CheckDisposed();

				List<string> addedFiles = new List<string>(scrFileList.Items.Count);
				foreach (ListViewItem lvi in scrFileList.Items)
					addedFiles.Add(lvi.Text);
				return addedFiles;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an array of all files in the currently selected list list of "Back Translation"
		/// files. Returns an empty list if no files have been added.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public List<string> BackTransFiles
		{
			get
			{
				CheckDisposed();

				List<string> addedFiles = new List<string>(btFileList.Items.Count);
				foreach (ListViewItem lvi in btFileList.Items)
					addedFiles.Add(lvi.Text);
				return addedFiles;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an array of all files in the currently selected list of "Notes" files. Returns
		/// an empty list if no files have been added.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public List<string> NotesFiles
		{
			get
			{
				CheckDisposed();

				List<string> addedFiles = new List<string>(notesFileList.Items.Count);
				foreach (ListViewItem lvi in notesFileList.Items)
					addedFiles.Add(lvi.Text);
				return addedFiles;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an array of "scripture" files in the list that are accessible. Returns an empty
		/// list if no files have been added or none are accessible.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public List<string> AccessibleFiles
		{
			get
			{
				CheckDisposed();

				List<string> addedFiles = new List<string>(scrFileList.Items.Count);
				foreach (ListViewItem lvi in scrFileList.Items)
				{
					ScrImportFileInfo info = (ScrImportFileInfo)lvi.Tag;
					if (info.IsReadable)
						addedFiles.Add(lvi.Text);
				}
				return addedFiles;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the latest import folder.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string LatestImportFolder
		{
			get
			{
				CheckDisposed();

				return m_OpenFileDlg.InitialDirectory;
			}
			set
			{
				CheckDisposed();

				if (value != null && value != string.Empty)
					m_OpenFileDlg.InitialDirectory = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the underlying listview for scripture files.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public FwListView ScrListView
		{
			get
			{
				CheckDisposed();
				return scrFileList;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the underlying listview for back translation files.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ListView BtListView
		{
			get
			{
				CheckDisposed();
				return btFileList;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the underlying listview for notes files.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ListView NotesListView
		{
			get
			{
				CheckDisposed();
				return notesFileList;
			}
		}
		#endregion

		#region Protected properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of the currently selected file, or -1 if none is selected.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private int SelectedItem
		{
			get
		{
				return (m_currentListView.SelectedItems.Count > 0) ?
					m_currentListView.SelectedItems[0].Index : -1;
			}
		}
		#endregion

		#region Event Handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verify that notification was issued for the expected file removal
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e">Argument that tells which file was removed</param>
		/// ------------------------------------------------------------------------------------
		protected void HandleFileRemoval(object sender, ScrImportFileEventArgs e)
		{
			ListViewItem itemToRemove = null;

			foreach (ListViewItem lvi in m_currentListView.Items)
			{
				if (lvi.Tag == e.FileInfo)
				{
					itemToRemove = lvi;
					break;
				}
			}
			Debug.Assert(itemToRemove != null);
			m_currentListView.Items.Remove(itemToRemove);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remember which thing we're on.
		/// </summary>
		/// <param name="sender">Not used</param>
		/// <param name="e">Not used</param>
		/// ------------------------------------------------------------------------------------
		protected void tabFileGroups_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			switch (tabFileGroups.SelectedIndex)
			{
				case 0:	// main
					m_currentListView = scrFileList;
					m_currentRemoveButton = btnRemoveScr;
					m_domain = ImportDomain.Main;
					m_wsId = null;
					m_noteType = null;
					break;

				case 1:	// BT
					m_currentListView = btFileList;
					m_currentRemoveButton = btnRemoveBT;
					m_domain = ImportDomain.BackTrans;
					m_wsId = ((IWritingSystem)cboShowBtWritingSystem.SelectedItem).Id;
					m_noteType = null;
					break;

				case 2:	//Annotations
					m_currentListView = notesFileList;
					m_currentRemoveButton = btnRemoveNotes;
					m_domain = ImportDomain.Annotations;
					m_wsId = ((IWritingSystem)cboShowNotesWritingSystem.SelectedItem).Id;
					m_noteType = ((DisplayAnnotationDefn)cboShowNoteTypes.SelectedItem).Definition;
					break;
			}
			m_currentRemoveButton.Enabled =
				(m_currentListView.Items.Count > 0 && m_currentListView.SelectedItems.Count > 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Refresh the file list based on the selected writing system
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected void cboShowWritingSystem_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			ComboBox combo = (ComboBox)sender;
			m_wsId = ((IWritingSystem)combo.SelectedItem).Id;
			if (m_ImportSettings != null)
			{
				m_currentListView.Items.Clear();
				AddFilesToListView(m_currentListView, m_ImportSettings.GetImportFiles(m_domain),
					m_wsId, m_noteType);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Refresh the file list based on the selected note type
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected void cboShowNoteTypes_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			m_noteType = ((DisplayAnnotationDefn)cboShowNoteTypes.SelectedItem).Definition;
			if (m_ImportSettings != null)
			{
				m_currentListView.Items.Clear();
				AddFilesToListView(m_currentListView, m_ImportSettings.GetImportFiles(m_domain),
					m_wsId, m_noteType);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the DrawSubItem event of the file lists.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.DrawListViewSubItemEventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void fwlv_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
		{
			// If we don't have any text to draw, there's no use in trying.
			if (e.SubItem.Text == null)
				return;

			FwListView lv = (FwListView)sender;

			// Draw the string without wrapping and so the ellipsis indicates when the
			// text won't fit in the column. Also draw it left aligned (or right with
			// a RTL interface) and vertically centered.
			using (StringFormat sf = new StringFormat(StringFormatFlags.NoWrap))
			{
				sf.Trimming = e.ColumnIndex > 0 ? StringTrimming.EllipsisCharacter : StringTrimming.EllipsisPath;
				sf.Alignment = StringAlignment.Near;
				sf.LineAlignment = StringAlignment.Center;

				Color foreColor = lv.GetTextColor(e);

				e.Graphics.DrawString(e.SubItem.Text, lv.Font, new SolidBrush(foreColor), e.Bounds, sf);
			}
		}

		/// -------------------------------------------------------------------------------
		/// <summary>
		/// Handle the addition of files.
		/// </summary>
		/// -------------------------------------------------------------------------------
		protected void btnAdd_Click(object sender, System.EventArgs e)
		{
			bool fileAdded = false;
			try
			{
				string[] filesToAdd = QueryUserForNewFilenames();
				if (filesToAdd != null)
				{
					AddFilesToProjectAndListView(filesToAdd);
					fileAdded = true;
				}
			}
			catch (CancelException)
			{
				// Stop adding files
			}

			if (fileAdded && FilesChanged != null)
				FilesChanged();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds files (select by the user) to the import settings and the current list view.
		/// </summary>
		/// <param name="filesToAdd"></param>
		/// ------------------------------------------------------------------------------------
		private void AddFilesToProjectAndListView(string[] filesToAdd)
		{
			IScrImportFileInfo fileInfo;
			foreach (string fileName in filesToAdd)
			{
				if (AddFileToProject(fileName, out fileInfo))
					AddFileToListView(m_currentListView, fileInfo, true);
			}
		}

		/// -------------------------------------------------------------------------------
		/// <summary>
		/// Show the open file dialog box to allow the user to choose the SF scripture files
		/// to import.
		/// </summary>
		/// <returns>Array of files to add</returns>
		/// <remarks>This is broken out as a separate method from
		/// <see cref="btnAdd_Click"/> to facilitate unit testing.</remarks>
		/// -------------------------------------------------------------------------------
		protected virtual string[] QueryUserForNewFilenames()
		{
			m_OpenFileDlg.FileName = string.Empty;

			if (m_OpenFileDlg.ShowDialog() == DialogResult.Cancel)
				return null;

			m_OpenFileDlg.InitialDirectory =
				DriveUtil.DirectoryNameOnly(m_OpenFileDlg.FileNames[0]);

			return m_OpenFileDlg.FileNames;
		}

		/// -------------------------------------------------------------------------------
		/// <summary>
		/// Processes clicking on the Remove button.
		/// </summary>
		/// -------------------------------------------------------------------------------
		protected void btnRemove_Click(object sender, System.EventArgs e)
		{
			// There is a weird situation this is designed to supress, except when
			// removing the only remaining item in the list. See the comments
			// in fwlv_SelectedIndexChanged.
			if (m_currentListView.Items.Count > 1)
				m_fRemovingLVItem = true;
			int selectedRow = SelectedItem;

			foreach (ListViewItem item in m_currentListView.SelectedItems)
			{
				m_ImportSettings.RemoveFile(item.Text, m_domain, m_wsId, m_noteType);
				item.Remove();
			}

			m_fRemovingLVItem = false;

			// When we've just removed the last item in the list, then it's invalid to
			// select the new item at 'row' since it's gone. Therefore, set row to
			// point to the new last item in the list.
			if (selectedRow >= m_currentListView.Items.Count)
				selectedRow--;

			// If there are still items in the list, select one.
			if (m_currentListView.Items.Count > 0)
				m_currentListView.Items[selectedRow].Selected = true;

			if (FilesChanged != null)
				FilesChanged();
		}

		/// -------------------------------------------------------------------------------
		/// <summary>
		/// Monitoring that the selected index changes will make sure the remove and
		/// properties buttons are not enabled when there isn't a selected item.
		/// </summary>
		/// -------------------------------------------------------------------------------
		public void fwlv_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			// When m_fRemovingLVItem is true, it means the user just clicked on the Remove
			// button and we're here as a result of the selected index going to -1 because
			// the selected item was just removed. In that case, do nothing here since a
			// new item will be selected shortly. When that happens, we'll be right back
			// here to set these button's enabled states again. Disabling a button, even
			// for a split second, will cause it to lose focus, so we don't want to
			// disable the buttons unnecessarily.
			if (m_fRemovingLVItem)
				return;

			m_currentRemoveButton.Enabled =
				(m_currentListView.Items.Count > 0 && m_currentListView.SelectedItems.Count > 0);
		}
		#endregion
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Derived class to display annotation definitions
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DisplayAnnotationDefn
	{
		private string m_displayName;
		private ICmAnnotationDefn m_defn;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:DisplayAnnotationDefn"/> class.
		/// </summary>
		/// <param name="source">source CmAnnotationDefn object to construct from</param>
		/// ------------------------------------------------------------------------------------
		public DisplayAnnotationDefn(ICmAnnotationDefn source)
		{
			m_displayName = source.Name.UserDefaultWritingSystem.Text;
			m_defn = source;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Provides a string representation of the annotation definition
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return m_displayName;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Annotation Definition type.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public ICmAnnotationDefn Definition
		{
			get { return m_defn; }
		}
	}
}
