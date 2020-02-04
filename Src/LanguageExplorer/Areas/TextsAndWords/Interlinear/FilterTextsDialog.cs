// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using SIL.Code;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.LCModel;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// FilterTextsDialog bundles both ordinary and Scripture texts, when appropriate.
	/// </summary>
	public class FilterTextsDialog : Form
	{
		#region Data Members
		/// <summary>List of Scripture objects</summary>
		private readonly IStText[] _objList;
		/// <summary>LCM cache</summary>
		private readonly LcmCache _cache;
		/// <summary>Help Provider</summary>
		private HelpProvider _helpProvider;
		private readonly IHelpTopicProvider _helpTopicProvider;
		/// <summary>Help Topic Id</summary>
		private readonly string _helpTopicId;
		/// <summary>The text tree with scripture, genres and unassigned texts</summary>
		private TextsTriStateTreeView _treeTexts;
		/// <summary>Label for the tree view.</summary>
		private Label _treeViewLabel;
		/// <remarks>protected because of testing</remarks>
		private Button _btnOK;
		private Button _btnCancel;
		private Button _btnHelp;
		private IContainer components;
		/// <summary>
		/// If the dialog is being used for exporting multiple texts at a time,
		/// then the tree must be pruned to show only those texts (and scripture books)
		/// that were previously selected for interlinearization. The following
		/// three variables allow this pruning to take place at the appropriate time.
		/// The m_selectedText variable indicates which text should be initially checked,
		/// as per LT-12177.
		/// </summary>
		private IEnumerable<IStText> _textsToShow;
		private IStText _selectedText;
		#endregion

		#region Constructor/Destructor

		/// <summary />
		protected FilterTextsDialog()
		{
			InitializeComponent();
		}

		/// <summary />
		public FilterTextsDialog(IApp app, LcmCache cache, IStText[] objList, IHelpTopicProvider helpTopicProvider)
			: this()
		{
			Guard.AgainstNull(app, nameof(app));
			Guard.AgainstNull(cache, nameof(cache));
			Guard.AgainstNull(objList, nameof(objList));
			Guard.AgainstNull(helpTopicProvider, nameof(helpTopicProvider));

			_treeTexts.App = app;
			_treeTexts.Cache = cache;
			_cache = cache;
			_objList = objList;
			_helpTopicProvider = helpTopicProvider;
			_treeTexts.AfterCheck += OnCheckedChanged;
			AccessibleName = "FilterTextsDialog";
			_helpTopicId = "khtpChooseTexts";
		}
		#endregion

		#region Overrides

		/// <summary/>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + ". ******");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				components?.Dispose();
			}
			base.Dispose(disposing);
		}

		/// <summary>
		/// Load settings for the dialog
		/// </summary>
		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);

			// Add all of the Scripture book names to the book list
			_treeTexts.BeginUpdate();
			LoadTexts();
			_treeTexts.ExpandToBooks();
			_treeTexts.EndUpdate();
			if (_btnOK == null || _objList == null)
			{
				return;
			}
			var prevSeqCount = _cache.ActionHandlerAccessor.UndoableSequenceCount;
			foreach (var obj in _objList)
			{
				_treeTexts.CheckNodeByTag(obj, TriStateTreeViewCheckState.Checked);
			}
			if (prevSeqCount != _cache.ActionHandlerAccessor.UndoableSequenceCount)
			{
				// Selecting node(s) changed something, so save it so that the UI doesn't become
				// unresponsive
				using (var progressDlg = new ProgressDialogWithTask(this))
				{
					progressDlg.IsIndeterminate = true;
					progressDlg.Title = Text;
					progressDlg.Message = ResourceHelper.GetResourceString("kstidSavingChanges");
					progressDlg.RunTask((progDlg, parms) =>
					{
						_cache.ServiceLocator.GetInstance<IUndoStackManager>().Save();
						return null;
					});
				}
			}
			UpdateButtonState();
		}

		#endregion

		/// <summary>
		/// Load all texts.
		/// </summary>
		private void LoadTexts()
		{
			_treeTexts.LoadAllTexts();
		}

		/// <summary>
		/// controls the logic for enabling/disabling buttons on this dialog.
		/// </summary>
		private void UpdateButtonState()
		{
			_btnOK.Enabled = true;
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
					if (!_textsToShow.Contains(node.Tag as IStText))
					{
						return true;
					}
					if (node.Tag == _selectedText)
					{
						_treeTexts.SelectedNode = node;
						_treeTexts.SetChecked(node, TriStateTreeViewCheckState.Checked);
					}
					else
					{
						_treeTexts.SetChecked(node, TriStateTreeViewCheckState.Unchecked);
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

		#region Public Methods
		/// <summary>
		/// Remove all nodes that aren't in our list of interestingTexts from the tree (m_textsToShow).
		/// Initially select the one specified (m_selectedText).
		/// </summary>
		/// <param name="interestingTexts">The list of texts to display in the dialog.</param>
		/// <param name="selectedText">The text that should be initially checked in the dialog.</param>
		public void PruneToInterestingTextsAndSelect(IEnumerable<IStText> interestingTexts, IStText selectedText)
		{
			_textsToShow = interestingTexts;
			_selectedText = selectedText;
			// ToList() is absolutely necessary to keep from changing node collection while looping!
			var unusedNodes = _treeTexts.Nodes.Cast<TreeNode>().Where(PruneChild).ToList();
			foreach (var treeNode in unusedNodes)
			{
				_treeTexts.Nodes.Remove(treeNode);
			}
		}

		/// <summary>
		/// Get/set the label shown above the tree view.
		/// </summary>
		public string TreeViewLabel
		{
			get { return _treeViewLabel.Text; }
			set { _treeViewLabel.Text = value; }
		}

		/// <summary>
		/// Return an array of all of the included objects of the filter type.
		/// </summary>
		public IStText[] GetListOfIncludedTexts()
		{
			return _treeTexts.GetCheckedTagData().OfType<IStText>().ToArray();
		}
		#endregion

		#region Event Handlers

		/// <summary>
		/// Open the help window when the help button is pressed.
		/// </summary>
		private void _btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(_helpTopicProvider, _helpTopicId);
		}

		/// <summary>
		/// Called after the box is checked or unchecked
		/// </summary>
		private void OnCheckedChanged(object sender, TreeViewEventArgs e)
		{
			UpdateButtonState();
		}

		/// <summary>
		/// OK event handler. Checks the text list and warns about situations
		/// where no texts are selected.
		/// </summary>
		private void OnOk(object obj, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			var showWarning = false;
			var message = ITextStrings.kOkbtnEmptySelection;
			var checkedList = _treeTexts.GetCheckedNodeList();
			var own = Owner as IFwMainWnd;
			if (own != null && OnlyGenresChecked(checkedList))
			{
				message = ITextStrings.kOkbtnGenreSelection;
				own.PropertyTable.SetProperty("RecordList-DelayedGenreAssignment", checkedList, true);
				showWarning = true;
			}
			if (_treeTexts.GetNodesWithState(TriStateTreeViewCheckState.Checked).Length == 0)
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

		#endregion

		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FilterTextsDialog));
			this._treeViewLabel = new System.Windows.Forms.Label();
			this._btnOK = new System.Windows.Forms.Button();
			this._treeTexts = new LanguageExplorer.Areas.TextsAndWords.Interlinear.TextsTriStateTreeView();
			this._helpProvider = new System.Windows.Forms.HelpProvider();
			this._btnCancel = new System.Windows.Forms.Button();
			this._btnHelp = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// _treeViewLabel
			//
			resources.ApplyResources(this._treeViewLabel, "_treeViewLabel");
			this._treeViewLabel.Name = "_treeViewLabel";
			this._helpProvider.SetShowHelp(this._treeViewLabel, ((bool)(resources.GetObject("_treeViewLabel.ShowHelp"))));
			//
			// _btnOK
			//
			this._btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			resources.ApplyResources(this._btnOK, "_btnOK");
			this._btnOK.Name = "_btnOK";
			this._helpProvider.SetShowHelp(this._btnOK, ((bool)(resources.GetObject("_btnOK.ShowHelp"))));
			this._btnHelp.Click += new System.EventHandler(this.OnOk);
			//
			// _treeTexts
			//
			resources.ApplyResources(this._treeTexts, "_treeTexts");
			this._treeTexts.ItemHeight = 16;
			this._treeTexts.Name = "_treeTexts";
			this._helpProvider.SetShowHelp(this._treeTexts, ((bool)(resources.GetObject("_treeTexts.ShowHelp"))));
			//
			// _btnCancel
			//
			this._btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(this._btnCancel, "_btnCancel");
			this._btnCancel.Name = "_btnCancel";
			this._helpProvider.SetShowHelp(this._btnCancel, ((bool)(resources.GetObject("_btnCancel.ShowHelp"))));
			//
			// _btnHelp
			//
			resources.ApplyResources(this._btnHelp, "_btnHelp");
			this._btnHelp.Name = "_btnHelp";
			this._helpProvider.SetShowHelp(this._btnHelp, ((bool)(resources.GetObject("_btnHelp.ShowHelp"))));
			this._btnHelp.Click += new System.EventHandler(this._btnHelp_Click);
			//
			// FilterTextsDialog
			//
			this.AcceptButton = this._btnOK;
			this.CancelButton = this._btnCancel;
			resources.ApplyResources(this, "$this");
			this.Controls.Add(this._treeTexts);
			this.Controls.Add(this._treeViewLabel);
			this.Controls.Add(this._btnOK);
			this.Controls.Add(this._btnHelp);
			this.Controls.Add(this._btnCancel);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FilterTextsDialog";
			this._helpProvider.SetShowHelp(this, ((bool)(resources.GetObject("$this.ShowHelp"))));
			this.ResumeLayout(false);

		}
	}
}