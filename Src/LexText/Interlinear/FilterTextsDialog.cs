// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using XCore;

namespace SIL.FieldWorks.IText
{
	#if RANDYTODO
	// TODO: there is no need now for the FilterAllTextsDialog superclass, so merge it into this class.
	#endif
	/// <summary>
	/// FilterTextsDialog bundles both ordinary and Scripture texts, when appropriate,
	/// </summary>
	public class FilterTextsDialog : FilterAllTextsDialog
	{
		/// <summary>
		/// If the dialog is being used for exporting multiple texts at a time,
		/// then the tree must be pruned to show only those texts (and scripture books)
		/// that were previously selected for interlinearization.
		/// This pruning must take place at the appropriate time.
		/// If this property is not set, the tree will not be pruned.
		/// </summary>
		public IEnumerable<IStText> TextsToShow { private get; set; }

		#region Constructor/Destructor

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the ChooseScriptureDialog class.
		/// WARNING: this constructor is called by reflection, at least in the Interlinear
		/// Text DLL. If you change its parameters be SURE to find and fix those callers also.
		/// </summary>
		/// <param name="app"></param>
		/// <param name="cache">The cache.</param>
		/// <param name="objList">A list of texts and books to check as an array of IStTexts</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// ------------------------------------------------------------------------------------
		public FilterTextsDialog(IApp app, LcmCache cache, IStText[] objList, IHelpTopicProvider helpTopicProvider) : base(app, cache, objList, helpTopicProvider)
		{
			m_helpTopicId = "khtpChooseTexts";
			InitializeComponent();
		}
		#endregion

		#region Overrides
		/// <summary>
		/// Load all texts. Prune if necessary.
		/// </summary>
		protected override void LoadTexts()
		{
			m_treeTexts.LoadAllTexts();
			PruneToTextsToShowIfAny();
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
		protected void OnOk(object obj, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			var showWarning = false;
			var message = ITextStrings.kOkbtnEmptySelection;
			var checkedList = m_treeTexts.GetCheckedNodeList();
			var own = Owner as XWindow;
			if (own != null && OnlyGenresChecked(checkedList))
			{
				message = ITextStrings.kOkbtnGenreSelection;
				own.PropTable.SetProperty("RecordClerk-DelayedGenreAssignment", checkedList, true);
				showWarning = true;
			}
			if (m_treeTexts.GetNodesWithState(TriStateTreeView.CheckState.Checked).Length == 0)
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
			if (checkedList.Count == 0) return false;
			return checkedList.All(node => node.Name == "Genre");
		}

		#endregion

		/// <summary>
		/// If TextsToShow is not null, remove all nodes that aren't in that list.
		/// </summary>
		private void PruneToTextsToShowIfAny()
		{
			if (TextsToShow == null)
				return;

			// ToList() is absolutely necessary to keep from changing node collection while looping!
			var unusedNodes = m_treeTexts.Nodes.Cast<TreeNode>().Where(PruneChild).ToList();
			foreach (var treeNode in unusedNodes)
				m_treeTexts.Nodes.Remove(treeNode);
		}

		/// <summary>
		/// Prune all of this node's children, then return true if this node should be removed.
		/// Select the first node that is to be checked (so it is in the user's view if the list is long).
		/// </summary>
		/// <remarks>
		/// Pruning happens before exporting texts. Only those texts selected for display are available for export.
		/// Hasso 2020.07: To permit lazy loading of scripture sections, scripture is pruned with book granularity. That is, if any portion of a book
		/// is selected to show, the entire book will be available to select.
		/// </remarks>
		private bool PruneChild(TreeNode node)
		{
			if (node.Nodes.Count > 0)
			{
				// ToList() is absolutely necessary to keep from changing node collection while looping!
				var unused = node.Nodes.Cast<TreeNode>().Where(PruneChild).ToList();
				foreach (var subTreeNode in unused)
					node.Nodes.Remove(subTreeNode);
			}

			switch (node.Tag)
			{
				case IStText text when !TextsToShow.Contains(text):
					return true;
				case IStText text:
				{
					if (text == m_objList[0])
					{
						m_treeTexts.SelectedNode = node;
					}
					return false;
				}
				// Scripture books have only a dummy child node until they are expanded, so prune books based on the texts they own.
				case IScrBook book when TextsToShow.All(txt => txt.OwnerOfClass<IScrBook>() != book):
					return true;
				case IScrBook book:
				{
					if (m_objList[0].OwnerOfClass<IScrBook>() == book)
					{
						// Expand this book and highlight the selected section
						m_treeTexts.CheckNodeByTag(m_objList[0], TriStateTreeView.CheckState.Checked);
						m_treeTexts.SelectedNode = node.Nodes.Cast<TreeNode>().FirstOrDefault(n => n.Tag == m_objList[0]);
					}
					return false;
				}
				default:
				{
					// Any other Tag is a Genre.
					// Null Tag could mean 'No Genre', Bible, Old or New Testament, or a dummy node that will be replaced when its parent is expanded.
					// Remove Genres, etc., with no texts, but preserve dummy nodes so their parents can be expanded.
					return node.Nodes.Count == 0 && node.Name != TextsTriStateTreeView.ksDummyName;
				}
			}
		}

		/// <summary>
		/// Get/set the label shown above the tree view.
		/// </summary>
		public string TreeViewLabel
		{
			get { return m_treeViewLabel.Text; }
			set { m_treeViewLabel.Text = value; }
		}

		[SuppressMessage("ReSharper", "RedundantNameQualifier", Justification = "Required for designer support")]
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
			// ReSharper disable once PossibleNullReferenceException
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
