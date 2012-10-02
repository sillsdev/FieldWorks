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
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using SIL.FieldWorks.FDO.Infrastructure;
using XCore;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// FilterTextsDialog bundles both texts and, when appropriate,
	/// This file cannot be moved to the ITextDll: ../Src/LexText/Interlinear because that
	/// dll is referenced by SIL.FieldWorks.TE and would create a circular reference.
	/// It can't be moved to FwControls either for a similar reason - ScrControls uses FwControls!
	/// This class uses TE to make sure the scriptures are properly initialized for use.
	/// </summary>
	public class FilterTextsDialog : FilterAllTextsDialog<IStText>, IFilterTextsDialog<IStText>
	{
		/// <summary>
		/// If the dialog is being used for exporting multiple texts at a time,
		/// then the tree must be pruned to show only those texts (and scripture books)
		/// that were previously selected for interlinearization. The following
		/// two variables allow this pruning to take place at the appropriate time.
		/// </summary>
		private IStText m_text;
		private bool m_fPruneToSelectedSections;

		#region Constructor/Destructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the ChooseScriptureDialog class.
		/// WARNING: this constructor is called by reflection, at least in the Interlinear
		/// Text DLL. If you change its parameters be SURE to find and fix those callers also.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="objList">A list of texts and books to check as an array of hvos</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// ------------------------------------------------------------------------------------
		public FilterTextsDialog(FdoCache cache, IStText[] objList,
			IHelpTopicProvider helpTopicProvider) : base(cache, objList, helpTopicProvider)
		{
			m_helpTopicId = "khtpChooseTexts";
			InitializeComponent();
		}
		#endregion

		#region Overrides
		/// <summary>
		/// Load the general texts.
		/// </summary>
		protected override void LoadTexts()
		{
			m_treeTexts.LoadGeneralTexts(m_cache);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// controls the logic for enabling/disabling buttons on this dialog.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void UpdateButtonState()
		{
			m_btnOK.Enabled = true;
		}

		/// <summary>
		/// Overridden to allow pruning after the checkmarks have been added to
		/// indicate sections selected for interlinearization already.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnHandleCreated(EventArgs e)
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
		public void PruneToSelectedTexts(IStText text)
		{
			m_fPruneToSelectedSections = true;
			m_text = text;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove all unchecked nodes from the tree.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void PruneToSelectedSections()
		{
			var unused = m_treeTexts.Nodes.Cast<TreeNode>().Where(PruneChild);
			foreach (TreeNode tn in unused)
				m_treeTexts.Nodes.Remove(tn);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Prune all of this node's children, then return true if this node should be removed.
		/// If this node is to stay, set its CheckState properly.
		/// </summary>
		/// ------------------------------------------------------------------------------------
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
				if (m_treeTexts.GetChecked(node) != TriStateTreeView.CheckState.Checked)
				{
					if (node.Nodes.Count == 0)
						return true;
				}
				else if (node.Tag is IStText && node.Tag != m_text)
				{
					m_treeTexts.SetChecked(node, TriStateTreeView.CheckState.Unchecked);
				}
				else
				{
					m_treeTexts.SelectedNode = node;
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
			var resources = new System.ComponentModel.ComponentResourceManager(typeof(FilterAllTextsDialog));
			this.SuspendLayout();
			//
			// m_treeTexts
			//
			resources.ApplyResources(this.m_treeTexts, "m_treeTexts");
			this.m_treeTexts.LineColor = System.Drawing.Color.Black;
			this.m_treeTexts.MinimumSize = new System.Drawing.Size(312, 264);
			//
			// m_treeViewLabel
			//
			resources.ApplyResources(this.m_treeViewLabel, "m_treeViewLabel");
			//
			// m_btnOK
			//
			resources.ApplyResources(this.m_btnOK, "m_btnOK");
			//
			// FilterTextsDialog
			//
			resources.ApplyResources(this, "$this");
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
			this.Name = "FilterTextsDialog";
			this.m_helpProvider.SetShowHelp(this, ((bool)(resources.GetObject("$this.ShowHelp"))));
			this.Controls.SetChildIndex(this.m_btnOK, 0);
			this.Controls.SetChildIndex(this.m_treeViewLabel, 0);
			this.Controls.SetChildIndex(this.m_treeTexts, 0);
			this.ResumeLayout(false);
			this.PerformLayout();

		}
	}
}
