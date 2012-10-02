namespace SILConvertersWordML
{
	partial class FilesToOpenListDlg
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FilesToOpenListDlg));
			this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.buttonOK = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.dataGridViewFilesList = new System.Windows.Forms.DataGridView();
			this.ColumnSelect = new System.Windows.Forms.DataGridViewCheckBoxColumn();
			this.ColumnFilename = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.tableLayoutPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.dataGridViewFilesList)).BeginInit();
			this.SuspendLayout();
			//
			// tableLayoutPanel
			//
			this.tableLayoutPanel.ColumnCount = 2;
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.tableLayoutPanel.Controls.Add(this.buttonOK, 0, 1);
			this.tableLayoutPanel.Controls.Add(this.buttonCancel, 1, 1);
			this.tableLayoutPanel.Controls.Add(this.dataGridViewFilesList, 0, 0);
			this.tableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel.Location = new System.Drawing.Point(0, 0);
			this.tableLayoutPanel.Name = "tableLayoutPanel";
			this.tableLayoutPanel.RowCount = 2;
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel.Size = new System.Drawing.Size(421, 367);
			this.tableLayoutPanel.TabIndex = 0;
			//
			// buttonOK
			//
			this.buttonOK.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.buttonOK.Location = new System.Drawing.Point(132, 341);
			this.buttonOK.Name = "buttonOK";
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
			this.buttonCancel.Location = new System.Drawing.Point(213, 341);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(75, 23);
			this.buttonCancel.TabIndex = 1;
			this.buttonCancel.Text = "Cancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
			//
			// dataGridViewFilesList
			//
			this.dataGridViewFilesList.AllowUserToAddRows = false;
			this.dataGridViewFilesList.AllowUserToDeleteRows = false;
			this.dataGridViewFilesList.AllowUserToResizeRows = false;
			this.dataGridViewFilesList.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.dataGridViewFilesList.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
			this.ColumnSelect,
			this.ColumnFilename});
			this.tableLayoutPanel.SetColumnSpan(this.dataGridViewFilesList, 2);
			this.dataGridViewFilesList.Dock = System.Windows.Forms.DockStyle.Fill;
			this.dataGridViewFilesList.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
			this.dataGridViewFilesList.Location = new System.Drawing.Point(3, 3);
			this.dataGridViewFilesList.Name = "dataGridViewFilesList";
			this.dataGridViewFilesList.Size = new System.Drawing.Size(415, 332);
			this.dataGridViewFilesList.TabIndex = 2;
			//
			// ColumnSelect
			//
			this.ColumnSelect.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
			this.ColumnSelect.HeaderText = "Open";
			this.ColumnSelect.Name = "ColumnSelect";
			this.ColumnSelect.Width = 39;
			//
			// ColumnFilename
			//
			this.ColumnFilename.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
			this.ColumnFilename.HeaderText = "Filename";
			this.ColumnFilename.Name = "ColumnFilename";
			this.ColumnFilename.ReadOnly = true;
			this.ColumnFilename.Width = 74;
			//
			// FilesToOpenListDlg
			//
			this.AcceptButton = this.buttonOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size(421, 367);
			this.Controls.Add(this.tableLayoutPanel);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FilesToOpenListDlg";
			this.Text = "Select Files to Open";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FilesToOpenListDlg_FormClosing);
			this.tableLayoutPanel.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.dataGridViewFilesList)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.DataGridView dataGridViewFilesList;
		private System.Windows.Forms.DataGridViewCheckBoxColumn ColumnSelect;
		private System.Windows.Forms.DataGridViewTextBoxColumn ColumnFilename;
	}
}