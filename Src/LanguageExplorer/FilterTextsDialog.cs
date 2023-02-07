// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.Resources;
using SIL.LCModel;

namespace LanguageExplorer
{
	/// <summary>
	/// FilterTextsDialog bundles both ordinary and Scripture texts, when appropriate.
	/// </summary>
	internal sealed class FilterTextsDialog : Form
	{
		#region Data Members
		/// <summary>List of Scripture objects</summary>
		private readonly List<IStText> _objList;
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
		/// that were previously selected for interlinearization.
		/// This pruning must take place at the appropriate time.
		/// If this property is not set, the tree will not be pruned.
		/// </summary>
		public IEnumerable<IStText> TextsToShow { private get; set; }
		private IStText _selectedText;
		#endregion

		#region Constructor/Destructor

		/// <summary />
		private FilterTextsDialog()
		{
			InitializeComponent();
		}

		/// <summary />
		internal FilterTextsDialog(IApp app, LcmCache cache, List<IStText> objList, IHelpTopicProvider helpTopicProvider)
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
		/// Load all texts. Prune if necessary.
		/// </summary>
		private void LoadTexts()
		{
			_treeTexts.LoadAllTexts();
			PruneToTextsToShowIfAny();
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
				foreach (var subTreeNode in node.Nodes.Cast<TreeNode>().Where(PruneChild).ToList())
				{
					node.Nodes.Remove(subTreeNode);
				}
			}
			switch (node.Tag)
			{
				case IStText text when !TextsToShow.Contains(text):
					return true;
				case IStText text:
				{
					if (text == _objList[0])
					{
						_treeTexts.SelectedNode = node;
					}
					return false;
				}
				// Scripture books have only a dummy child node until they are expanded, so prune books based on the texts they own.
				case IScrBook book when TextsToShow.All(txt => txt.OwnerOfClass<IScrBook>() != book):
					return true;
				case IScrBook book:
				{
					if (_objList[0].OwnerOfClass<IScrBook>() == book)
					{
						// Expand this book and highlight the selected section
						_treeTexts.CheckNodeByTag(_objList[0], TriStateTreeViewCheckState.Checked);
						_treeTexts.SelectedNode = node.Nodes.Cast<TreeNode>().FirstOrDefault(n => n.Tag == _objList[0]);
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
		/// If TextsToShow is not null, remove all nodes that aren't in that list.
		/// </summary>
		private void PruneToTextsToShowIfAny()
		{
			if (TextsToShow == null)
			{
				return;
			}
			// ToList() is absolutely necessary to keep from changing node collection while looping!
			foreach (var treeNode in _treeTexts.Nodes.Cast<TreeNode>().Where(PruneChild).ToList())
			{
				_treeTexts.Nodes.Remove(treeNode);
			}
		}

		#region Internal Methods

		/// <summary>
		/// Get/set the label shown above the tree view.
		/// </summary>
		internal string TreeViewLabel
		{
			get => _treeViewLabel.Text;
			set => _treeViewLabel.Text = value;
		}

		/// <summary>
		/// Return an array of all of the included objects of the filter type.
		/// </summary>
		internal IStText[] GetListOfIncludedTexts()
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
			var message = LanguageExplorerResources.kOkbtnEmptySelection;
			var checkedList = _treeTexts.GetCheckedNodeList();
			if (Owner is IFwMainWnd mainWnd && OnlyGenresChecked(checkedList))
			{
				message = LanguageExplorerResources.kOkbtnGenreSelection;
				mainWnd.PropertyTable.SetProperty("RecordList-DelayedGenreAssignment", checkedList, true);
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
			if (MessageBox.Show(message, LanguageExplorerResources.kOkbtnNoTextSelection, MessageBoxButtons.OKCancel) == DialogResult.Cancel)
			{
				DialogResult = DialogResult.None;
			}
		}

		#endregion

		[SuppressMessage("ReSharper", "RedundantNameQualifier", Justification = "Required for designer support")]
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FilterTextsDialog));
			this._treeTexts = new TextsTriStateTreeView();
			this._treeViewLabel = new System.Windows.Forms.Label();
			this._btnOK = new System.Windows.Forms.Button();
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
			this.Controls.Add(this._btnHelp);
			this.Controls.Add(this._treeViewLabel);
			this.Controls.Add(this._treeTexts);
			this.Controls.Add(this._btnOK);
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