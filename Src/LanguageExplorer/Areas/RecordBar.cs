using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using SIL.Utils;

namespace LanguageExplorer.Areas
{
	/// <summary>
	/// Summary description for RecordBar.
	/// </summary>
	internal sealed class RecordBar : UserControl, IFWDisposable
	{
		/// <summary />
		private TreeView m_treeView;
		/// <summary />
		private ListView m_listView;
		private ColumnHeader m_columnHeader1;
		private Control m_optionalHeaderControl;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		/// <summary>
		/// Constructor
		/// </summary>
		public RecordBar()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			TreeView.HideSelection = false;
			m_listView.HideSelection = false;

			TreeView.Dock = DockStyle.Fill;
			m_listView.Dock = DockStyle.Fill;

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

		/// <summary />
		public TreeView TreeView
		{
			get
			{
				CheckDisposed();

				return m_treeView;
			}
		}

		/// <summary />
		public ListView ListView
		{
			get
			{
				CheckDisposed();

				return m_listView;
			}
		}

		/// <summary />
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

		/// <summary />
		public bool HasHeaderControl { get { return m_optionalHeaderControl != null; } }

		/// <summary />
		public void AddHeaderControl(Control c)
		{
			CheckDisposed();

			if (c == null || HasHeaderControl)
				return;

			m_optionalHeaderControl = c;
			Controls.Add(c);
			c.Dock = DockStyle.Top;
		}

		/// <summary />
		public object SelectedNode
		{
			set
			{
				CheckDisposed();

				TreeView.SelectedNode = (TreeNode) value;
			}
		}

		/// <summary />
		public void Clear()
		{
			CheckDisposed();

			TreeView.Nodes.Clear();
			m_listView.Items.Clear();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Design", "UseCorrectDisposeSignaturesRule",
			Justification = "Has to be protected in sealed class, since the superclass has it be protected.")]
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

		/// <summary />
		public void ShowHeaderControl()
		{
			if (HasHeaderControl)
				m_optionalHeaderControl.Visible = true;
		}

		/// <summary />
		public void HideHeaderControl()
		{
			if (HasHeaderControl)
				m_optionalHeaderControl.Visible = false;
		}
	}
}
