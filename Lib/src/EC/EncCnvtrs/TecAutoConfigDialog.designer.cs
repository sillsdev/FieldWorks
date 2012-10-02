namespace SilEncConverters40
{
	partial class TecAutoConfigDialog
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
			this.labelTECkitFile = new System.Windows.Forms.Label();
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.textBoxFileSpec = new System.Windows.Forms.TextBox();
			this.buttonBrowse = new System.Windows.Forms.Button();
			this.openFileDialogBrowse = new System.Windows.Forms.OpenFileDialog();
			this.tabControl.SuspendLayout();
			this.tabPageSetup.SuspendLayout();
			this.tableLayoutPanel1.SuspendLayout();
			this.SuspendLayout();
			//
			// tabPageSetup
			//
			this.tabPageSetup.Controls.Add(this.tableLayoutPanel1);
			//
			// labelTECkitFile
			//
			this.labelTECkitFile.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.labelTECkitFile.AutoSize = true;
			this.labelTECkitFile.Location = new System.Drawing.Point(3, 8);
			this.labelTECkitFile.Name = "labelTECkitFile";
			this.labelTECkitFile.Size = new System.Drawing.Size(58, 13);
			this.labelTECkitFile.TabIndex = 0;
			this.labelTECkitFile.Text = "TECkit file:";
			//
			// tableLayoutPanel1
			//
			this.tableLayoutPanel1.ColumnCount = 3;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.Controls.Add(this.labelTECkitFile, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.textBoxFileSpec, 1, 0);
			this.tableLayoutPanel1.Controls.Add(this.buttonBrowse, 2, 0);
			this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel1.Location = new System.Drawing.Point(3, 3);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 2;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Size = new System.Drawing.Size(596, 394);
			this.tableLayoutPanel1.TabIndex = 1;
			//
			// textBoxFileSpec
			//
			this.textBoxFileSpec.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.textBoxFileSpec.Location = new System.Drawing.Point(67, 4);
			this.textBoxFileSpec.Name = "textBoxFileSpec";
			this.textBoxFileSpec.Size = new System.Drawing.Size(496, 20);
			this.textBoxFileSpec.TabIndex = 1;
			this.textBoxFileSpec.TextChanged += new System.EventHandler(this.textBoxFileSpec_TextChanged);
			//
			// buttonBrowse
			//
			this.buttonBrowse.Dock = System.Windows.Forms.DockStyle.Fill;
			this.buttonBrowse.Location = new System.Drawing.Point(569, 3);
			this.buttonBrowse.Name = "buttonBrowse";
			this.buttonBrowse.Size = new System.Drawing.Size(24, 23);
			this.buttonBrowse.TabIndex = 2;
			this.buttonBrowse.Text = "...";
			this.buttonBrowse.UseVisualStyleBackColor = true;
			this.buttonBrowse.Click += new System.EventHandler(this.buttonBrowse_Click);
			//
			// openFileDialogBrowse
			//
			this.openFileDialogBrowse.DefaultExt = "tec";
			this.openFileDialogBrowse.Filter = "TECkit (compiled) files (*.tec)|*.tec|TECkit (compilable) files (*.map)|*.map";
			this.openFileDialogBrowse.Title = "Browse for TECkit map";
			//
			// TecAutoConfigDialog
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.ClientSize = new System.Drawing.Size(634, 479);
			this.Name = "TecAutoConfigDialog";
			this.tabControl.ResumeLayout(false);
			this.tabPageSetup.ResumeLayout(false);
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Label labelTECkitFile;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.TextBox textBoxFileSpec;
		private System.Windows.Forms.Button buttonBrowse;
		private System.Windows.Forms.OpenFileDialog openFileDialogBrowse;
	}
}
