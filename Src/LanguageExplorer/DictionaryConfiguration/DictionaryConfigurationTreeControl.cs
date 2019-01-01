// Copyright (c) 2014-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using LanguageExplorer.Controls;

namespace LanguageExplorer.DictionaryConfiguration
{
	public partial class DictionaryConfigurationTreeControl : UserControl
	{
		private readonly ContextMenuStrip _ctrlRightClickMenu;

		/// <summary>
		/// For events occurring on TreeNodes in the TreeView in this control.
		/// </summary>
		public delegate void TreeNodeEventHandler(TreeNode treeNode);

		/// <summary>
		/// Event when a TreeNode is requested to be moved up.
		/// </summary>
		public event TreeNodeEventHandler MoveUp;

		/// <summary>
		/// Event when a TreeNode is requested to be moved down.
		/// </summary>
		public event TreeNodeEventHandler MoveDown;

		/// <summary>
		/// Event when a TreeNode is requested to be duplicated.
		/// </summary>
		public event TreeNodeEventHandler Duplicate;

		/// <summary>
		/// Event when a TreeNode is requested to be removed.
		/// </summary>
		public event TreeNodeEventHandler Remove;

		/// <summary>
		/// Event when a TreeNode is requested to be renamed.
		/// </summary>
		public event TreeNodeEventHandler Rename;

		/// <summary>
		/// For events affecting highlighting in the preview pane.
		/// </summary>
		public delegate void HighlightEventHandler(TreeNode treeNode, Button button, ToolTip toolTip);
		/// <summary>
		/// Event when a TreeNode's corresponding content is requested to be highlighted in the preview pane.
		/// </summary>
		public event HighlightEventHandler Highlight;

		/// <summary>
		/// Event when a user askes to CheckAll treenode children
		/// </summary>
		public event TreeNodeEventHandler CheckAll;

		/// <summary>
		/// Event when a user askes to UnCheckAll treenode children
		/// </summary>
		public event TreeNodeEventHandler UnCheckAll;

		/// <summary>
		/// Tree of TreeNodes.
		/// </summary>
		public TreeView Tree => tree;

		/// <summary>
		/// Set whether button is enabled.
		/// </summary>
		public bool MoveUpEnabled { set { moveUp.Enabled = value; } }

		/// <summary>
		/// Set whether button is enabled.
		/// </summary>
		public bool MoveDownEnabled { set { moveDown.Enabled = value; } }

		/// <summary>
		/// Set whether button is enabled.
		/// </summary>
		public bool DuplicateEnabled { set { duplicate.Enabled = value; } }

		/// <summary>
		/// Set whether button is enabled.
		/// </summary>
		public bool RemoveEnabled { set { remove.Enabled = value; } }

		/// <summary>
		/// Set whether button is enabled.
		/// </summary>
		public bool RenameEnabled { set { rename.Enabled = value; } }

		/// <summary>
		/// Set whether button is enabled.
		/// </summary>
		public bool HighlightEnabled { set { highlight.Enabled = value; } }

		public DictionaryConfigurationTreeControl()
		{
			InitializeComponent();

			var toolTip = new ToolTip();
			toolTip.SetToolTip(moveUp, LanguageExplorerResources.MoveUp);
			toolTip.SetToolTip(moveDown, LanguageExplorerResources.MoveDown);
			toolTip.SetToolTip(duplicate, DictionaryConfigurationStrings.Duplicate);
			toolTip.SetToolTip(remove, DictionaryConfigurationStrings.Delete);
			toolTip.SetToolTip(rename, DictionaryConfigurationStrings.EditLabel);
			toolTip.SetToolTip(highlight, DictionaryConfigurationStrings.HighlightAffectedContent);
			moveUp.Click += (sender, args) =>
			{
				MoveUp?.Invoke(tree.SelectedNode);
			};
			moveDown.Click += (sender, args) =>
			{
				MoveDown?.Invoke(tree.SelectedNode);
			};
			duplicate.Click += (sender, args) =>
			{
				Duplicate?.Invoke(tree.SelectedNode);
			};
			remove.Click += (sender, args) =>
			{
				Remove?.Invoke(tree.SelectedNode);
			};
			rename.Click += (sender, args) =>
			{
				Rename?.Invoke(tree.SelectedNode);
			};
			highlight.Click += (sender, args) =>
			{
				Highlight?.Invoke(tree.SelectedNode, highlight, toolTip);
			};
			// Create the ContextMenuStrip.
			_ctrlRightClickMenu = new ContextMenuStrip();
			// Create the checkall and uncheckall items
			var checkAllItem = new DisposableToolStripMenuItem
			{
				Text = DictionaryConfigurationStrings.ConfigurationTreeControl_SelectAllChildren,
				DisplayStyle = ToolStripItemDisplayStyle.Text
			};
			var uncheckAllItem = new DisposableToolStripMenuItem
			{
				Text = DictionaryConfigurationStrings.ConfigurationTreeControl_ClearAllChildren,
				DisplayStyle = ToolStripItemDisplayStyle.Text
			};
			_ctrlRightClickMenu.Items.AddRange(new ToolStripItem[] { checkAllItem, uncheckAllItem });
			// If the user selects one of the items perform the action and select the node they right clicked on
			_ctrlRightClickMenu.ItemClicked += (menu, args) =>
			{
				var selectedNode = (TreeNode)_ctrlRightClickMenu.Tag;
				if (args.ClickedItem.Text == DictionaryConfigurationStrings.ConfigurationTreeControl_SelectAllChildren && CheckAll != null)
				{
					tree.SelectedNode = selectedNode;
					CheckAll(selectedNode);
				}
				if (args.ClickedItem.Text == DictionaryConfigurationStrings.ConfigurationTreeControl_ClearAllChildren && UnCheckAll != null)
				{
					tree.SelectedNode = selectedNode;
					UnCheckAll(selectedNode);
				}
			};
		}

		private void TreeClick(object sender, System.EventArgs e)
		{
			var buttonArgs = e as MouseEventArgs;
			if (buttonArgs?.Button != MouseButtons.Right || (ModifierKeys & Keys.Control) != Keys.Control)
			{
				return;
			}
			// store the node under the mouse click
			var selectedNode = tree.GetNodeAt(buttonArgs.X, buttonArgs.Y);
			if (selectedNode != null)
			{
				// pass the node under the mouse click through the menu item
				_ctrlRightClickMenu.Tag = selectedNode;
				// show the menu
				_ctrlRightClickMenu.Show(tree, buttonArgs.Location);
			}
		}
	}
}