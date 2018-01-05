// Copyright (c) 2014-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.FwCoreDlgControls;
using System.Windows.Forms;

namespace LanguageExplorer.DictionaryConfiguration
{
	public partial class DictionaryConfigurationTreeControl : UserControl
	{
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

		private readonly ContextMenuStrip m_CtrlRightClickMenu;

		private readonly ToolTip m_toolTip;

		/// <summary>
		/// Tree of TreeNodes.
		/// </summary>
		public TreeView Tree
		{
			get { return tree; }
		}

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

			m_toolTip = new ToolTip();
			m_toolTip.SetToolTip(moveUp, LanguageExplorerResources.MoveUp);
			m_toolTip.SetToolTip(moveDown, LanguageExplorerResources.MoveDown);
			m_toolTip.SetToolTip(duplicate, DictionaryConfigurationStrings.Duplicate);
			m_toolTip.SetToolTip(remove, DictionaryConfigurationStrings.Delete);
			m_toolTip.SetToolTip(rename, DictionaryConfigurationStrings.EditLabel);
			m_toolTip.SetToolTip(highlight, DictionaryConfigurationStrings.HighlightAffectedContent);

			moveUp.Click += (sender, args) =>
			{
				if (MoveUp != null)
					MoveUp(tree.SelectedNode);
			};

			moveDown.Click += (sender, args) =>
			{
				if (MoveDown != null)
					MoveDown(tree.SelectedNode);
			};

			duplicate.Click += (sender, args) =>
			{
				if (Duplicate != null)
					Duplicate(tree.SelectedNode);
			};

			remove.Click += (sender, args) =>
			{
				if (Remove != null)
					Remove(tree.SelectedNode);
			};

			rename.Click += (sender, args) =>
			{
				if (Rename != null)
					Rename(tree.SelectedNode);
			};

			highlight.Click += (sender, args) =>
			{
				if (Highlight != null)
					Highlight(tree.SelectedNode, highlight, m_toolTip);
			};

			// Create the ContextMenuStrip.
			m_CtrlRightClickMenu = new ContextMenuStrip();
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
			m_CtrlRightClickMenu.Items.AddRange(new ToolStripItem[] { checkAllItem, uncheckAllItem });
			// If the user selects one of the items perform the action and select the node they right clicked on
			m_CtrlRightClickMenu.ItemClicked += (menu, args) =>
			{
				var selectedNode = (TreeNode)m_CtrlRightClickMenu.Tag;
				if(args.ClickedItem.Text == DictionaryConfigurationStrings.ConfigurationTreeControl_SelectAllChildren &&
						CheckAll != null)
				{
					tree.SelectedNode = selectedNode;
					CheckAll(selectedNode);
				}
				if(args.ClickedItem.Text == DictionaryConfigurationStrings.ConfigurationTreeControl_ClearAllChildren &&
						UnCheckAll != null)
				{
					tree.SelectedNode = selectedNode;
					UnCheckAll(selectedNode);
				}
			};
		}

		private void TreeClick(object sender, System.EventArgs e)
		{
			var buttonArgs = e as MouseEventArgs;
			if(buttonArgs == null)
				return;
			if(buttonArgs.Button == MouseButtons.Right && (ModifierKeys & Keys.Control) == Keys.Control)
			{
				// store the node under the mouse click
				var selectedNode = tree.GetNodeAt(buttonArgs.X, buttonArgs.Y);
				if(selectedNode != null)
				{
					// pass the node under the mouse click through the menu item
					m_CtrlRightClickMenu.Tag = selectedNode;
					// show the menu
					m_CtrlRightClickMenu.Show(tree, buttonArgs.Location);
				}
			}
		}
	}
}
