// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
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
		/// Tree of TreeNodes.
		/// </summary>
		public TreeView Tree
		{
			get { return tree; }
		}

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
		}
	}
}
