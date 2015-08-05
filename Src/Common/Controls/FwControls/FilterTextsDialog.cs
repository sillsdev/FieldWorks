// Copyright (c) 2004-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.FDO;
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
		/// three variables allow this pruning to take place at the appropriate time.
		/// The m_selectedText variable indicates which text should be intially checked,
		/// as per LT-12177.
		/// </summary>
		private IEnumerable<IStText> m_textsToShow;
		private IStText m_selectedText;

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
		/// OK event handler. Checks the text list and warns about situations
		/// where no texts are selected.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="e"></param>
		protected void OnOk(Object obj, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			bool showWarning = false;
			string message = FwControls.kOkbtnEmptySelection;
			var checkedList = m_treeTexts.GetCheckedNodeList();
			var own = Owner as XWindow;
			if (own != null && OnlyGenresChecked(checkedList))
			{
				message = FwControls.kOkbtnGenreSelection;
				own.PropTable.SetProperty("RecordClerk-DelayedGenreAssignment", checkedList, true, true);
				showWarning = true;
			}
			if (m_treeTexts.GetNodesWithState(TriStateTreeView.CheckState.Checked).Length == 0)
				showWarning = true;
			if (showWarning)
			{
				DialogResult result;
				MessageBoxButtons buttons = MessageBoxButtons.OKCancel;
				result = MessageBox.Show(message, FwControls.kOkbtnNoTextSelection, buttons);
				if (result == DialogResult.Cancel) DialogResult = DialogResult.None;
			}
		}

		/// <summary>
		/// Are only genres checked?
		/// </summary>
		/// <param name="checkedList">A list of TreeNodes that are also ICmPossibility(s)</param>
		/// <returns>true if not empty and all genres, false otherwise.</returns>
		private bool OnlyGenresChecked(List<TreeNode> checkedList)
		{
			if (checkedList.Count == 0) return false;
			return checkedList.All(node => node.Name == "Genre");
		}

		#endregion

		#region Public Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove all nodes that aren't in our list of interestingTexts from the tree (m_textsToShow).
		/// Initially select the one specified (m_selectedText).
		/// </summary>
		/// <param name="interestingTexts">The list of texts to display in the dialog.</param>
		/// <param name="selectedText">The text that should be initially checked in the dialog.</param>
		/// ------------------------------------------------------------------------------------
		public void PruneToInterestingTextsAndSelect(IEnumerable<IStText> interestingTexts, IStText selectedText)
		{
			m_textsToShow = interestingTexts;
			m_selectedText = selectedText;
			// ToList() is absolutely necessary to keep from changing node collection while looping!
			var unusedNodes = m_treeTexts.Nodes.Cast<TreeNode>().Where(PruneChild).ToList();
			foreach (var treeNode in unusedNodes)
				m_treeTexts.Nodes.Remove(treeNode);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Prune all of this node's children, then return true if this node should be removed.
		/// If this node is to be selected, set its CheckState properly, otherwise uncheck it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool PruneChild(TreeNode node)
		{
			if (node.Nodes.Count > 0)
			{
				// ToList() is absolutely necessary to keep from changing node collection while looping!
				var unused = node.Nodes.Cast<TreeNode>().Where(PruneChild).ToList();
				foreach (var subTreeNode in unused)
					node.Nodes.Remove(subTreeNode);
			}
			if (node.Tag != null)
			{
				if (node.Tag is IStText)
				{
					if (!m_textsToShow.Contains(node.Tag as IStText))
						return true;
					if (node.Tag == m_selectedText)
					{
						m_treeTexts.SelectedNode = node;
						m_treeTexts.SetChecked(node, TriStateTreeView.CheckState.Checked);
					}
					else
						m_treeTexts.SetChecked(node, TriStateTreeView.CheckState.Unchecked);
				}
				else
				{
					if (node.Nodes.Count == 0)
						return true; // Delete Genres and Books with no texts
				}
			}
			else
			{
				// Usually this condition means 'No Genre', but could also be Testament node
				if (node.Nodes.Count == 0)
					return true;
			}
			return false; // Keep this node!
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
			this.m_btnOK.Click += this.OnOk;
			this.Controls.SetChildIndex(this.m_treeViewLabel, 0);
			this.Controls.SetChildIndex(this.m_treeTexts, 0);
			this.ResumeLayout(false);
			this.PerformLayout();

		}
	}
}
