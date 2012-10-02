namespace SilEncConverters31
{
	partial class CmpdAutoConfigDialog
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

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.labelCompoundConverterName = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.dataGridViewSteps = new System.Windows.Forms.DataGridView();
			this.ConverterStep = new System.Windows.Forms.DataGridViewButtonColumn();
			this.DirectionReverse = new System.Windows.Forms.DataGridViewCheckBoxColumn();
			this.Normalization = new System.Windows.Forms.DataGridViewComboBoxColumn();
			this.tabControl.SuspendLayout();
			this.tabPageSetup.SuspendLayout();
			this.tableLayoutPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.dataGridViewSteps)).BeginInit();
			this.SuspendLayout();
			//
			// tabPageSetup
			//
			this.tabPageSetup.Controls.Add(this.tableLayoutPanel);
			//
			// buttonSaveInRepository
			//
			this.helpProvider.SetHelpString(this.buttonSaveInRepository, "\r\nClick to add this converter to the system repository permanently.\r\n    ");
			this.helpProvider.SetShowHelp(this.buttonSaveInRepository, true);
			//
			// tableLayoutPanel
			//
			this.tableLayoutPanel.ColumnCount = 2;
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel.Controls.Add(this.labelCompoundConverterName, 1, 1);
			this.tableLayoutPanel.Controls.Add(this.label1, 0, 1);
			this.tableLayoutPanel.Controls.Add(this.dataGridViewSteps, 0, 3);
			this.tableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel.Location = new System.Drawing.Point(3, 3);
			this.tableLayoutPanel.Name = "tableLayoutPanel";
			this.tableLayoutPanel.RowCount = 4;
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel.Size = new System.Drawing.Size(596, 394);
			this.tableLayoutPanel.TabIndex = 0;
			//
			// labelCompoundConverterName
			//
			this.labelCompoundConverterName.AutoSize = true;
			this.labelCompoundConverterName.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.labelCompoundConverterName.Dock = System.Windows.Forms.DockStyle.Fill;
			this.helpProvider.SetHelpString(this.labelCompoundConverterName, "This box shows the name that the compound converter goes by");
			this.labelCompoundConverterName.Location = new System.Drawing.Point(147, 50);
			this.labelCompoundConverterName.Name = "labelCompoundConverterName";
			this.helpProvider.SetShowHelp(this.labelCompoundConverterName, true);
			this.labelCompoundConverterName.Size = new System.Drawing.Size(446, 15);
			this.labelCompoundConverterName.TabIndex = 8;
			//
			// label1
			//
			this.label1.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(3, 51);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(138, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "Compound converter name:";
			//
			// dataGridViewSteps
			//
			this.dataGridViewSteps.AllowUserToResizeColumns = false;
			this.dataGridViewSteps.AllowUserToResizeRows = false;
			this.dataGridViewSteps.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.dataGridViewSteps.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
			this.ConverterStep,
			this.DirectionReverse,
			this.Normalization});
			this.tableLayoutPanel.SetColumnSpan(this.dataGridViewSteps, 2);
			this.dataGridViewSteps.Dock = System.Windows.Forms.DockStyle.Fill;
			this.dataGridViewSteps.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
			this.dataGridViewSteps.Location = new System.Drawing.Point(3, 118);
			this.dataGridViewSteps.MultiSelect = false;
			this.dataGridViewSteps.Name = "dataGridViewSteps";
			this.dataGridViewSteps.RowHeadersWidth = 25;
			this.dataGridViewSteps.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.dataGridViewSteps.ShowEditingIcon = false;
			this.dataGridViewSteps.Size = new System.Drawing.Size(590, 273);
			this.dataGridViewSteps.TabIndex = 10;
			this.dataGridViewSteps.CellMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dataGridViewSteps_CellMouseClick);
			this.dataGridViewSteps.SelectionChanged += new System.EventHandler(this.dataGridViewSteps_SelectionChanged);
			//
			// ConverterStep
			//
			this.ConverterStep.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
			this.ConverterStep.HeaderText = "Converter Step Name";
			this.ConverterStep.MinimumWidth = 200;
			this.ConverterStep.Name = "ConverterStep";
			this.ConverterStep.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
			this.ConverterStep.Text = "Click to Add Step";
			this.ConverterStep.ToolTipText = "Click this cell to bring up the Converter Selection Dialog for this step";
			this.ConverterStep.Width = 200;
			//
			// DirectionReverse
			//
			this.DirectionReverse.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
			this.DirectionReverse.HeaderText = "Reverse?";
			this.DirectionReverse.Name = "DirectionReverse";
			this.DirectionReverse.Resizable = System.Windows.Forms.DataGridViewTriState.False;
			this.DirectionReverse.ToolTipText = "Check this box to use this step in reverse";
			this.DirectionReverse.Width = 59;
			//
			// Normalization
			//
			this.Normalization.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			this.Normalization.HeaderText = "Normalization?";
			this.Normalization.Items.AddRange(new object[] {
			"None",
			"Fully Composed",
			"Fully Decomposed"});
			this.Normalization.MaxDropDownItems = 3;
			this.Normalization.MinimumWidth = 80;
			this.Normalization.Name = "Normalization";
			this.Normalization.ToolTipText = "Select the Normalization form for the output of this step (only valid for Unicode" +
				" encoding)";
			this.Normalization.Width = 120;
			//
			// CmpdAutoConfigDialog
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.ClientSize = new System.Drawing.Size(634, 479);
			this.Name = "CmpdAutoConfigDialog";
			this.Controls.SetChildIndex(this.tabControl, 0);
			this.Controls.SetChildIndex(this.buttonApply, 0);
			this.Controls.SetChildIndex(this.buttonCancel, 0);
			this.Controls.SetChildIndex(this.buttonOK, 0);
			this.Controls.SetChildIndex(this.buttonSaveInRepository, 0);
			this.tabControl.ResumeLayout(false);
			this.tabPageSetup.ResumeLayout(false);
			this.tableLayoutPanel.ResumeLayout(false);
			this.tableLayoutPanel.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.dataGridViewSteps)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label labelCompoundConverterName;
		private System.Windows.Forms.DataGridView dataGridViewSteps;
		private System.Windows.Forms.DataGridViewButtonColumn ConverterStep;
		private System.Windows.Forms.DataGridViewCheckBoxColumn DirectionReverse;
		private System.Windows.Forms.DataGridViewComboBoxColumn Normalization;
	}
}
