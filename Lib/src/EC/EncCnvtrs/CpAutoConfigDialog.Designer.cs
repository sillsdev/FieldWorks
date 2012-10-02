namespace SilEncConverters31
{
	partial class CpAutoConfigDialog
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
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.label1 = new System.Windows.Forms.Label();
			this.comboBoxCodePageList = new System.Windows.Forms.ComboBox();
			this.textBoxCodePageDetails = new System.Windows.Forms.TextBox();
			this.tabControl.SuspendLayout();
			this.tabPageSetup.SuspendLayout();
			this.tableLayoutPanel1.SuspendLayout();
			this.SuspendLayout();
			//
			// tabPageSetup
			//
			this.tabPageSetup.Controls.Add(this.tableLayoutPanel1);
			//
			// buttonSaveInRepository
			//
			this.helpProvider.SetHelpString(this.buttonSaveInRepository, "\r\nClick to add this converter to the system repository permanently.\r\n    ");
			this.helpProvider.SetShowHelp(this.buttonSaveInRepository, true);
			//
			// tableLayoutPanel1
			//
			this.tableLayoutPanel1.ColumnCount = 1;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Controls.Add(this.label1, 0, 1);
			this.tableLayoutPanel1.Controls.Add(this.comboBoxCodePageList, 0, 2);
			this.tableLayoutPanel1.Controls.Add(this.textBoxCodePageDetails, 0, 3);
			this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel1.Location = new System.Drawing.Point(3, 3);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 4;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Size = new System.Drawing.Size(596, 394);
			this.tableLayoutPanel1.TabIndex = 0;
			//
			// label1
			//
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(3, 50);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(165, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "Choose a code page from the list:";
			//
			// comboBoxCodePageList
			//
			this.comboBoxCodePageList.Dock = System.Windows.Forms.DockStyle.Fill;
			this.comboBoxCodePageList.FormattingEnabled = true;
			this.comboBoxCodePageList.Location = new System.Drawing.Point(3, 66);
			this.comboBoxCodePageList.Name = "comboBoxCodePageList";
			this.comboBoxCodePageList.Size = new System.Drawing.Size(590, 21);
			this.comboBoxCodePageList.TabIndex = 1;
			this.comboBoxCodePageList.SelectedIndexChanged += new System.EventHandler(this.comboBoxCodePageList_SelectedIndexChanged);
			//
			// textBoxCodePageDetails
			//
			this.textBoxCodePageDetails.Dock = System.Windows.Forms.DockStyle.Fill;
			this.textBoxCodePageDetails.Location = new System.Drawing.Point(3, 93);
			this.textBoxCodePageDetails.Multiline = true;
			this.textBoxCodePageDetails.Name = "textBoxCodePageDetails";
			this.textBoxCodePageDetails.ReadOnly = true;
			this.textBoxCodePageDetails.Size = new System.Drawing.Size(590, 298);
			this.textBoxCodePageDetails.TabIndex = 2;
			//
			// CpAutoConfigDialog
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.ClientSize = new System.Drawing.Size(634, 479);
			this.Name = "CpAutoConfigDialog";
			this.tabControl.ResumeLayout(false);
			this.tabPageSetup.ResumeLayout(false);
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ComboBox comboBoxCodePageList;
		private System.Windows.Forms.TextBox textBoxCodePageDetails;
	}
}
