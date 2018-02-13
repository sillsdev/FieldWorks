// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Areas
{
	/// <summary>
	/// Summary description for RecordBar.
	/// </summary>
	internal sealed class RecordBar : UserControl, IRecordBar
	{
		/// <summary />
		private TreeView m_treeView;
		/// <summary />
		private ListView m_listView;
		private ColumnHeader m_columnHeader1;
		private Control m_optionalHeaderControl;
		private IPropertyTable m_propertyTable;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		/// <summary />
		public RecordBar(IPropertyTable propertyTable)
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			m_propertyTable = propertyTable;

			m_treeView.HideSelection = false;
			m_listView.HideSelection = false;

			m_treeView.Dock = DockStyle.Fill;
			m_listView.Dock = DockStyle.Fill;

			IsFlatList = true;

			Clear();
		}

		/// <summary>
		/// Get the TreeView control.
		/// </summary>
		public TreeView TreeView => m_treeView;

		/// <summary>
		/// Get the ListView control.
		/// </summary>
		public ListView ListView => m_listView;

		/// <summary>
		/// Use 'true' to show as a ListView, otherwise 'false' for a TreeView.
		/// </summary>
		public bool IsFlatList
		{
			set
			{
				if (value)
				{
					m_treeView.Visible = false;
					m_listView.Visible = true;
				}
				else
				{
					m_treeView.Visible = true;
					m_listView.Visible = false;
				}
			}
		}

		/// <summary>
		/// 'true' if the control has the optional header control, otherwise 'false'.
		/// </summary>
		public bool HasHeaderControl => m_optionalHeaderControl != null;

		/// <summary>
		/// Add an optional header control
		/// </summary>
		/// <param name="c">An optional header control.</param>
		public void AddHeaderControl(Control c)
		{
			if (c == null || HasHeaderControl)
			{
				return;
			}

			m_optionalHeaderControl = c;
			Controls.Add(c);
			c.Dock = DockStyle.Top;
		}

		/// <summary>
		/// Select the given TreeNode (when showing the TreeView).
		/// </summary>
		public TreeNode SelectedNode
		{
			set
			{
				m_treeView.SelectedNode = value;
			}
		}

		/// <summary>
		/// Clear both views.
		/// </summary>
		public void Clear()
		{
			m_treeView.AfterSelect -= OnTreeBarAfterSelect;
			m_treeView.Nodes.Clear();
			m_treeView.AfterSelect += OnTreeBarAfterSelect;

			m_listView.SelectedIndexChanged -= OnListBarSelect;
			m_listView.Items.Clear();
			m_listView.SelectedIndexChanged += OnListBarSelect;
		}

		private void OnListBarSelect(object sender, EventArgs e)
		{
			m_propertyTable.SetProperty("SelectedListBarNode", ListView.SelectedItems.Count == 0 ? null : ListView.SelectedItems[0], false, true);
		}

		private void OnTreeBarAfterSelect(object sender, TreeViewEventArgs e)
		{
			m_propertyTable.SetProperty("SelectedTreeBarNode", e.Node, false, true);
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			if (disposing)
			{
				components?.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RecordBar));
			this.m_treeView = new System.Windows.Forms.TreeView();
			this.m_listView = new System.Windows.Forms.ListView();
			this.m_columnHeader1 = new System.Windows.Forms.ColumnHeader();
			this.SuspendLayout();
			//
			// m_treeView
			//
			resources.ApplyResources(this.m_treeView, "m_treeView");
			this.m_treeView.Name = "m_treeView";
			this.m_treeView.Nodes.AddRange(new System.Windows.Forms.TreeNode[] {
			((System.Windows.Forms.TreeNode)(resources.GetObject("m_treeView.Nodes"))),
			((System.Windows.Forms.TreeNode)(resources.GetObject("m_treeView.Nodes1"))),
			((System.Windows.Forms.TreeNode)(resources.GetObject("m_treeView.Nodes2")))});
			//
			// m_listView
			//
			this.m_listView.AutoArrange = false;
			this.m_listView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
			this.m_columnHeader1});
			resources.ApplyResources(this.m_listView, "m_listView");
			this.m_listView.HideSelection = false;
			this.m_listView.MultiSelect = false;
			this.m_listView.Name = "m_listView";
			this.m_listView.UseCompatibleStateImageBehavior = false;
			this.m_listView.View = System.Windows.Forms.View.Details;
			//
			// columnHeader1
			//
			resources.ApplyResources(this.m_columnHeader1, "m_columnHeader1");
			//
			// RecordBar
			//
			this.Controls.Add(this.m_listView);
			this.Controls.Add(this.m_treeView);
			this.Name = "RecordBar";
			resources.ApplyResources(this, "$this");
			this.ResumeLayout(false);

		}
		#endregion

		/// <summary>
		/// 'true' to show the optional header control, otherwsie 'false' to hide it.
		/// </summary>
		/// <remarks>Has no affect, if there is no header control.</remarks>
		public bool ShowHeaderControl
		{
			set
			{
				if (HasHeaderControl)
				{
					m_optionalHeaderControl.Visible = value;
				}
			}
		}
	}
}