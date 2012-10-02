using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.Utils;

namespace XCore
{
	/// <summary>
	/// Summary description for RecordBar.
	/// </summary>
	public class RecordBar : UserControl, IFWDisposable
	{
		protected System.Windows.Forms.TreeView m_treeView;
		protected System.Windows.Forms.ListView m_listView;
		private System.Windows.Forms.ColumnHeader columnHeader1;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public RecordBar()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			TreeView.HideSelection = false;
			m_listView.HideSelection = false;

			TreeView.Dock = System.Windows.Forms.DockStyle.Fill;
			m_listView.Dock = System.Windows.Forms.DockStyle.Fill;

			IsFlatList = true;
		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		public System.Windows.Forms.TreeView TreeView
		{
			get
			{
				CheckDisposed();

				return m_treeView;
			}
		}

		public System.Windows.Forms.ListView ListView
		{
			get
			{
				CheckDisposed();

				return m_listView;
			}
		}
		public bool IsFlatList
		{
			set
			{
				CheckDisposed();

				if (value)
				{
					TreeView.Visible = false;
					m_listView.Visible = true;
				}
				else
				{
					TreeView.Visible = true;
					m_listView.Visible = false;
				}
			}
		}

		public object SelectedNode
		{
			set
			{
				CheckDisposed();

				TreeView.SelectedNode = (TreeNode) value;
			}
		}

		public void Clear()
		{
			CheckDisposed();

//			m_treeView.AfterSelect -= new TreeViewEventHandler(OnTreeBarAfterSelect);
			TreeView.Nodes.Clear();
			m_listView.Items.Clear();
//			m_treeView.AfterSelect += new TreeViewEventHandler(OnTreeBarAfterSelect);
		}
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
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
			this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
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
			this.columnHeader1});
			resources.ApplyResources(this.m_listView, "m_listView");
			this.m_listView.HideSelection = false;
			this.m_listView.MultiSelect = false;
			this.m_listView.Name = "m_listView";
			this.m_listView.UseCompatibleStateImageBehavior = false;
			this.m_listView.View = System.Windows.Forms.View.Details;
			//
			// columnHeader1
			//
			resources.ApplyResources(this.columnHeader1, "columnHeader1");
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
	}
}
