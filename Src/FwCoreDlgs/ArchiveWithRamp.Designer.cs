namespace SIL.FieldWorks.FwCoreDlgs
{
	partial class ArchiveWithRamp
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
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
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
			this.m_help = new System.Windows.Forms.Button();
			this.m_cancel = new System.Windows.Forms.Button();
			this.m_archive = new System.Windows.Forms.Button();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.panel1 = new System.Windows.Forms.Panel();
			this._rbExistingBackup = new System.Windows.Forms.RadioButton();
			this._rbNewBackup = new System.Windows.Forms.RadioButton();
			this.m_fieldWorksBackup = new System.Windows.Forms.CheckBox();
			this._lblMostRecentBackup = new System.Windows.Forms.Label();
			this.groupBox1.SuspendLayout();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_help
			// 
			this.m_help.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_help.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.m_help.Location = new System.Drawing.Point(266, 298);
			this.m_help.Name = "m_help";
			this.m_help.Size = new System.Drawing.Size(75, 23);
			this.m_help.TabIndex = 12;
			this.m_help.Text = "Help";
			this.m_help.UseVisualStyleBackColor = true;
			// 
			// m_cancel
			// 
			this.m_cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_cancel.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.m_cancel.Location = new System.Drawing.Point(184, 298);
			this.m_cancel.Name = "m_cancel";
			this.m_cancel.Size = new System.Drawing.Size(75, 23);
			this.m_cancel.TabIndex = 11;
			this.m_cancel.Text = "Cancel";
			this.m_cancel.UseVisualStyleBackColor = true;
			// 
			// m_archive
			// 
			this.m_archive.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_archive.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_archive.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.m_archive.Location = new System.Drawing.Point(103, 298);
			this.m_archive.Name = "m_archive";
			this.m_archive.Size = new System.Drawing.Size(75, 23);
			this.m_archive.TabIndex = 10;
			this.m_archive.Text = "OK";
			this.m_archive.UseVisualStyleBackColor = true;
			this.m_archive.Click += new System.EventHandler(this.m_archive_Click);
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.panel1);
			this.groupBox1.Controls.Add(this.m_fieldWorksBackup);
			this.groupBox1.Location = new System.Drawing.Point(12, 12);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(329, 265);
			this.groupBox1.TabIndex = 13;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Items to Archive";
			// 
			// panel1
			// 
			this.panel1.Controls.Add(this._lblMostRecentBackup);
			this.panel1.Controls.Add(this._rbExistingBackup);
			this.panel1.Controls.Add(this._rbNewBackup);
			this.panel1.Location = new System.Drawing.Point(30, 42);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(293, 73);
			this.panel1.TabIndex = 1;
			// 
			// _rbExistingBackup
			// 
			this._rbExistingBackup.AutoSize = true;
			this._rbExistingBackup.Location = new System.Drawing.Point(4, 27);
			this._rbExistingBackup.Name = "_rbExistingBackup";
			this._rbExistingBackup.Size = new System.Drawing.Size(141, 17);
			this._rbExistingBackup.TabIndex = 1;
			this._rbExistingBackup.Text = "Use most recent backup";
			this._rbExistingBackup.UseVisualStyleBackColor = true;
			// 
			// _rbNewBackup
			// 
			this._rbNewBackup.AutoSize = true;
			this._rbNewBackup.Checked = true;
			this._rbNewBackup.Location = new System.Drawing.Point(4, 4);
			this._rbNewBackup.Name = "_rbNewBackup";
			this._rbNewBackup.Size = new System.Drawing.Size(127, 17);
			this._rbNewBackup.TabIndex = 0;
			this._rbNewBackup.TabStop = true;
			this._rbNewBackup.Text = "Create a new backup";
			this._rbNewBackup.UseVisualStyleBackColor = true;
			// 
			// m_fieldWorksBackup
			// 
			this.m_fieldWorksBackup.AutoSize = true;
			this.m_fieldWorksBackup.Checked = true;
			this.m_fieldWorksBackup.CheckState = System.Windows.Forms.CheckState.Checked;
			this.m_fieldWorksBackup.Enabled = false;
			this.m_fieldWorksBackup.ForeColor = System.Drawing.SystemColors.ControlText;
			this.m_fieldWorksBackup.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.m_fieldWorksBackup.Location = new System.Drawing.Point(15, 21);
			this.m_fieldWorksBackup.Name = "m_fieldWorksBackup";
			this.m_fieldWorksBackup.Size = new System.Drawing.Size(134, 17);
			this.m_fieldWorksBackup.TabIndex = 0;
			this.m_fieldWorksBackup.Text = "FieldWorks backup file";
			this.m_fieldWorksBackup.UseVisualStyleBackColor = true;
			// 
			// _lblMostRecentBackup
			// 
			this._lblMostRecentBackup.AutoSize = true;
			this._lblMostRecentBackup.Location = new System.Drawing.Point(32, 50);
			this._lblMostRecentBackup.Name = "_lblMostRecentBackup";
			this._lblMostRecentBackup.Size = new System.Drawing.Size(69, 13);
			this._lblMostRecentBackup.TabIndex = 2;
			this._lblMostRecentBackup.Text = "(None found)";
			// 
			// ArchiveWithRamp
			// 
			this.AcceptButton = this.m_archive;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_cancel;
			this.ClientSize = new System.Drawing.Size(353, 333);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.m_help);
			this.Controls.Add(this.m_cancel);
			this.Controls.Add(this.m_archive);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ArchiveWithRamp";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Archive With RAMP";
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button m_help;
		private System.Windows.Forms.Button m_cancel;
		private System.Windows.Forms.Button m_archive;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.CheckBox m_fieldWorksBackup;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.RadioButton _rbExistingBackup;
		private System.Windows.Forms.RadioButton _rbNewBackup;
		private System.Windows.Forms.Label _lblMostRecentBackup;
	}
}