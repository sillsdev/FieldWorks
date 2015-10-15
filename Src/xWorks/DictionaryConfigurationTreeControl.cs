// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;

namespace SIL.FieldWorks.XWorks
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
		/// Event when a user askes to CheckAll treenode children
		/// </summary>
		public event TreeNodeEventHandler CheckAll;

		/// <summary>
		/// Event when a user askes to UnCheckAll treenode children
		/// </summary>
		public event TreeNodeEventHandler UnCheckAll;

		private readonly ContextMenuStrip m_CtrlRightClickMenu;

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

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "ToolStripMenuItems disposed in Dispose method")]
		public DictionaryConfigurationTreeControl()
		{
			InitializeComponent();

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

			// Create the ContextMenuStrip.
			m_CtrlRightClickMenu = new ContextMenuStrip();
			// Create the checkall and uncheckall items
			var checkAllItem = new ToolStripMenuItem
			{
				Text = xWorksStrings.ConfigurationTreeControl_SelectAllChildren,
				DisplayStyle = ToolStripItemDisplayStyle.Text
			};
			var uncheckAllItem = new ToolStripMenuItem
			{
				Text = xWorksStrings.ConfigurationTreeControl_ClearAllChildren,
				DisplayStyle = ToolStripItemDisplayStyle.Text
			};
			m_CtrlRightClickMenu.Items.AddRange(new ToolStripItem[] { checkAllItem, uncheckAllItem });
			// If the user selects one of the items perform the action and select the node they right clicked on
			m_CtrlRightClickMenu.ItemClicked += (menu, args) =>
			{
				var selectedNode = (TreeNode)m_CtrlRightClickMenu.Tag;
				if(args.ClickedItem.Text == xWorksStrings.ConfigurationTreeControl_SelectAllChildren &&
						CheckAll != null)
				{
					tree.SelectedNode = selectedNode;
					CheckAll(selectedNode);
				}
				if(args.ClickedItem.Text == xWorksStrings.ConfigurationTreeControl_ClearAllChildren &&
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
