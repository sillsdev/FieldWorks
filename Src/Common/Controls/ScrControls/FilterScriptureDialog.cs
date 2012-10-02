// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2004' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FilterScrSectionDialog.cs
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using XCore;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Abstract base dialog for displaying a list of Scripture portions (books, sections, etc.)
	/// and allowing the user to choose which ones to include
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public abstract class FilterScriptureDialog : Form
	{
		#region Data Members
		/// <summary>
		/// Help Provider
		/// </summary>
		protected HelpProvider m_helpProvider;
		/// <summary>Book tree view</summary>
		protected ScriptureTriStateTreeView m_treeScripture;
		/// <summary>
		/// label for the tree view.
		/// </summary>
		protected Label m_treeViewLabel;
		//		private IContainer components;
		/// <summary></summary>
		/// <remarks>protected because of testing</remarks>
		protected Button m_btnOK;
		private System.ComponentModel.IContainer components;
		#endregion

		#region Constructor/Destructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FilterScriptureDialog"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected FilterScriptureDialog()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			m_treeScripture.AfterCheck += OnCheckedChanged;
			AccessibleName = "FilterScriptureDialog";
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

		#region Event Handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Open the help window when the help button is pressed.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected abstract void m_btnHelp_Click(object sender, System.EventArgs e);

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

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Abstract base dialog for displaying a list of Scripture portions (books, sections, etc.)
	/// and allowing the user to choose which ones to include
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public abstract class FilterScriptureDialog<T> : FilterScriptureDialog where T : ICmObject
	{
		#region Data Members
		/// <summary>
		/// list of Scripture objects
		/// </summary>
		protected T[] m_objList;
		/// <summary>
		/// fdo cache
		/// </summary>
		protected FdoCache m_cache;
		/// <summary>
		/// Help Topic Id
		/// </summary>
		protected string m_helpTopicId = "";
		private IHelpTopicProvider m_helpTopicProvider;
		#endregion

		#region Constructor/Destructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the FilterScriptureDialog class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="objList">A list of objects to check as an array</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// ------------------------------------------------------------------------------------
		public FilterScriptureDialog(FdoCache cache, T[] objList, IHelpTopicProvider helpTopicProvider) : base()
		{
			if (cache == null)
				throw new ArgumentNullException("cache");
			if (objList == null)
				throw new ArgumentNullException("bookList");

			m_cache = cache;
			m_objList = objList;
			m_helpTopicProvider = helpTopicProvider;

			// Add all of the scripture book names to the book list
			m_treeScripture.BeginUpdate();
			LoadScriptureList(cache);
			m_treeScripture.EndUpdate();
			m_treeScripture.ExpandToBooks();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load the books of scripture
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void LoadScriptureList(FdoCache cache)
		{
			if (m_cache == null)
				m_cache = cache;
			m_treeScripture.LoadBooks(cache);
		}
		#endregion

		#region Public Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return an array of all of the included objects of the filter type.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public T[] GetListOfIncludedScripture()
		{
			return m_treeScripture.GetCheckedTagData().OfType<T>().ToArray();
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

			if (m_btnOK == null || m_objList == null)
				return;

			foreach (ICmObject obj in m_objList)
				m_treeScripture.CheckNodeByTag(obj, TriStateTreeView.CheckState.Checked);

			UpdateButtonState();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Open the help window when the help button is pressed.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void m_btnHelp_Click(object sender, System.EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, m_helpTopicId);
		}
		#endregion
	}

	#region ScriptureTriStateTreeView class
	/// <summary>
	/// TriStateTreeView that knows how to load scripture from the cache.
	/// </summary>
	public class ScriptureTriStateTreeView : TriStateTreeView
	{
		/// <summary>
		/// Make one.
		/// </summary>
		public ScriptureTriStateTreeView()
		{
			BeforeExpand += ScriptureTriStateTreeView_BeforeExpand;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load ScrBooks into the scripture tree view.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// ------------------------------------------------------------------------------------
		public void LoadBooks(FdoCache cache)
		{
			if (cache.LanguageProject.TranslatedScriptureOA == null)
				return;

			List<TreeNode> otBooks = new List<TreeNode>();
			List<TreeNode> ntBooks = new List<TreeNode>();
			foreach (IScrBook book in cache.LanguageProject.TranslatedScriptureOA.ScriptureBooksOS)
			{
				TreeNode node = new TreeNode(book.BookIdRA.BookName.UserDefaultWritingSystem.Text);
				node.Tag = book;
				node.Name = "Book";	// help us query for books.
				if (book.CanonicalNum < ScriptureTags.kiNtMin)
					otBooks.Add(node);
				else
					ntBooks.Add(node);
			}

			TreeNode bibleNode = new TreeNode(ScrControls.kstidBibleNode);
			if (otBooks.Count > 0)
			{
				TreeNode testamentNode = new TreeNode(ScrControls.kstidOtNode, otBooks.ToArray());
				testamentNode.Name = "Testament"; // help us query for Testaments
				bibleNode.Nodes.Add(testamentNode);
			}

			if (ntBooks.Count > 0)
			{
				TreeNode testamentNode = new TreeNode(ScrControls.kstidNtNode, ntBooks.ToArray());
				testamentNode.Name = "Testament"; // help us query for Testaments
				bibleNode.Nodes.Add(testamentNode);
			}

			Nodes.Add(bibleNode);
		}

		/// <summary>
		/// Since we're being lazy about filling in sections and footnotes, we need to do it for
		/// checked books when obtaining their children.
		/// </summary>
		protected override void FillInMissingChildren(TreeNode bookNode)
		{
			FillInChildren(bookNode, true);
		}

		/// <summary>
		/// If using lazy initialization, fill this in to make sure the relevant part of the tree is built
		/// so that the specified tag can be checked.
		/// </summary>
		/// <param name="tag"></param>
		protected override void FillInIfHidden(object tag)
		{
			if (tag is ICmObject)
			{
				IScrBook book = ((ICmObject)tag).OwnerOfClass<IScrBook>();
				if (book != null)
					FillInChildren(FindNode(Nodes, book), false);
			}
		}

		private static TreeNode FindNode(TreeNodeCollection nodes, IScrBook book)
		{
			foreach (TreeNode node in nodes)
			{
				if (node.Tag != null && node.Tag.Equals(book))
					return node;
				var result = FindNode(node.Nodes, book);
				if (result != null)
					return result;
			}
			return null;
		}

		/// ------------------------------------------------------------------------
		/// <summary>
		/// Fills the in children.
		/// </summary>
		/// <param name="bookNode">The book node. This can (rarely) be null, in
		/// which case, this method won't do anything. Caller needs to be able to
		/// deal with that possibility. See FWR-3498 for details.</param>
		/// <param name="checkChildren">if set to <c>true</c> all child nodes will
		/// be selected.</param>
		/// ------------------------------------------------------------------------
		private void FillInChildren(TreeNode bookNode, bool checkChildren)
		{
			if (bookNode != null && bookNode.Tag is IScrBook && bookNode.Nodes.Count == 1 && bookNode.Nodes[0].Name == ksDummmyName)
			{
				BeforeExpand -= ScriptureTriStateTreeView_BeforeExpand; // prevent recursion
				FillInBookChildren(bookNode, ((IScrBook)bookNode.Tag).Cache.LangProject.TranslatedScriptureOA);
				if (checkChildren)
					CheckAllChildren(bookNode);
				BeforeExpand += ScriptureTriStateTreeView_BeforeExpand;
			}
		}

		private void CheckAllChildren(TreeNode node)
		{
			foreach (TreeNode child in node.Nodes)
			{
				SetChecked(child, CheckState.Checked);
				CheckAllChildren(child);
			}
		}

		void ScriptureTriStateTreeView_BeforeExpand(object sender, TreeViewCancelEventArgs e)
		{
			FillInChildren(e.Node, false);
		}

		private const string ksDummmyName = "dummy"; // used for Name of dummy nodes.
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load sections into the books of a scripture tree view optionally including the
		/// heading as well as the content of each section.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// ------------------------------------------------------------------------------------
		public void LoadSections(FdoCache cache)
		{
			// first load the book ids.
			Nodes.Clear();
			LoadBooks(cache);

			if (Nodes.Count == 0)
				return;
			if (Nodes.Count > 1)
				throw new ArgumentException("We should only have 1 Bible node, not " + Nodes.Count);

			TreeNode bibleNode = Nodes[0];

			foreach (TreeNode testament in bibleNode.Nodes)
			{
				foreach (TreeNode bookNode in testament.Nodes)
				{
					// Put a dummy node into each book so the computer thinks it can be expanded.
					// When it is, we will replace this with the real children.
					TreeNode node = new TreeNode(ksDummmyName);
					node.Name = ksDummmyName;
					bookNode.Nodes.Add(node);
				}
			}
		}

		/// <summary>
		/// Fill in the children for a particular book. This is typically done when it gets expanded or
		/// when we need a list including the children. Separating it from the initial load saves a lot of
		/// time when we have a long list of books.
		/// </summary>
		public void FillInBookChildren(TreeNode bookNode, IScripture scripture)
		{
			bookNode.Nodes.Clear(); // Gets rid of dummy.
			IScrBook book = (IScrBook)bookNode.Tag;
			// Add Title node.
			if (book.TitleOA != null)
			{
				TreeNode titleNode =
					new TreeNode(ResourceHelper.GetResourceString("kstidScriptureTitle"));
				titleNode.Name = book.TitleOA.ToString();
				titleNode.Tag = book.TitleOA;
				bookNode.Nodes.Add(titleNode);
			}

			// Add Sections.
			foreach (IScrSection section in book.SectionsOS)
			{
				string chapterVerseBridge = scripture.ChapterVerseBridgeAsString(section);
				if (section.HeadingOA != null)
				{
					// Include the heading text if it's not empty.  See LT-8764.
					int cTotal = 0;
					foreach (IScrTxtPara para in section.HeadingOA.ParagraphsOS)
						cTotal += para.Contents.Length;
					if (cTotal > 0)
					{
						string sFmt = ResourceHelper.GetResourceString("kstidSectionHeading");
						TreeNode node = new TreeNode(String.Format(sFmt, chapterVerseBridge));
						node.Name = String.Format(sFmt, section);
						node.Tag = section.HeadingOA; // expect an StText
						bookNode.Nodes.Add(node);
					}
				}
				TreeNode sectionNode = new TreeNode(chapterVerseBridge);
				sectionNode.Name = section.ToString();
				sectionNode.Tag = section.ContentOA; // expect an StText
				bookNode.Nodes.Add(sectionNode);
			}

			// Add Footnotes in reverse order, so we can insert them in the proper order.
			List<IScrFootnote> footnotes = new List<IScrFootnote>(book.FootnotesOS);
			footnotes.Reverse();
			foreach (IScrFootnote scrFootnote in footnotes)
			{
				// insert under the relevant section, if any (LTB-408)
				IScrSection containingSection;
				IStText containingTitle = null;
				if (scrFootnote.TryGetContainingSection(out containingSection) ||
					scrFootnote.TryGetContainingTitle(out containingTitle))
				{
					string nodeName = scripture.ContainingRefAsString(scrFootnote);
					TreeNode footnoteNode = new TreeNode(nodeName);
					footnoteNode.Tag = scrFootnote;
					footnoteNode.Name = "Footnote";

					// see if we can lookup the node of this section.
					int nodeIndex = bookNode.Nodes.IndexOfKey(
						containingSection != null ? containingSection.ToString() : containingTitle.ToString());
					//TreeNode[] sectionNodes = bookNode.Nodes.Find(hvoSection.ToString(), false);
					//if (sectionNodes != null && sectionNodes.Length > 0)
					if (nodeIndex >= 0)
						bookNode.Nodes.Insert(nodeIndex + 1, footnoteNode);
					else
						bookNode.Nodes.Add(footnoteNode);	// insert at end.
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Expand nodes to book level
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ExpandToBooks()
		{
			ExpandAll();
			// Collapse anything below book level
			foreach (TreeNode node in Nodes.Find("Book", true))
				node.Collapse();
		}
	}
	#endregion
}
