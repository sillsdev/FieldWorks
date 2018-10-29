// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	#if RANDYTODO
	// TODO: there is no need now for the FilterAllTextsDialog superclass, so merge it into this class.
	#endif
	/// <summary>
	/// FilterTextsDialog bundles both ordinary and Scripture texts, when appropriate.
	/// </summary>
	public class FilterTextsDialog : FilterAllTextsDialog
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

		/// <summary>
		/// Initializes a new instance of the ChooseScriptureDialog class.
		/// </summary>
		public FilterTextsDialog(IApp app, LcmCache cache, IStText[] objList, IHelpTopicProvider helpTopicProvider) : base(app, cache, objList, helpTopicProvider)
		{
			m_helpTopicId = "khtpChooseTexts";
			InitializeComponent();
		}
		#endregion

		#region Overrides
		/// <summary>
		/// Load all texts.
		/// </summary>
		protected override void LoadTexts()
		{
			m_treeTexts.LoadAllTexts();
		}

		/// <summary>
		/// controls the logic for enabling/disabling buttons on this dialog.
		/// </summary>
		protected override void UpdateButtonState()
		{
			m_btnOK.Enabled = true;
		}

		/// <summary>
		/// OK event handler. Checks the text list and warns about situations
		/// where no texts are selected.
		/// </summary>
		protected void OnOk(object obj, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			var showWarning = false;
			var message = ITextStrings.kOkbtnEmptySelection;
			var checkedList = m_treeTexts.GetCheckedNodeList();
#if RANDYTODO
			var own = Owner as IFwMainWnd;
			if (own != null && OnlyGenresChecked(checkedList))
			{
				message = ITextStrings.kOkbtnGenreSelection;
				own.PropTable.SetProperty("RecordList-DelayedGenreAssignment", checkedList, true, true);
				showWarning = true;
			}
#endif
			if (m_treeTexts.GetNodesWithState(TriStateTreeViewCheckState.Checked).Length == 0)
			{
				showWarning = true;
			}
			if (!showWarning)
			{
				return;
			}
			if (MessageBox.Show(message, ITextStrings.kOkbtnNoTextSelection, MessageBoxButtons.OKCancel) == DialogResult.Cancel)
			{
				DialogResult = DialogResult.None;
			}
		}

		/// <summary>
		/// Are only genres checked?
		/// </summary>
		/// <param name="checkedList">A list of TreeNodes that are also ICmPossibility(s)</param>
		/// <returns>true if not empty and all genres, false otherwise.</returns>
		private static bool OnlyGenresChecked(List<TreeNode> checkedList)
		{
			return checkedList.Count != 0 && checkedList.All(node => node.Name == "Genre");
		}

		#endregion

		#region Public Methods
		/// <summary>
		/// Remove all nodes that aren't in our list of interestingTexts from the tree (m_textsToShow).
		/// Initially select the one specified (m_selectedText).
		/// </summary>
		/// <param name="interestingTexts">The list of texts to display in the dialog.</param>
		/// <param name="selectedText">The text that should be initially checked in the dialog.</param>
		public void PruneToInterestingTextsAndSelect(IEnumerable<IStText> interestingTexts, IStText selectedText)
		{
			m_textsToShow = interestingTexts;
			m_selectedText = selectedText;
			// ToList() is absolutely necessary to keep from changing node collection while looping!
			var unusedNodes = m_treeTexts.Nodes.Cast<TreeNode>().Where(PruneChild).ToList();
			foreach (var treeNode in unusedNodes)
			{
				m_treeTexts.Nodes.Remove(treeNode);
			}
		}

		/// <summary>
		/// Prune all of this node's children, then return true if this node should be removed.
		/// If this node is to be selected, set its CheckState properly, otherwise uncheck it.
		/// </summary>
		private bool PruneChild(TreeNode node)
		{
			if (node.Nodes.Count > 0)
			{
				// ToList() is absolutely necessary to keep from changing node collection while looping!
				var unused = node.Nodes.Cast<TreeNode>().Where(PruneChild).ToList();
				foreach (var subTreeNode in unused)
				{
					node.Nodes.Remove(subTreeNode);
				}
			}
			if (node.Tag != null)
			{
				if (node.Tag is IStText)
				{
					if (!m_textsToShow.Contains(node.Tag as IStText))
					{
						return true;
					}
					if (node.Tag == m_selectedText)
					{
						m_treeTexts.SelectedNode = node;
						m_treeTexts.SetChecked(node, TriStateTreeViewCheckState.Checked);
					}
					else
					{
						m_treeTexts.SetChecked(node, TriStateTreeViewCheckState.Unchecked);
					}
				}
				else
				{
					if (node.Nodes.Count == 0)
					{
						return true; // Delete Genres and Books with no texts
					}
				}
			}
			else
			{
				// Usually this condition means 'No Genre', but could also be Testament node
				if (node.Nodes.Count == 0)
				{
					return true;
				}
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