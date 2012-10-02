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
// File: FilterSectionDialog.cs
// --------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.FDO;
using XCore;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// Summary description for FilterScrSectionDialog.
	/// </summary>
	public class FilterScrSectionDialog : FilterScriptureDialog<IStText>, IFilterScrSectionDialog<IStText>
	{
		/// <summary>
		/// If the dialog is being used for exporting multiple sections at a time,
		/// then the tree must be pruned to show only those sections (and books)
		/// that were previously selected for interlinearization. The following
		/// two variables allow this pruning to take place at the appropriate time.
		/// </summary>
		private IStText m_text;
		private bool m_fPruneToSelectedSections;

		#region Constructor/Destructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the FilterScrSectionDialog class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="objList">A list of books to check as an array of hvos</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// ------------------------------------------------------------------------------------
		public FilterScrSectionDialog(FdoCache cache, IStText[] objList, IHelpTopicProvider helpTopicProvider)
			: base(cache, objList, helpTopicProvider)
		{
			InitializeComponent();
			m_helpTopicId = "khtpScrSectionFilter";
		}
		#endregion

		#region Overrides
		/// <summary>
		/// Overridden to add ScrSections and Title under each book.
		/// </summary>
		/// <param name="cache"></param>
		override protected void LoadScriptureList(FdoCache cache)
		{
			if (cache.LangProject.TranslatedScriptureOA == null || cache.LangProject.TranslatedScriptureOA.Hvo == 0)
				return;	// nothing to load
			m_treeScripture.LoadSections(cache);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// controls the logic for enabling/disabling buttons on this dialog.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		override protected void UpdateButtonState()
		{
			m_btnOK.Enabled = true;
		}

		/// <summary>
		/// Overridden to allow pruning after the checkmarks have been added to
		/// indicate sections selected for interlinearization already.
		/// </summary>
		/// <param name="e"></param>
		override protected void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			if (m_fPruneToSelectedSections)
				PruneToSelectedSections();
		}
		#endregion

		#region Public Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save the information needed to prune the tree later.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void PruneToSelectedSections(IStText text)
		{
			m_fPruneToSelectedSections = true;
			m_text = text;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove all sections (and books) that are not checked from the tree.  Clear
		/// all the checks except for the section given by hvoText.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void PruneToSelectedSections()
		{
			var unused = m_treeScripture.Nodes.Cast<TreeNode>().Where(PruneChild).ToList();
			foreach (TreeNode tn in unused)
				m_treeScripture.Nodes.Remove(tn);
		}

		/// <summary>
		/// Prune all of this node's children, then return true if this node should be removed.
		/// If this node is to stay, set it CheckState properly.
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		private bool PruneChild(TreeNode node)
		{
			if (node.Nodes.Count > 0)
			{
				List<TreeNode> unused = node.Nodes.Cast<TreeNode>().Where(PruneChild).ToList();
				foreach (TreeNode tn in unused)
					node.Nodes.Remove(tn);
			}
			if (node.Tag != null)
			{
				if (m_treeScripture.GetChecked(node) != TriStateTreeView.CheckState.Checked)
				{
					if (node.Nodes.Count == 0)
						return true;
				}
				else if (node.Tag is IStText && node.Tag != m_text)
				{
					m_treeScripture.SetChecked(node, TriStateTreeView.CheckState.Unchecked);
				}
				else
				{
					m_treeScripture.SelectedNode = node;
				}
			}
			else
			{
				if (node.Nodes.Count == 0)
					return true;
			}
			return false;
		}

		/// <summary>
		/// Get/set the label shown above the tree view.
		/// </summary>
		public string TreeViewLabel
		{
			get { return m_treeViewLabel.Text; }
			set { m_treeViewLabel.Text = value; }
		}
		#endregion

		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FilterScrSectionDialog));
			this.SuspendLayout();
			//
			// m_treeScripture
			//
			resources.ApplyResources(this.m_treeScripture, "m_treeScripture");
			this.m_treeScripture.LineColor = System.Drawing.Color.Black;
			this.m_treeScripture.MinimumSize = new System.Drawing.Size(312, 264);
			//
			// m_treeViewLabel
			//
			resources.ApplyResources(this.m_treeViewLabel, "m_treeViewLabel");
			//
			// m_btnOK
			//
			resources.ApplyResources(this.m_btnOK, "m_btnOK");
			//
			// FilterScrSectionDialog
			//
			resources.ApplyResources(this, "$this");
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
			this.Name = "FilterScrSectionDialog";
			this.m_helpProvider.SetShowHelp(this, ((bool)(resources.GetObject("$this.ShowHelp"))));
			this.Controls.SetChildIndex(this.m_btnOK, 0);
			this.Controls.SetChildIndex(this.m_treeViewLabel, 0);
			this.Controls.SetChildIndex(this.m_treeScripture, 0);
			this.ResumeLayout(false);
			this.PerformLayout();

		}
	}
}
