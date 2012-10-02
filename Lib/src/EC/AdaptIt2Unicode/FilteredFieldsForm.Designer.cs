namespace AdaptIt2Unicode
{
	partial class FilteredFieldsForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FilteredFieldsForm));
			this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.buttonOK = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.dataGridViewFilterSfms = new System.Windows.Forms.DataGridView();
			this.ColumnConvert = new System.Windows.Forms.DataGridViewCheckBoxColumn();
			this.ColumnSfm = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ColumnData = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ColumnResult = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.helpProvider = new System.Windows.Forms.HelpProvider();
			this.tableLayoutPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.dataGridViewFilterSfms)).BeginInit();
			this.SuspendLayout();
			//
			// tableLayoutPanel
			//
			this.tableLayoutPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.tableLayoutPanel.ColumnCount = 2;
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.tableLayoutPanel.Controls.Add(this.buttonOK, 0, 1);
			this.tableLayoutPanel.Controls.Add(this.buttonCancel, 1, 1);
			this.tableLayoutPanel.Controls.Add(this.dataGridViewFilterSfms, 0, 0);
			this.tableLayoutPanel.Location = new System.Drawing.Point(13, 13);
			this.tableLayoutPanel.Name = "tableLayoutPanel";
			this.tableLayoutPanel.RowCount = 2;
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel.Size = new System.Drawing.Size(522, 241);
			this.tableLayoutPanel.TabIndex = 0;
			//
			// buttonOK
			//
			this.buttonOK.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.helpProvider.SetHelpString(this.buttonOK, "Click to process the filtered fields in the adaptation file with the above settin" +
					"gs");
			this.buttonOK.Location = new System.Drawing.Point(183, 215);
			this.buttonOK.Name = "buttonOK";
			this.helpProvider.SetShowHelp(this.buttonOK, true);
			this.buttonOK.Size = new System.Drawing.Size(75, 23);
			this.buttonOK.TabIndex = 0;
			this.buttonOK.Text = "OK";
			this.buttonOK.UseVisualStyleBackColor = true;
			this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
			//
			// buttonCancel
			//
			this.buttonCancel.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.helpProvider.SetHelpString(this.buttonCancel, "Click to cause none of the filtered fields to be converted");
			this.buttonCancel.Location = new System.Drawing.Point(264, 215);
			this.buttonCancel.Name = "buttonCancel";
			this.helpProvider.SetShowHelp(this.buttonCancel, true);
			this.buttonCancel.Size = new System.Drawing.Size(75, 23);
			this.buttonCancel.TabIndex = 1;
			this.buttonCancel.Text = "Cancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			//
			// dataGridViewFilterSfms
			//
			this.dataGridViewFilterSfms.AllowUserToAddRows = false;
			this.dataGridViewFilterSfms.AllowUserToDeleteRows = false;
			this.dataGridViewFilterSfms.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.dataGridViewFilterSfms.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.dataGridViewFilterSfms.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.dataGridViewFilterSfms.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
			this.ColumnConvert,
			this.ColumnSfm,
			this.ColumnData,
			this.ColumnResult});
			this.tableLayoutPanel.SetColumnSpan(this.dataGridViewFilterSfms, 2);
			this.dataGridViewFilterSfms.Location = new System.Drawing.Point(3, 3);
			this.dataGridViewFilterSfms.Name = "dataGridViewFilterSfms";
			this.dataGridViewFilterSfms.RowHeadersVisible = false;
			this.dataGridViewFilterSfms.Size = new System.Drawing.Size(516, 206);
			this.dataGridViewFilterSfms.TabIndex = 2;
			this.dataGridViewFilterSfms.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridViewFilterSfms_CellClick);
			//
			// ColumnConvert
			//
			this.ColumnConvert.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
			this.ColumnConvert.HeaderText = "Convert";
			this.ColumnConvert.Name = "ColumnConvert";
			this.ColumnConvert.Resizable = System.Windows.Forms.DataGridViewTriState.False;
			this.ColumnConvert.ToolTipText = "Check this box to convert the SFM field with the Source language converter";
			this.ColumnConvert.Width = 50;
			//
			// ColumnSfm
			//
			this.ColumnSfm.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.ColumnSfm.HeaderText = "SFMs";
			this.ColumnSfm.Name = "ColumnSfm";
			this.ColumnSfm.ReadOnly = true;
			this.ColumnSfm.Width = 59;
			//
			// ColumnData
			//
			this.ColumnData.FillWeight = 200F;
			this.ColumnData.HeaderText = "Example Data";
			this.ColumnData.Name = "ColumnData";
			this.ColumnData.ReadOnly = true;
			//
			// ColumnResult
			//
			this.ColumnResult.FillWeight = 200F;
			this.ColumnResult.HeaderText = "Example Result";
			this.ColumnResult.Name = "ColumnResult";
			this.ColumnResult.ReadOnly = true;
			//
			// FilteredFieldsForm
			//
			this.AcceptButton = this.buttonOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size(547, 266);
			this.Controls.Add(this.tableLayoutPanel);
			this.HelpButton = true;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FilteredFieldsForm";
			this.Text = "Conversion Settings for Filtered Fields";
			this.tableLayoutPanel.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.dataGridViewFilterSfms)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.DataGridView dataGridViewFilterSfms;
		private System.Windows.Forms.DataGridViewCheckBoxColumn ColumnConvert;
		private System.Windows.Forms.DataGridViewTextBoxColumn ColumnSfm;
		private System.Windows.Forms.DataGridViewTextBoxColumn ColumnData;
		private System.Windows.Forms.DataGridViewTextBoxColumn ColumnResult;
		private System.Windows.Forms.HelpProvider helpProvider;
	}
}