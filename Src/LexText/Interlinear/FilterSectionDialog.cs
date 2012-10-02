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
// File: FilterSectionDialog.cs
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
using Microsoft.Win32;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.LangProj;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// Summary description for FilterScrSectionDialog.
	/// </summary>
	public class FilterScrSectionDialog : FilterScriptureDialog
	{
		/// <summary>
		/// If the dialog is being used for exporting multiple sections at a time,
		/// then the tree must be pruned to show only those sections (and books)
		/// that were previously selected for interlinearization.  The following
		/// two variables allow this pruning to take place at the appropriate time.
		/// </summary>
		private int m_hvoText = 0;
		private bool m_fPruneToSelectedSections = false;

		#region Constructor/Destructor
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FilterScrSectionDialog"/> class.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvoList">A list of books to check as an array of hvos</param>
		/// -----------------------------------------------------------------------------------
		public FilterScrSectionDialog(FdoCache cache, int[] hvoList)
			: base(cache, hvoList)
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
			if (cache.LangProject.TranslatedScriptureOAHvo == 0)
				return;	// nothing to load
			// We can't reference TeScrInitializer because TeViewConstructors needs to reference this and
			// it creates a circular dependency.
			ReflectionHelper.CallStaticMethod("TeScrInitializer.dll", "SIL.FieldWorks.TE.TeScrInitializer", "PreloadData",
				new object[] {cache, null});
			m_treeScripture.LoadSections(cache, true);
		}

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
		/// Return a list of HVO values for all of the included StText nodes.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public int[] GetListOfIncludedSections()
		{
			CheckDisposed();
			int[] selectedHvos = GetListOfIncludedScripture();
			List<int> sectionsToInclude = new List<int>();
			foreach (int hvo in selectedHvos)
			{
				// only return hvos of class StText
				if (m_cache.ClassIsOrInheritsFrom((uint)m_cache.GetClassOfObject(hvo), (uint)FDO.Cellar.StText.kclsidStText))
					sectionsToInclude.Add(hvo);
			}
			return sectionsToInclude.ToArray();
		}

		/// <summary>
		/// Save the information needed to prune the tree later.
		/// </summary>
		/// <param name="hvoText"></param>
		public void PruneToSelectedSections(int hvoText)
		{
			m_fPruneToSelectedSections = true;
			m_hvoText = hvoText;
		}

		/// <summary>
		/// Remove all sections (and books) that are not checked from the tree.  Clear
		/// all the checks except for the section given by hvoText.
		/// </summary>
		/// <param name="hvoText"></param>
		private void PruneToSelectedSections()
		{
			List<TreeNode> unused = new List<TreeNode>();
			foreach (TreeNode tn in m_treeScripture.Nodes)
			{
				if (PruneChild(tn))
					unused.Add(tn);
			}
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
			if (node.Nodes != null && node.Nodes.Count > 0)
			{
				List<TreeNode> unused = new List<TreeNode>();
				foreach (TreeNode tn in node.Nodes)
				{
					if (PruneChild(tn))
						unused.Add(tn);
				}
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
				else if ((int)node.Tag != m_hvoText)
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
