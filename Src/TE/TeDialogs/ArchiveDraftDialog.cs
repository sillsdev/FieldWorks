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
// File: ArchiveDraftDialog.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.TE;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Dialog used to take a snapshot of the current Scripture an save a version (archive) in
	/// the database
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class SaveVersionDialog : Form, IFWDisposable
	{
		#region Member variables

		private FdoCache m_cache;
		private IScripture m_scr;
		private TriStateTreeView m_treeView;
		private List<int> m_BooksToSave;
		private System.Windows.Forms.TextBox m_description;
		private Button m_btnOk;
		private System.ComponentModel.IContainer components = null;
		private IScrDraft m_SavedVersion;

		#endregion

		#region Constructors and Deconstructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="SaveVersionDialog"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SaveVersionDialog(): this(null)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="SaveVersionDialog"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SaveVersionDialog(FdoCache cache)
		{
			m_cache = cache;
			m_scr = m_cache.LangProject.TranslatedScriptureOA;

			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			if (m_cache == null)
				return;

			m_BooksToSave = new List<int>();
			m_treeView.BeginUpdate();

			List<TreeNode> otBooks = new List<TreeNode>();
			List<TreeNode> ntBooks = new List<TreeNode>();
			foreach (IScrBook book in m_scr.ScriptureBooksOS)
			{
				TreeNode node = new TreeNode((book as ScrBook).BestUIName);
				node.Tag = book.Hvo;

				if (book.CanonicalNum < Scripture.kiNtMin)
					otBooks.Add(node); // OT book
				else
					ntBooks.Add(node); // NT book
			}
			TreeNode bibleNode = new TreeNode(TeResourceHelper.GetResourceString("kstidBibleNode"));
			if (otBooks.Count > 0)
			{
				bibleNode.Nodes.Add(new TreeNode(TeResourceHelper.GetResourceString("kstidOtNode"),
					otBooks.ToArray()));
			}
			if (ntBooks.Count > 0)
			{
				bibleNode.Nodes.Add(new TreeNode(TeResourceHelper.GetResourceString("kstidNtNode"),
					ntBooks.ToArray()));
			}

			m_treeView.Nodes.Add(bibleNode);

			// REVIEW: once we have sections we probably don't want to expand below book level
			m_treeView.ExpandAll();
			m_treeView.EndUpdate();
			// update the ok button enabled state
			m_treeView_NodeCheckChanged(null, EventArgs.Empty);
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
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
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
			this.components = new System.ComponentModel.Container();
			System.Windows.Forms.Label label1;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SaveVersionDialog));
			System.Windows.Forms.Button m_btnCancel;
			System.Windows.Forms.Button m_btnHelp;
			System.Windows.Forms.Label label2;
			this.m_btnOk = new System.Windows.Forms.Button();
			this.m_treeView = new SIL.FieldWorks.Common.Controls.TriStateTreeView();
			this.m_description = new System.Windows.Forms.TextBox();
			label1 = new System.Windows.Forms.Label();
			m_btnCancel = new System.Windows.Forms.Button();
			m_btnHelp = new System.Windows.Forms.Button();
			label2 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			//
			// label1
			//
			resources.ApplyResources(label1, "label1");
			label1.Name = "label1";
			//
			// m_btnCancel
			//
			resources.ApplyResources(m_btnCancel, "m_btnCancel");
			m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			m_btnCancel.Name = "m_btnCancel";
			//
			// m_btnHelp
			//
			resources.ApplyResources(m_btnHelp, "m_btnHelp");
			m_btnHelp.Name = "m_btnHelp";
			m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// label2
			//
			resources.ApplyResources(label2, "label2");
			label2.Name = "label2";
			//
			// m_btnOk
			//
			resources.ApplyResources(this.m_btnOk, "m_btnOk");
			this.m_btnOk.Name = "m_btnOk";
			this.m_btnOk.Click += new System.EventHandler(this.OnOk);
			//
			// m_treeView
			//
			resources.ApplyResources(this.m_treeView, "m_treeView");
			this.m_treeView.ItemHeight = 16;
			this.m_treeView.Name = "m_treeView";
			this.m_treeView.NodeCheckChanged += new System.EventHandler(this.m_treeView_NodeCheckChanged);
			//
			// m_description
			//
			resources.ApplyResources(this.m_description, "m_description");
			this.m_description.Name = "m_description";
			//
			// SaveVersionDialog
			//
			this.AcceptButton = this.m_btnOk;
			resources.ApplyResources(this, "$this");
			this.CancelButton = m_btnCancel;
			this.Controls.Add(this.m_description);
			this.Controls.Add(label2);
			this.Controls.Add(this.m_treeView);
			this.Controls.Add(m_btnHelp);
			this.Controls.Add(m_btnCancel);
			this.Controls.Add(this.m_btnOk);
			this.Controls.Add(label1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "SaveVersionDialog";
			this.ShowInTaskbar = false;
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		#region Event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// User clicked OK button
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void OnOk(object sender, System.EventArgs e)
		{
			using (new WaitCursor(this))
			{
				StepThroughNodesAndAddToList(m_treeView.Nodes[0]);
				if (m_BooksToSave.Count == 0)
				{
					Close();
					return;
				}

				using (new SuppressSubTasks(m_cache))
				{
					string sSavePoint;
					m_cache.DatabaseAccessor.SetSavePointOrBeginTrans(out sSavePoint);
					try
					{
						m_SavedVersion = m_scr.CreateSavedVersion(m_description.Text,
							m_BooksToSave.ToArray());
						m_cache.DatabaseAccessor.CommitTrans();
					}
					catch
					{
						m_cache.DatabaseAccessor.RollbackSavePoint(sSavePoint);
						throw;
					}
					finally
					{
						Close();
					}
				}
			}
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
			ShowHelp.ShowHelpTopic(FwApp.App, "khtpSaveVersionDialog");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the NodeCheckChanged event of the m_treeView control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_treeView_NodeCheckChanged(object sender, EventArgs e)
		{
			StepThroughNodesAndAddToList(m_treeView.Nodes[0]);
			m_btnOk.Enabled = (m_BooksToSave.Count > 0);
		}
		#endregion

		#region Private methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add current node to list of books to save and steps through all child nodes
		/// </summary>
		/// <param name="node"></param>
		/// ------------------------------------------------------------------------------------
		private void StepThroughNodesAndAddToList(TreeNode node)
		{
			m_BooksToSave.Clear();
			StepThroughNodes(node);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add current node to list of books to save and steps through all child nodes
		/// </summary>
		/// <param name="node">The starting node.</param>
		/// ------------------------------------------------------------------------------------
		private void StepThroughNodes(TreeNode node)
		{
			if (m_treeView.GetChecked(node) == TriStateTreeView.CheckState.Checked
				&& node.Tag != null && (int)node.Tag > 0)
			{
				m_BooksToSave.Add((int)node.Tag);
			}

			if (node.Nodes != null)
			{
				foreach (TreeNode childNode in node.Nodes)
					StepThroughNodes(childNode);
			}
		}
		#endregion

		#region Public Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the saved version.
		/// </summary>
		/// <value>The saved version.</value>
		/// ------------------------------------------------------------------------------------
		public IScrDraft SavedVersion
		{
			get { return m_SavedVersion; }
		}
		#endregion
	}
}
