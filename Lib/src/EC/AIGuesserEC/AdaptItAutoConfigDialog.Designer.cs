namespace SilEncConverters40
{
	partial class AdaptItAutoConfigDialog
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
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.radioButtonLegacy = new System.Windows.Forms.RadioButton();
			this.radioButtonUnicode = new System.Windows.Forms.RadioButton();
			this.label1 = new System.Windows.Forms.Label();
			this.listBoxProjects = new System.Windows.Forms.ListBox();
			this.tabControl.SuspendLayout();
			this.tabPageSetup.SuspendLayout();
			this.tableLayoutPanel1.SuspendLayout();
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			//
			// tabPageSetup
			//
			this.tabPageSetup.Controls.Add(this.tableLayoutPanel1);
			//
			// buttonApply
			//
			this.helpProvider.SetHelpString(this.buttonApply, "Click this button to apply the configured values for this converter");
			this.helpProvider.SetShowHelp(this.buttonApply, true);
			//
			// buttonCancel
			//
			this.helpProvider.SetHelpString(this.buttonCancel, "Click this button to cancel this dialog");
			this.helpProvider.SetShowHelp(this.buttonCancel, true);
			//
			// buttonOK
			//
			this.helpProvider.SetHelpString(this.buttonOK, "Click this button to accept the configured values for this converter");
			this.helpProvider.SetShowHelp(this.buttonOK, true);
			//
			// buttonSaveInRepository
			//
			this.helpProvider.SetHelpString(this.buttonSaveInRepository, "\r\nClick to add this converter to the system repository permanently.\r\n    ");
			this.helpProvider.SetShowHelp(this.buttonSaveInRepository, true);
			//
			// tableLayoutPanel1
			//
			this.tableLayoutPanel1.ColumnCount = 1;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.Controls.Add(this.groupBox1, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.label1, 0, 1);
			this.tableLayoutPanel1.Controls.Add(this.listBoxProjects, 0, 2);
			this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel1.Location = new System.Drawing.Point(3, 3);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 3;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Size = new System.Drawing.Size(596, 394);
			this.tableLayoutPanel1.TabIndex = 0;
			//
			// groupBox1
			//
			this.groupBox1.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.groupBox1.AutoSize = true;
			this.groupBox1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.groupBox1.Controls.Add(this.radioButtonLegacy);
			this.groupBox1.Controls.Add(this.radioButtonUnicode);
			this.groupBox1.Location = new System.Drawing.Point(183, 3);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(231, 57);
			this.groupBox1.TabIndex = 0;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "AdaptIt Version";
			//
			// radioButtonLegacy
			//
			this.radioButtonLegacy.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.radioButtonLegacy.AutoSize = true;
			this.radioButtonLegacy.Location = new System.Drawing.Point(139, 19);
			this.radioButtonLegacy.Name = "radioButtonLegacy";
			this.radioButtonLegacy.Size = new System.Drawing.Size(85, 17);
			this.radioButtonLegacy.TabIndex = 1;
			this.radioButtonLegacy.TabStop = true;
			this.radioButtonLegacy.Text = "Legacy/Ansi";
			this.radioButtonLegacy.UseVisualStyleBackColor = true;
			this.radioButtonLegacy.Click += new System.EventHandler(this.radioButtonLegacy_Click);
			//
			// radioButtonUnicode
			//
			this.radioButtonUnicode.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.radioButtonUnicode.AutoSize = true;
			this.radioButtonUnicode.Location = new System.Drawing.Point(6, 19);
			this.radioButtonUnicode.Name = "radioButtonUnicode";
			this.radioButtonUnicode.Size = new System.Drawing.Size(127, 17);
			this.radioButtonUnicode.TabIndex = 0;
			this.radioButtonUnicode.TabStop = true;
			this.radioButtonUnicode.Text = "Non-Roman/Unicode";
			this.radioButtonUnicode.UseVisualStyleBackColor = true;
			this.radioButtonUnicode.Click += new System.EventHandler(this.radioButtonUnicode_Click);
			//
			// label1
			//
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(3, 63);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(48, 13);
			this.label1.TabIndex = 1;
			this.label1.Text = "Projects:";
			//
			// listBoxProjects
			//
			this.listBoxProjects.Dock = System.Windows.Forms.DockStyle.Fill;
			this.listBoxProjects.FormattingEnabled = true;
			this.listBoxProjects.Location = new System.Drawing.Point(3, 79);
			this.listBoxProjects.Name = "listBoxProjects";
			this.listBoxProjects.Size = new System.Drawing.Size(590, 303);
			this.listBoxProjects.TabIndex = 2;
			this.listBoxProjects.SelectedIndexChanged += new System.EventHandler(this.listBoxProjects_SelectedIndexChanged);
			//
			// AdaptItAutoConfigDialog
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.ClientSize = new System.Drawing.Size(634, 479);
			this.Name = "AdaptItAutoConfigDialog";
			this.Controls.SetChildIndex(this.tabControl, 0);
			this.Controls.SetChildIndex(this.buttonApply, 0);
			this.Controls.SetChildIndex(this.buttonCancel, 0);
			this.Controls.SetChildIndex(this.buttonOK, 0);
			this.Controls.SetChildIndex(this.buttonSaveInRepository, 0);
			this.tabControl.ResumeLayout(false);
			this.tabPageSetup.ResumeLayout(false);
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.RadioButton radioButtonUnicode;
		private System.Windows.Forms.RadioButton radioButtonLegacy;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ListBox listBoxProjects;
	}
}
