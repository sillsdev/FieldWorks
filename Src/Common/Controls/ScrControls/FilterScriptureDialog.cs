// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FilterScrSectionDialog.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Resources;
using Microsoft.Win32;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	///
	/// </summary>
	public class FilterScriptureDialog : Form, IFWDisposable
	{
		#region Data Members

		/// <summary>
		/// Help Provider
		/// </summary>
		protected System.Windows.Forms.HelpProvider m_helpProvider;
		/// <summary>Book tree view</summary>
		protected SIL.FieldWorks.Common.Controls.ScriptureTriStateTreeView m_treeScripture;
		/// <summary>
		/// label for the tree view.
		/// </summary>
		protected System.Windows.Forms.Label m_treeViewLabel;
		private IContainer components;
		/// <summary></summary>
		/// <remarks>protected because of testing</remarks>
		protected Button m_btnOK;
		/// <summary>
		/// list of scripture hvos
		/// </summary>
		protected int[] m_hvoList;
		/// <summary>
		/// fdo cache
		/// </summary>
		protected FdoCache m_cache;
		/// <summary>
		/// Help Topic Id
		/// </summary>
		protected string m_helpTopicId = "";
		#endregion

		#region Constructor/Destructor
		private FilterScriptureDialog()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			m_treeScripture.AfterCheck += new TreeViewEventHandler(OnCheckedChanged);
		}
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FilterScriptureDialog"/> class.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvoList">A list of books to check as an array of hvos</param>
		/// -----------------------------------------------------------------------------------
		public FilterScriptureDialog(FdoCache cache, int[] hvoList) : this()
		{
			if (cache == null)
				throw new ArgumentNullException("cache");
			if (hvoList == null)
				throw new ArgumentNullException("hvoList");

			m_cache = cache;
			m_hvoList = hvoList;

			// Add all of the scripture book names to the book list
			m_treeScripture.BeginUpdate();
			LoadScriptureList(cache);
			m_treeScripture.EndUpdate();
			m_treeScripture.ExpandToBooks();
		}

		/// <summary>
		/// Load the books of scripture
		/// </summary>
		/// <param name="cache"></param>
		virtual protected void LoadScriptureList(FdoCache cache)
		{
			if (m_cache == null)
				m_cache = cache;
			m_treeScripture.LoadBooks(cache);
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

		#endregion

		#region Public Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return a list of HVO values for all of the included books.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected int[] GetListOfIncludedScripture()
		{
			CheckDisposed();

			// Note: One might be tempted to have GetCheckedTagData
			// return a List<int>, but there is no reason to do so.
			// The data comes from the Tag property of a TreeNode, which has already boxed
			// them all as objects.
			return (int[])m_treeScripture.GetCheckedTagData().ToArray(typeof(int));
		}
		#endregion

		#region Windows Form Designer generated code
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.Windows.Forms.Button m_btnCancel;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FilterScriptureDialog));
			System.Windows.Forms.Button m_btnHelp;
			this.m_treeViewLabel = new System.Windows.Forms.Label();
			this.m_btnOK = new System.Windows.Forms.Button();
			this.m_treeScripture = new SIL.FieldWorks.Common.Controls.ScriptureTriStateTreeView();
			this.m_helpProvider = new System.Windows.Forms.HelpProvider();
			m_btnCancel = new System.Windows.Forms.Button();
			m_btnHelp = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// m_btnCancel
			//
			resources.ApplyResources(m_btnCancel, "m_btnCancel");
			m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			m_btnCancel.Name = "m_btnCancel";
			this.m_helpProvider.SetShowHelp(m_btnCancel, ((bool)(resources.GetObject("m_btnCancel.ShowHelp"))));
			//
			// m_btnHelp
			//
			resources.ApplyResources(m_btnHelp, "m_btnHelp");
			m_btnHelp.Name = "m_btnHelp";
			this.m_helpProvider.SetShowHelp(m_btnHelp, ((bool)(resources.GetObject("m_btnHelp.ShowHelp"))));
			m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// m_treeViewLabel
			//
			resources.ApplyResources(this.m_treeViewLabel, "m_treeViewLabel");
			this.m_treeViewLabel.Name = "m_treeViewLabel";
			this.m_helpProvider.SetShowHelp(this.m_treeViewLabel, ((bool)(resources.GetObject("m_treeViewLabel.ShowHelp"))));
			//
			// m_btnOK
			//
			resources.ApplyResources(this.m_btnOK, "m_btnOK");
			this.m_btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btnOK.Name = "m_btnOK";
			this.m_helpProvider.SetShowHelp(this.m_btnOK, ((bool)(resources.GetObject("m_btnOK.ShowHelp"))));
			//
			// m_treeScripture
			//
			resources.ApplyResources(this.m_treeScripture, "m_treeScripture");
			this.m_treeScripture.ItemHeight = 16;
			this.m_treeScripture.Name = "m_treeScripture";
			this.m_helpProvider.SetShowHelp(this.m_treeScripture, ((bool)(resources.GetObject("m_treeScripture.ShowHelp"))));
			//
			// FilterScriptureDialog
			//
			this.AcceptButton = this.m_btnOK;
			resources.ApplyResources(this, "$this");
			this.CancelButton = m_btnCancel;
			this.Controls.Add(this.m_treeScripture);
			this.Controls.Add(this.m_treeViewLabel);
			this.Controls.Add(m_btnHelp);
			this.Controls.Add(m_btnCancel);
			this.Controls.Add(this.m_btnOK);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FilterScriptureDialog";
			this.m_helpProvider.SetShowHelp(this, ((bool)(resources.GetObject("$this.ShowHelp"))));
			this.ShowInTaskbar = false;
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		#region Overridden Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load settings for the dialog
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);

			if (m_btnOK == null || m_hvoList == null)
				return;

			foreach (int hvo in m_hvoList)
				m_treeScripture.CheckNodeByTag(hvo, TriStateTreeView.CheckState.Checked);

			UpdateButtonState();
		}
		#endregion

		#region Event Handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Open the help window when the help button is pressed.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected void m_btnHelp_Click(object sender, System.EventArgs e)
		{
			ShowHelp.ShowHelpTopic(FwApp.App, m_helpTopicId);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called after the box is checked or unchecked
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected void OnCheckedChanged(object sender, TreeViewEventArgs e)
		{
			UpdateButtonState();
		}

		/// <summary>
		/// controls the logic for enabling/disabling buttons on this dialog.
		/// </summary>
		protected virtual void UpdateButtonState()
		{
			m_btnOK.Enabled = (m_treeScripture.GetCheckedTagData().Count > 0);
		}
		#endregion
	}

	/// <summary>
	/// TriStateTreeView that knows how to load scripture from the cache.
	/// </summary>
	public class ScriptureTriStateTreeView : TriStateTreeView
	{
		/// <summary>
		/// Load ScrBooks into the scripture tree view.
		/// </summary>
		/// <param name="cache"></param>
		public void LoadBooks(FdoCache cache)
		{
			if (cache.LangProject.TranslatedScriptureOA == null)
				return;
			try
			{
				cache.EnableBulkLoadingIfPossible(true);
				List<TreeNode> otBooks = new List<TreeNode>();
				List<TreeNode> ntBooks = new List<TreeNode>();
				foreach (IScrBook book in cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS)
				{
					TreeNode node = new TreeNode((book.BookIdRA as ScrBookRef).UIBookName);
					node.Tag = book.Hvo;
					node.Name = "Book";	// help us query for books.
					if (book.CanonicalNum < Scripture.kiNtMin)
						otBooks.Add(node);
					else
						ntBooks.Add(node);
				}

				TreeNode bibleNode = new TreeNode(ScrControls.kstidBibleNode);
				if (otBooks.Count > 0)
				{
					TreeNode testamentNode = new TreeNode(ScrControls.kstidOtNode,
						otBooks.ToArray());
					testamentNode.Name = "Testament"; // help us query for Testaments
					bibleNode.Nodes.Add(testamentNode);
				}

				if (ntBooks.Count > 0)
				{
					TreeNode testamentNode = new TreeNode(ScrControls.kstidNtNode,
						ntBooks.ToArray());
					testamentNode.Name = "Testament"; // help us query for Testaments
					bibleNode.Nodes.Add(testamentNode);
				}

				this.Nodes.Add(bibleNode);
			}
			finally
			{
				cache.EnableBulkLoadingIfPossible(false);
			}
		}

		/// <summary>
		/// Load sections into the books of a scripture tree view.
		/// </summary>
		/// <param name="cache"></param>
		public void LoadSections(FdoCache cache)
		{
			LoadSections(cache, false);
		}

		/// <summary>
		/// Load sections into the books of a scripture tree view optionally including the
		/// heading as well as the content of each section.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="fIncludeHeadings"></param>
		public void LoadSections(FdoCache cache, bool fIncludeHeadings)
		{
			try
			{
				cache.EnableBulkLoadingIfPossible(true);
				// first load the book ids.
				this.Nodes.Clear();
				this.LoadBooks(cache);

				TreeNode bibleNode;
				if (this.Nodes.Count == 1)
				{
					bibleNode = this.Nodes[0];
				}
				else if (this.Nodes.Count > 0)
				{
					throw new ArgumentException("We should only have 1 Bible node, not " + this.Nodes.Count);
				}
				else
					return;
				if (cache.LangProject.TranslatedScriptureOA == null)
					return;

				Scripture scripture = cache.LangProject.TranslatedScriptureOA as Scripture;
				foreach (TreeNode testament in bibleNode.Nodes)
				{
					foreach (TreeNode bookNode in testament.Nodes)
					{
						IScrBook book = ScrBook.CreateFromDBObject(cache, (int)bookNode.Tag) as IScrBook;
						// Add Title node.
						if (book.TitleOAHvo != 0)
						{
							TreeNode titleNode =
								new TreeNode(ResourceHelper.GetResourceString("kstidScriptureTitle"));
							titleNode.Name = book.TitleOAHvo.ToString();
							titleNode.Tag = book.TitleOAHvo;
							bookNode.Nodes.Add(titleNode);
						}

						// Add Sections.
						foreach (IScrSection section in book.SectionsOS)
						{
							string chapterVerseBridge = scripture.ChapterVerseBridgeAsString(section);
							if (fIncludeHeadings && section.HeadingOAHvo != 0)
							{
								// Include the heading text if it's not empty.  See LT-8764.
								int cTotal = 0;
								foreach (IStTxtPara para in section.HeadingOA.ParagraphsOS)
									cTotal += para.Contents.Length;
								if (cTotal > 0)
								{
									string sFmt = ResourceHelper.GetResourceString("kstidSectionHeading");
									TreeNode node = new TreeNode(String.Format(sFmt, chapterVerseBridge));
									node.Name = String.Format(sFmt, section.Hvo.ToString());
									node.Tag = section.HeadingOAHvo;	// expect an StText
									bookNode.Nodes.Add(node);
								}
							}
							TreeNode sectionNode =
								new TreeNode(chapterVerseBridge);
							sectionNode.Name = section.Hvo.ToString();
							sectionNode.Tag = section.ContentOAHvo;	// expect an StText
							bookNode.Nodes.Add(sectionNode);
						}

						// Add Footnotes in reverse order, so we can insert them in the proper order.
						List<IStFootnote> footnotes = new List<IStFootnote>(book.FootnotesOS);
						footnotes.Reverse();
						foreach (IStFootnote footnote in footnotes)
						{
							ScrFootnote scrFootnote = footnote as ScrFootnote;
							if (scrFootnote == null)
								scrFootnote = new ScrFootnote(cache, footnote.Hvo);
							//  insert under the relevant section, if any (LTB-408)
							int hvoContainingObj;
							if (scrFootnote.TryGetContainingSectionHvo(out hvoContainingObj) ||
								scrFootnote.TryGetContainingTitle(out hvoContainingObj))
							{
								string nodeName = scripture.ContainingRefAsString(scrFootnote);
								TreeNode footnoteNode = new TreeNode(nodeName);
								footnoteNode.Tag = footnote.Hvo;
								footnoteNode.Name = "Footnote";

								// see if we can lookup the node of this section.
								int nodeIndex = bookNode.Nodes.IndexOfKey(hvoContainingObj.ToString());
								//TreeNode[] sectionNodes = bookNode.Nodes.Find(hvoSection.ToString(), false);
								//if (sectionNodes != null && sectionNodes.Length > 0)
								if (nodeIndex >= 0)
									bookNode.Nodes.Insert(nodeIndex + 1, footnoteNode);
								else
									bookNode.Nodes.Add(footnoteNode);	// insert at end.
							}
						}
					}
				}
			}
			finally
			{
				cache.EnableBulkLoadingIfPossible(false);
			}
		}

		/// <summary>
		/// Expand nodes to book level
		/// </summary>
		public void ExpandToBooks()
		{
			this.ExpandAll();
			// show only book level
			TreeNode[] scriptureNodes = this.Nodes.Find("Book", true);
			foreach (TreeNode node in scriptureNodes)
			{
				node.Collapse();
			}
		}
	}
}
