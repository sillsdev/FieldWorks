// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Areas
{
	/// <summary />
	internal sealed class RecordBar : UserControl, IRecordBar
	{
		private ColumnHeader m_columnHeader;
		private Control m_optionalHeaderControl;
		private IPropertyTable m_propertyTable;
		private System.ComponentModel.Container components = null;

		/// <summary />
		public RecordBar(IPropertyTable propertyTable)
		{
			InitializeComponent();
			m_propertyTable = propertyTable;
			TreeView.HideSelection = false;
			ListView.HideSelection = false;
			TreeView.Dock = DockStyle.Fill;
			ListView.Dock = DockStyle.Fill;
			IsFlatList = true;
			Clear();
			ListView.SizeChanged += ListView_SizeChanged;
		}

		private void ListView_SizeChanged(object sender, EventArgs e)
		{
			if (m_columnHeader.Width != ListView.Width)
			{
				m_columnHeader.Width = ListView.Width;
			}
		}

		/// <summary>
		/// Get the TreeView control.
		/// </summary>
		public TreeView TreeView { get; private set; }

		/// <summary>
		/// Get the ListView control.
		/// </summary>
		public ListView ListView { get; private set; }

		/// <summary>
		/// Use 'true' to show as a ListView, otherwise 'false' for a TreeView.
		/// </summary>
		public bool IsFlatList
		{
			set
			{
				if (value)
				{
					TreeView.Visible = false;
					ListView.Visible = true;
				}
				else
				{
					TreeView.Visible = true;
					ListView.Visible = false;
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
				TreeView.SelectedNode = value;
			}
		}

		/// <summary>
		/// Clear both views.
		/// </summary>
		public void Clear()
		{
			TreeView.AfterSelect -= OnTreeBarAfterSelect;
			TreeView.Nodes.Clear();
			TreeView.AfterSelect += OnTreeBarAfterSelect;
			ListView.SelectedIndexChanged -= OnListBarSelect;
			ListView.Items.Clear();
			ListView.SelectedIndexChanged += OnListBarSelect;
		}

		private void OnListBarSelect(object sender, EventArgs e)
		{
			m_propertyTable.SetProperty(LanguageExplorerConstants.SelectedListBarNode, ListView.SelectedItems.Count == 0 ? null : ListView.SelectedItems[0], doBroadcastIfChanged: true);
		}

		private void OnTreeBarAfterSelect(object sender, TreeViewEventArgs e)
		{
			m_propertyTable.SetProperty(LanguageExplorerConstants.SelectedTreeBarNode, e.Node, doBroadcastIfChanged: true);
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				m_propertyTable.RemoveProperty(LanguageExplorerConstants.SelectedListBarNode);
				m_propertyTable.RemoveProperty(LanguageExplorerConstants.SelectedTreeBarNode);
				components?.Dispose();
				TreeView?.Dispose();
			}
			TreeView = null;

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
			TreeView = new TreeView();
			ListView = new ListView();
			m_columnHeader = new ColumnHeader();
			SuspendLayout();
			//
			// TreeView tree view
			//
			resources.ApplyResources(this.TreeView, "m_treeView");
			TreeView.Name = "TreeView";
			TreeView.Nodes.AddRange(new TreeNode[] {
			((TreeNode)(resources.GetObject("m_treeView.Nodes"))),
			((TreeNode)(resources.GetObject("m_treeView.Nodes1"))),
			((TreeNode)(resources.GetObject("m_treeView.Nodes2")))});
			//
			// ListView
			//
			ListView.AutoArrange = false;
			ListView.Columns.AddRange(new ColumnHeader[] {
			m_columnHeader});
			resources.ApplyResources(ListView, "m_listView");
			ListView.HideSelection = false;
			ListView.MultiSelect = false;
			ListView.Name = "ListView";
			ListView.UseCompatibleStateImageBehavior = false;
			ListView.View = View.Details;
			//
			// columnHeader1
			//
			resources.ApplyResources(m_columnHeader, "m_columnHeader");
			//
			// RecordBar
			//
			Controls.Add(ListView);
			Controls.Add(TreeView);
			Name = "RecordBar";
			resources.ApplyResources(this, "$this");
			ResumeLayout(false);

		}
		#endregion

		/// <summary>
		/// 'true' to show the optional header control, otherwise 'false' to hide it.
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