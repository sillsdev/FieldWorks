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
// File: ArchiveMaintenanceDialog.cs
// Responsibility: TE team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Windows.Forms;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using System.Diagnostics;
using SIL.FieldWorks.FDO.DomainServices;
using XCore;

namespace SIL.FieldWorks.TE
{
	#region SavedVersionsDialog class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Dialog for displaying and managing saved versions.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class SavedVersionsDialog : Form, IBookVersionAgent, IFWDisposable
	{
		#region Data members
		/// <summary>the tree view</summary>
		protected System.Windows.Forms.TreeView m_treeArchives;
		/// <summary></summary>
		protected System.Windows.Forms.Button m_btnDelete;
		/// <summary></summary>
		/// <remarks>protected because of testing</remarks>
		protected Button m_btnDiff;
		private Button btnSave;
		private Button m_btnCopyToCurr;
		/// Required designer variable.
		private System.ComponentModel.Container components = null;

		/// <summary></summary>
		protected readonly FdoCache m_cache;
		private readonly IHelpTopicProvider m_helpTopicProvider;
		private readonly FwApp m_app;
		private readonly IVwStylesheet m_styleSheet;
		private readonly float m_zoomDraft;
		private readonly float m_zoomFootnote;
		private readonly IScripture m_scr;
		#endregion

		#region Constructor and Destructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:SavedVersionsDialog"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="styleSheet">The style sheet.</param>
		/// <param name="zoomFactorDraft">The zoom percentage to be used for the "draft" (i.e.,
		/// main Scripture) view</param>
		/// <param name="zoomFactorFootnote">The zoom percentage to be used for the "footnote"
		/// view</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// <param name="app">The app.</param>
		/// ------------------------------------------------------------------------------------
		public SavedVersionsDialog(FdoCache cache, IVwStylesheet styleSheet,
			float zoomFactorDraft, float zoomFactorFootnote, IHelpTopicProvider helpTopicProvider,
			FwApp app)
		{
			InitializeComponent();

			m_cache = cache;
			m_helpTopicProvider = helpTopicProvider;
			m_app = app;
			m_styleSheet = styleSheet;
			m_zoomDraft = zoomFactorDraft;
			m_zoomFootnote = zoomFactorFootnote;
			m_scr = cache.LangProject.TranslatedScriptureOA;

			// Create a list of all of the nodes to add.  The list will be sorted by date
			// before adding them to the tree.
			List<TreeNode> list = new List<TreeNode>();

			// Add all of the saved version as root nodes in the tree
			foreach (IScrDraft savedVersion in m_scr.ArchivedDraftsOC)
			{
				TreeNode versionNode = new TreeNode(GetSavedVersionLabel(savedVersion));
				versionNode.Tag = savedVersion;
				list.Add(versionNode);

				// Add all of the books in the saved version as child nodes.
				foreach (IScrBook book in savedVersion.BooksOS)
				{
					TreeNode bookNode = new TreeNode(((IScrBook)book).BestUIName + " " +
						ImportedBooks.GetBookInfo(book));
					bookNode.Tag = book;
					versionNode.Nodes.Add(bookNode);
				}
			}

			// Sort the list of root nodes by date in decending order.
			list.Sort(new ArchiveDateComparer(false));

			// Add all of the nodes in the list to the tree view.
			foreach (TreeNode node in list)
				m_treeArchives.Nodes.Add(node);

			// If the list is empty then disable the action buttons.
			if (list.Count == 0)
			{
				m_btnDiff.Enabled = false;
				m_btnCopyToCurr.Enabled = false;
				m_btnDelete.Enabled = false;
			}
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
		protected override void Dispose( bool disposing )
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
			System.Windows.Forms.Button m_btnClose;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SavedVersionsDialog));
			System.Windows.Forms.Button m_btnHelp;
			System.Windows.Forms.Label label1;
			System.Windows.Forms.Label label2;
			System.Windows.Forms.Label label3;
			System.Windows.Forms.Label label4;
			this.m_treeArchives = new System.Windows.Forms.TreeView();
			this.m_btnDelete = new System.Windows.Forms.Button();
			this.m_btnDiff = new System.Windows.Forms.Button();
			this.btnSave = new System.Windows.Forms.Button();
			this.m_btnCopyToCurr = new System.Windows.Forms.Button();
			m_btnClose = new System.Windows.Forms.Button();
			m_btnHelp = new System.Windows.Forms.Button();
			label1 = new System.Windows.Forms.Label();
			label2 = new System.Windows.Forms.Label();
			label3 = new System.Windows.Forms.Label();
			label4 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			//
			// m_btnClose
			//
			resources.ApplyResources(m_btnClose, "m_btnClose");
			m_btnClose.DialogResult = System.Windows.Forms.DialogResult.OK;
			m_btnClose.Name = "m_btnClose";
			m_btnClose.Click += new System.EventHandler(this.m_btnClose_Click);
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
			// label2
			//
			resources.ApplyResources(label2, "label2");
			label2.Name = "label2";
			//
			// label3
			//
			resources.ApplyResources(label3, "label3");
			label3.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			label3.Name = "label3";
			//
			// label4
			//
			resources.ApplyResources(label4, "label4");
			label4.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			label4.Name = "label4";
			//
			// m_treeArchives
			//
			resources.ApplyResources(this.m_treeArchives, "m_treeArchives");
			this.m_treeArchives.HideSelection = false;
			this.m_treeArchives.ItemHeight = 16;
			this.m_treeArchives.Name = "m_treeArchives";
			this.m_treeArchives.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.m_treeArchives_AfterSelect);
			this.m_treeArchives.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.m_treeArchives_NodeMouseClick);
			this.m_treeArchives.KeyDown += new System.Windows.Forms.KeyEventHandler(this.m_treeArchives_KeyDown);
			//
			// m_btnDelete
			//
			resources.ApplyResources(this.m_btnDelete, "m_btnDelete");
			this.m_btnDelete.Name = "m_btnDelete";
			this.m_btnDelete.Click += new System.EventHandler(this.m_btnDelete_Click);
			//
			// m_btnDiff
			//
			resources.ApplyResources(this.m_btnDiff, "m_btnDiff");
			this.m_btnDiff.Name = "m_btnDiff";
			this.m_btnDiff.Click += new System.EventHandler(this.m_btnDiff_Click);
			//
			// btnSave
			//
			resources.ApplyResources(this.btnSave, "btnSave");
			this.btnSave.Name = "btnSave";
			this.btnSave.UseVisualStyleBackColor = true;
			this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
			//
			// m_btnCopyToCurr
			//
			resources.ApplyResources(this.m_btnCopyToCurr, "m_btnCopyToCurr");
			this.m_btnCopyToCurr.Name = "m_btnCopyToCurr";
			this.m_btnCopyToCurr.Click += new System.EventHandler(this.m_btnCopyToCurr_Click);
			//
			// SavedVersionsDialog
			//
			this.AcceptButton = m_btnClose;
			resources.ApplyResources(this, "$this");
			this.CancelButton = m_btnClose;
			this.Controls.Add(this.m_btnCopyToCurr);
			this.Controls.Add(label2);
			this.Controls.Add(label4);
			this.Controls.Add(label3);
			this.Controls.Add(this.btnSave);
			this.Controls.Add(label1);
			this.Controls.Add(m_btnHelp);
			this.Controls.Add(m_btnClose);
			this.Controls.Add(this.m_btnDiff);
			this.Controls.Add(this.m_btnDelete);
			this.Controls.Add(this.m_treeArchives);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "SavedVersionsDialog";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		#region Event Handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the btnSave control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void btnSave_Click(object sender, EventArgs e)
		{
			IScrDraft savedVersion;
			using (SaveVersionDialog dlg = new SaveVersionDialog(m_cache, m_helpTopicProvider))
			{
				dlg.ShowDialog();
				savedVersion = dlg.SavedVersion;
			}

			if (savedVersion == null)
				return;

			// Add the archive as a new root node in the tree
			TreeNode versionNode = new TreeNode(GetSavedVersionLabel(savedVersion));
			versionNode.Tag = savedVersion;
			// Add all of the books in the archive as child nodes.
			foreach (IScrBook book in savedVersion.BooksOS)
			{
				TreeNode bookNode = new TreeNode(((IScrBook)book).BestUIName + " " +
						ImportedBooks.GetBookInfo(book));
				bookNode.Tag = book;
				versionNode.Nodes.Add(bookNode);
			}
			m_treeArchives.Nodes.Insert(0, versionNode);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the Close button
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_btnClose_Click(object sender, System.EventArgs e)
		{
			Close();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the Delete button.  Either delete an archive or a book within the archive
		/// depending on the selected item.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void m_btnDelete_Click(object sender, System.EventArgs e)
		{
			using (new WaitCursor(this))
			{
				// Figure out what is selected (book or archive)
				TreeNode node = m_treeArchives.SelectedNode;
				if (node == null)
					return;

				// If the tree node is a book and it is the only book in the archive then
				// switch the node to the parent (archive) node to delete it instead.
				if (node.Parent != null && node.Parent.Nodes.Count == 1)
					node = node.Parent;

				string stUndo;
				string stRedo;
				// If the tree node is a book, then delete the book from the archive.
				if (node.Tag is IScrBook)
				{
					IScrBook book = (IScrBook)node.Tag;
					TreeNode parentNode = node.Parent;
					IScrDraft archive = (IScrDraft)parentNode.Tag;
					if (archive.Protected)
					{
						MessageBox.Show(this, TeResourceHelper.GetResourceString("kstidCannotDeleteBookFromProtectedVersion"),
										TeResourceHelper.GetResourceString("kstidProtectedVersionCaption"), MessageBoxButtons.OK,
										MessageBoxIcon.Information);
						return;
					}

					// Delete the book from the saved version
					TeResourceHelper.MakeUndoRedoLabels("kstidUndoDeleteArchiveBook", out stUndo,
						out stRedo);
					using (UndoTaskHelper undoHelper = new UndoTaskHelper(m_cache.ActionHandlerAccessor,
						null, stUndo, stRedo))
					{
						archive.BooksOS.Remove(book);
						undoHelper.RollBack = false;
					}

					// Delete the node from the tree
					parentNode.Nodes.Remove(node);
				}

				// If the tree node is an archive then delete the entire archive
				else if (node.Tag is IScrDraft)
				{
					IScrDraft archive = (IScrDraft)node.Tag;
					if (archive.Protected)
					{
						MessageBox.Show(this, TeResourceHelper.GetResourceString("kstidCannotDeleteProtectedVersion"),
										TeResourceHelper.GetResourceString("kstidProtectedVersionCaption"), MessageBoxButtons.OK,
										MessageBoxIcon.Information);
						return;
					}

					// Delete the ScrDraft object from the scripture
					TeResourceHelper.MakeUndoRedoLabels("kstidUndoDeleteArchive", out stUndo,
						out stRedo);
					using (UndoTaskHelper undoHelper = new UndoTaskHelper(m_cache.ActionHandlerAccessor,
						null, stUndo, stRedo))
					{
						m_scr.ArchivedDraftsOC.Remove(archive);
						undoHelper.RollBack = false;
					}

					// Delete the archive node from the tree
					m_treeArchives.Nodes.Remove(node);
				}

				// If the last thing was deleted from the tree, disable the action buttons.
				if (m_treeArchives.Nodes.Count == 0)
				{
					m_btnDelete.Enabled = false;
					m_btnCopyToCurr.Enabled = false;
					m_btnDiff.Enabled = false;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the View Differences button. Display the Review Differences window to
		/// view/merge differences between the current scripture and selected book in the tree.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_btnDiff_Click(object sender, System.EventArgs e)
		{
			// If the selected item is not a book, then don't do a diff.
			TreeNode node = m_treeArchives.SelectedNode;
			if (node == null)
				return;

			IScrBook bookRev = (IScrBook)node.Tag;
			if (bookRev == null)
				return;

			using (BookMerger merger = new BookMerger(m_cache, m_styleSheet, bookRev))
			{
			using (ProgressDialogWithTask progress = new ProgressDialogWithTask(this))
			{
				progress.Title = DlgResources.ResourceString("kstidCompareCaption");
				progress.Message = string.Format(
					DlgResources.ResourceString("kstidMergeProgress"), bookRev.BestUIName);
				progress.RunTask(true, merger.DetectDifferences);
			}

			int cUnfilteredDifferences = merger.NumberOfDifferences;

			// always hide diffs that could cause deletion of current sections, if reverted
			merger.UseFilteredDiffList = true;

			bool fShowCompareAndMergeDlg = (merger.NumberOfDifferences != 0);
			if (!fShowCompareAndMergeDlg && cUnfilteredDifferences > 0)
			{
				// Tell users that no differences were found in the merge
				if (MessageBox.Show(this,
					string.Format(DlgResources.ResourceString("kstidOnlyAdditionsDetected"), bookRev.BestUIName),
					m_app.ApplicationName, MessageBoxButtons.YesNo) == DialogResult.Yes)
				{
					fShowCompareAndMergeDlg = true;
					merger.UseFilteredDiffList = false;
				}
			}

			// If there were differences detected then show the diff dialog
			if (fShowCompareAndMergeDlg)
			{
				using (DiffDialog dlg = new DiffDialog(merger, m_cache, m_styleSheet, m_zoomDraft,
					m_zoomFootnote, m_app, m_helpTopicProvider))
				{
					// We have to pass the owner (this), so that the dialog shows when the
					// user clicks on the TE icon in the taskbar. Otherwise only the Archive
					// dialog would pop up and beeps; diff dialog could only be regained by Alt-Tab.
					dlg.ShowDialog(this);
				}
			}
			else if (cUnfilteredDifferences == 0)
			{
				// Tell users that no differences were found in the merge
					MessageBoxUtils.Show(this,
					string.Format(DlgResources.ResourceString("kstidNoDifferencesDetected"), bookRev.BestUIName),
					m_app.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the help button
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_btnHelp_Click(object sender, System.EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpSavedVersionsDialog");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle key presses in the tree view.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_treeArchives_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			switch (e.KeyCode)
			{
				// DEL to delete an item
				case Keys.Delete:
					m_btnDelete_Click(sender, null);
					break;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a selection change in the tree view.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected void m_treeArchives_AfterSelect(object sender, System.Windows.Forms.TreeViewEventArgs e)
		{
			bool enableCompare = false;

			// Enable the Delete, Diff and Copy to Current buttons based on whether the selection is on
			// a book node and whether the corresponding book is present in the current version.
			TreeNode node = m_treeArchives.SelectedNode;
			m_btnDelete.Enabled = (node != null);
			if (node != null && node.Tag is IScrBook)
			{
				m_btnDelete.Enabled = m_btnCopyToCurr.Enabled = true;
				int bookId = ((IScrBook)node.Tag).CanonicalNum;
				IScrBook sameBookInDB = m_scr.FindBook(bookId);
				enableCompare = (sameBookInDB != null);
			}
			else
				m_btnCopyToCurr.Enabled = false;

			m_btnDiff.Enabled = enableCompare;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the m_btnCopyToCurr control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_btnCopyToCurr_Click(object sender, EventArgs e)
		{
			// If nothing or a complete archive is selected, do nothing.
			TreeNode node = m_treeArchives.SelectedNode;
			if (node == null || !(node.Tag is IScrBook))
				return;

			IScrBook savedBook = (IScrBook)node.Tag;
			IScrBook originalBook = (IScrBook)m_scr.FindBook(savedBook.CanonicalNum);
			TreeNode parentNode = node.Parent;
			IScrDraft archive = (IScrDraft)parentNode.Tag;
			OverwriteType typeOfOverwrite = OverwriteType.FullNoDataLoss;
			List<IScrSection> sectionsToRemove = null;
			if (originalBook != null)
			{
				string sDetails;
				HashSet<int> missingBtWs;
				typeOfOverwrite = originalBook.DetermineOverwritability(savedBook, out sDetails,
					out sectionsToRemove, out missingBtWs);
				if (typeOfOverwrite == OverwriteType.DataLoss)
				{
					// There will be data loss if the user overwrites so we don't allow them
					// to continue.
					ImportedBooks.ReportDataLoss(originalBook, ScrDraftType.SavedVersion, this, sDetails);
					return;
				}

				if (missingBtWs != null && !ImportedBooks.ConfirmBtOverwrite(originalBook,
					ScrDraftType.SavedVersion, sectionsToRemove, missingBtWs, this))
				{
					// The user might lose back translation(s) if they proceed and they decided
					// against it.
					return;
				}
			}

			string stUndo;
			string stRedo;
			TeResourceHelper.MakeUndoRedoLabels("kstidOverwriteCurrentWithSaved", out stUndo, out stRedo);
			using (new WaitCursor(this))
			using (UndoTaskHelper undoHelper = new UndoTaskHelper(m_cache.ActionHandlerAccessor,
				null, stUndo, stRedo))
			{
				if (typeOfOverwrite == OverwriteType.Partial)
				{
					// Perform an automerge of the original book and the saved version.
					using (BookMerger merger = new BookMerger(m_cache, m_styleSheet, savedBook))
					{
						using (ProgressDialogWithTask progress = new ProgressDialogWithTask(this))
						{
							progress.Title = DlgResources.ResourceString("kstidOverwriteCaption");
							progress.RunTask(true, merger.DoPartialOverwrite, sectionsToRemove);
						}
						if (!merger.AutoMerged)
							throw new ContinuableErrorException("Partial Overwrite was not successful.");
					}
				}
				else
				{

					// FWNX-677 - caused by mono-calgary patch bug-614850_Modal_614850_v6.patch
					List<Form> disabled_forms = new List<Form>();
					if (MiscUtils.IsUnix)
					{
						lock (Application.OpenForms)
						{
							foreach (Form form in Application.OpenForms)
								if (form.Enabled == false)
									disabled_forms.Add(form);
						}
						foreach (Form form in disabled_forms)
									form.Enabled = true;
					}

					if (originalBook != null)
					{
						if (m_cache.ActionHandlerAccessor != null)
						{
							// When Undoing, we need to first resurrect the deleted book, then
							// put it back in the book filter...so we need a RIFF in the sequence
							// BEFORE the delete.
							ReplaceInFilterFixer fixer1 = new ReplaceInFilterFixer(originalBook, null, m_app);
							m_cache.ActionHandlerAccessor.AddAction(fixer1);
						}
						m_scr.ScriptureBooksOS.Remove(originalBook);
					}
					IScrBook newBook = m_scr.CopyBookToCurrent(savedBook);
					ReplaceInFilterFixer fixer = new ReplaceInFilterFixer(null, newBook, m_app);

					fixer.Redo();
					if (m_cache.ActionHandlerAccessor != null)
						m_cache.ActionHandlerAccessor.AddAction(fixer);

					// FWNX-677 - caused by mono-calgary patch bug-614850_Modal_614850_v6.patch
					if (MiscUtils.IsUnix)
					{
						foreach(Form form in disabled_forms)
							form.Enabled = false;
						disabled_forms.Clear();
					}
				}
				undoHelper.RollBack = false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handled the click event of the Properties context menu.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void propertiesClick(object sender, EventArgs e)
		{
			TreeNode node = m_treeArchives.SelectedNode;
			if (node != null && node.Tag is IScrDraft)
			{
				IScrDraft draft = node.Tag as IScrDraft; //JEH
				using (DraftPropertiesDialog dlg = new DraftPropertiesDialog())
				{
					dlg.SetDialogInfo(draft);
					if (dlg.ShowDialog(this) == DialogResult.OK)
					{
						node.Text = GetSavedVersionLabel(draft);
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the NodeMouseClick event of the m_treeArchives control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.TreeNodeMouseClickEventArgs"/>
		/// instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_treeArchives_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
		{
			if (e.Button == System.Windows.Forms.MouseButtons.Right)
			{
				TreeNode node = e.Node;
				if (node == null || !(node.Tag is IScrDraft))
					return;
				m_treeArchives.SelectedNode = node;
				ContextMenuStrip menu = new ContextMenuStrip();
				menu.Items.Add(new ToolStripMenuItem(TeResourceHelper.GetResourceString("kstidProperties"), null,
												 new EventHandler(propertiesClick)));
				menu.Show(m_treeArchives, e.Location);
			}
		}
		#endregion

		#region Misc Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make a label for an archive object.  This will include its date/time created
		/// and its description .
		/// </summary>
		/// <param name="archive">the saved version</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private string GetSavedVersionLabel(IScrDraft archive)
		{
			string description;
			if (archive.Description == null)
				description = DlgResources.ResourceString("kstidEmptyArchiveLabel");
			else
				description = archive.Description;

			return archive.DateCreated.ToString() + " " + description;
		}
		#endregion

		#region IBookVersionAgent Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes a saved version of the current version of the book in the given BookMerger if
		/// needed (the agent is responsible for knowing whether a backup already exists).
		/// </summary>
		/// <param name="bookMerger">The book merger.</param>
		/// ------------------------------------------------------------------------------------
		public void MakeBackupIfNeeded(BookMerger bookMerger)
		{
			Debug.Fail("Will we want to create a backup here? We may refactor so that import and" +
				"saved versions dialog use the same method... and the same IBookVersionAgent");
			return;
		}
		#endregion
	}
	#endregion

	#region ArchiveDateComparer class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Compares the creation dates of saved versions.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ArchiveDateComparer : IComparer<TreeNode>
	{
		private bool m_ascending;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:ArchiveDateComparer"/> class.
		/// </summary>
		/// <param name="ascending">if set to <c>true</c> [ascending]; otherwise descending
		/// order.</param>
		/// ------------------------------------------------------------------------------------
		public ArchiveDateComparer(bool ascending)
		{
			m_ascending = ascending;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Comparison method that compares the dates of two archive objects for the purpose of
		/// sorting them.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Compare(TreeNode obj1, TreeNode obj2)
		{
			IScrDraft archive1 = (IScrDraft)obj1.Tag;
			IScrDraft archive2 = (IScrDraft)obj2.Tag;
			if (m_ascending)
				return archive1.DateCreated.CompareTo(archive2.DateCreated);
			else // compare for descending order sort
				return archive2.DateCreated.CompareTo(archive1.DateCreated);
		}
	}
	#endregion
}
