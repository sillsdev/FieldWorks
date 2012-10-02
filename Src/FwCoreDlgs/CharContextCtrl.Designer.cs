namespace SIL.FieldWorks.FwCoreDlgs
{
	partial class CharContextCtrl
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CharContextCtrl));
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
			this.pnlScanForPatterns = new System.Windows.Forms.Panel();
			this.btnScan = new System.Windows.Forms.Button();
			this.lblScanMsg = new System.Windows.Forms.Label();
			this.splitContainer = new System.Windows.Forms.SplitContainer();
			this.pnlTokenGrid = new SIL.FieldWorks.Common.Controls.FwPanel();
			this.pnlLowerGrid = new SIL.FieldWorks.Common.Controls.FwPanel();
			this.gridContext = new SIL.FieldWorks.FwCoreDlgs.ContextGrid();
			this.colRef = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colContextBefore = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colContextItem = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colContextAfter = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.pnlScan = new System.Windows.Forms.Panel();
			this.m_openFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.m_cmnuScan = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.cmnuScanScripture = new System.Windows.Forms.ToolStripMenuItem();
			this.cmnuScanFile = new System.Windows.Forms.ToolStripMenuItem();
			this.splitContainer.Panel1.SuspendLayout();
			this.splitContainer.Panel2.SuspendLayout();
			this.splitContainer.SuspendLayout();
			this.pnlLowerGrid.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.gridContext)).BeginInit();
			this.pnlScan.SuspendLayout();
			this.m_cmnuScan.SuspendLayout();
			this.SuspendLayout();
			//
			// pnlScanForPatterns
			//
			resources.ApplyResources(this.pnlScanForPatterns, "pnlScanForPatterns");
			this.pnlScanForPatterns.Name = "pnlScanForPatterns";
			//
			// btnScan
			//
			resources.ApplyResources(this.btnScan, "btnScan");
			this.btnScan.Image = global::SIL.FieldWorks.FwCoreDlgs.Properties.Resources.ButtonDropDownArrow;
			this.btnScan.Name = "btnScan";
			this.btnScan.UseVisualStyleBackColor = true;
			this.btnScan.Click += new System.EventHandler(this.btnScan_Click);
			//
			// lblScanMsg
			//
			resources.ApplyResources(this.lblScanMsg, "lblScanMsg");
			this.lblScanMsg.AutoEllipsis = true;
			this.lblScanMsg.Name = "lblScanMsg";
			//
			// splitContainer
			//
			this.splitContainer.BackColor = System.Drawing.Color.Transparent;
			resources.ApplyResources(this.splitContainer, "splitContainer");
			this.splitContainer.Name = "splitContainer";
			//
			// splitContainer.Panel1
			//
			this.splitContainer.Panel1.Controls.Add(this.pnlTokenGrid);
			//
			// splitContainer.Panel2
			//
			this.splitContainer.Panel2.Controls.Add(this.pnlLowerGrid);
			this.splitContainer.TabStop = false;
			//
			// pnlTokenGrid
			//
			this.pnlTokenGrid.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.pnlTokenGrid.ClipTextForChildControls = true;
			this.pnlTokenGrid.ControlReceivingFocusOnMnemonic = null;
			resources.ApplyResources(this.pnlTokenGrid, "pnlTokenGrid");
			this.pnlTokenGrid.DoubleBuffered = true;
			this.pnlTokenGrid.MnemonicGeneratesClick = false;
			this.pnlTokenGrid.Name = "pnlTokenGrid";
			this.pnlTokenGrid.PaintExplorerBarBackground = false;
			//
			// pnlLowerGrid
			//
			this.pnlLowerGrid.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.pnlLowerGrid.ClipTextForChildControls = true;
			this.pnlLowerGrid.ControlReceivingFocusOnMnemonic = null;
			this.pnlLowerGrid.Controls.Add(this.gridContext);
			resources.ApplyResources(this.pnlLowerGrid, "pnlLowerGrid");
			this.pnlLowerGrid.DoubleBuffered = true;
			this.pnlLowerGrid.MnemonicGeneratesClick = false;
			this.pnlLowerGrid.Name = "pnlLowerGrid";
			this.pnlLowerGrid.PaintExplorerBarBackground = false;
			//
			// gridContext
			//
			this.gridContext.AllowUserToAddRows = false;
			this.gridContext.AllowUserToDeleteRows = false;
			this.gridContext.AllowUserToResizeColumns = false;
			this.gridContext.AllowUserToResizeRows = false;
			this.gridContext.BackgroundColor = System.Drawing.SystemColors.Window;
			this.gridContext.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.gridContext.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
			this.gridContext.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
			this.gridContext.ColumnHeadersVisible = false;
			this.gridContext.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
			this.colRef,
			this.colContextBefore,
			this.colContextItem,
			this.colContextAfter});
			dataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle5.BackColor = System.Drawing.SystemColors.Window;
			dataGridViewCellStyle5.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle5.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle5.SelectionBackColor = System.Drawing.SystemColors.Window;
			dataGridViewCellStyle5.SelectionForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle5.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.gridContext.DefaultCellStyle = dataGridViewCellStyle5;
			resources.ApplyResources(this.gridContext, "gridContext");
			this.gridContext.GridColor = System.Drawing.SystemColors.GrayText;
			this.gridContext.MultiSelect = false;
			this.gridContext.Name = "gridContext";
			this.gridContext.ReadOnly = true;
			this.gridContext.RowHeadersVisible = false;
			this.gridContext.ShowCellToolTips = false;
			this.gridContext.StandardTab = true;
			this.gridContext.VirtualMode = true;
			this.gridContext.ClientSizeChanged += new System.EventHandler(this.gridContext_ClientSizeChanged);
			this.gridContext.RowHeightInfoNeeded += new System.Windows.Forms.DataGridViewRowHeightInfoNeededEventHandler(this.HandleRowHeightInfoNeeded);
			this.gridContext.CellValueNeeded += new System.Windows.Forms.DataGridViewCellValueEventHandler(this.gridContext_CellValueNeeded);
			this.gridContext.CellPainting += new System.Windows.Forms.DataGridViewCellPaintingEventHandler(this.gridContext_CellPainting);
			//
			// colRef
			//
			this.colRef.DataPropertyName = "Reference";
			dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			this.colRef.DefaultCellStyle = dataGridViewCellStyle1;
			this.colRef.HeaderText = global::SIL.FieldWorks.FwCoreDlgs.FwCoreDlgs.kstidOpen;
			this.colRef.Name = "colRef";
			this.colRef.ReadOnly = true;
			resources.ApplyResources(this.colRef, "colRef");
			//
			// colContextBefore
			//
			this.colContextBefore.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			this.colContextBefore.DataPropertyName = "Before";
			dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
			this.colContextBefore.DefaultCellStyle = dataGridViewCellStyle2;
			this.colContextBefore.HeaderText = global::SIL.FieldWorks.FwCoreDlgs.FwCoreDlgs.kstidOpen;
			this.colContextBefore.Name = "colContextBefore";
			this.colContextBefore.ReadOnly = true;
			//
			// colContextItem
			//
			this.colContextItem.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			this.colContextItem.DataPropertyName = "Character";
			dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
			dataGridViewCellStyle3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
			this.colContextItem.DefaultCellStyle = dataGridViewCellStyle3;
			this.colContextItem.HeaderText = global::SIL.FieldWorks.FwCoreDlgs.FwCoreDlgs.kstidOpen;
			this.colContextItem.Name = "colContextItem";
			this.colContextItem.ReadOnly = true;
			resources.ApplyResources(this.colContextItem, "colContextItem");
			//
			// colContextAfter
			//
			this.colContextAfter.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			this.colContextAfter.DataPropertyName = "After";
			dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			this.colContextAfter.DefaultCellStyle = dataGridViewCellStyle4;
			this.colContextAfter.HeaderText = global::SIL.FieldWorks.FwCoreDlgs.FwCoreDlgs.kstidOpen;
			this.colContextAfter.Name = "colContextAfter";
			this.colContextAfter.ReadOnly = true;
			resources.ApplyResources(this.colContextAfter, "colContextAfter");
			//
			// pnlScan
			//
			this.pnlScan.BackColor = System.Drawing.Color.Transparent;
			this.pnlScan.Controls.Add(this.btnScan);
			this.pnlScan.Controls.Add(this.lblScanMsg);
			resources.ApplyResources(this.pnlScan, "pnlScan");
			this.pnlScan.Name = "pnlScan";
			//
			// m_openFileDialog
			//
			this.m_openFileDialog.DefaultExt = "lds";
			resources.ApplyResources(this.m_openFileDialog, "m_openFileDialog");
			//
			// m_cmnuScan
			//
			this.m_cmnuScan.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.cmnuScanScripture,
			this.cmnuScanFile});
			this.m_cmnuScan.Name = "m_cmnuScan";
			this.m_cmnuScan.ShowImageMargin = false;
			this.m_cmnuScan.ShowItemToolTips = false;
			resources.ApplyResources(this.m_cmnuScan, "m_cmnuScan");
			//
			// cmnuScanScripture
			//
			this.cmnuScanScripture.Name = "cmnuScanScripture";
			resources.ApplyResources(this.cmnuScanScripture, "cmnuScanScripture");
			this.cmnuScanScripture.Click += new System.EventHandler(this.cmnuScanScripture_Click);
			//
			// cmnuScanFile
			//
			this.cmnuScanFile.Name = "cmnuScanFile";
			resources.ApplyResources(this.cmnuScanFile, "cmnuScanFile");
			this.cmnuScanFile.Click += new System.EventHandler(this.cmnuScanFile_Click);
			//
			// CharContextCtrl
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.splitContainer);
			this.Controls.Add(this.pnlScan);
			this.Name = "CharContextCtrl";
			this.splitContainer.Panel1.ResumeLayout(false);
			this.splitContainer.Panel2.ResumeLayout(false);
			this.splitContainer.ResumeLayout(false);
			this.pnlLowerGrid.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.gridContext)).EndInit();
			this.pnlScan.ResumeLayout(false);
			this.m_cmnuScan.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Panel pnlScanForPatterns;
		private System.Windows.Forms.Button btnScan;
		private System.Windows.Forms.Label lblScanMsg;
		private System.Windows.Forms.SplitContainer splitContainer;
		private SIL.FieldWorks.Common.Controls.FwPanel pnlTokenGrid;
		private SIL.FieldWorks.Common.Controls.FwPanel pnlLowerGrid;
		private ContextGrid gridContext;
		private System.Windows.Forms.Panel pnlScan;
		private System.Windows.Forms.ContextMenuStrip m_cmnuScan;
		private System.Windows.Forms.ToolStripMenuItem cmnuScanScripture;
		private System.Windows.Forms.ToolStripMenuItem cmnuScanFile;
		private System.Windows.Forms.DataGridViewTextBoxColumn colRef;
		private System.Windows.Forms.DataGridViewTextBoxColumn colContextBefore;
		private System.Windows.Forms.DataGridViewTextBoxColumn colContextItem;
		private System.Windows.Forms.DataGridViewTextBoxColumn colContextAfter;
		private System.Windows.Forms.OpenFileDialog m_openFileDialog;
	}
}
