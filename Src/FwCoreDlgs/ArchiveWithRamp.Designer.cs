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
			this.m_frame = new System.Windows.Forms.GroupBox();
			this.m_whichBackup = new System.Windows.Forms.Panel();
			this.m_lblMostRecentBackup = new System.Windows.Forms.Label();
			this.m_rbExistingBackup = new System.Windows.Forms.RadioButton();
			this.m_rbNewBackup = new System.Windows.Forms.RadioButton();
			this.m_fieldWorksBackup = new System.Windows.Forms.CheckBox();
			this.m_frame.SuspendLayout();
			this.m_whichBackup.SuspendLayout();
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
			this.m_help.Click += new System.EventHandler(this.m_help_Click);
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
			// m_frame
			// 
			this.m_frame.Controls.Add(this.m_whichBackup);
			this.m_frame.Controls.Add(this.m_fieldWorksBackup);
			this.m_frame.Location = new System.Drawing.Point(12, 12);
			this.m_frame.Name = "m_frame";
			this.m_frame.Size = new System.Drawing.Size(329, 265);
			this.m_frame.TabIndex = 13;
			this.m_frame.TabStop = false;
			this.m_frame.Text = "Items to Archive";
			// 
			// m_whichBackup
			// 
			this.m_whichBackup.Controls.Add(this.m_lblMostRecentBackup);
			this.m_whichBackup.Controls.Add(this.m_rbExistingBackup);
			this.m_whichBackup.Controls.Add(this.m_rbNewBackup);
			this.m_whichBackup.Location = new System.Drawing.Point(30, 42);
			this.m_whichBackup.Name = "m_whichBackup";
			this.m_whichBackup.Size = new System.Drawing.Size(293, 73);
			this.m_whichBackup.TabIndex = 1;
			// 
			// m_lblMostRecentBackup
			// 
			this.m_lblMostRecentBackup.AutoSize = true;
			this.m_lblMostRecentBackup.Location = new System.Drawing.Point(32, 50);
			this.m_lblMostRecentBackup.Name = "m_lblMostRecentBackup";
			this.m_lblMostRecentBackup.Size = new System.Drawing.Size(69, 13);
			this.m_lblMostRecentBackup.TabIndex = 2;
			this.m_lblMostRecentBackup.Text = "(None found)";
			// 
			// m_rbExistingBackup
			// 
			this.m_rbExistingBackup.AutoSize = true;
			this.m_rbExistingBackup.Location = new System.Drawing.Point(4, 27);
			this.m_rbExistingBackup.Name = "m_rbExistingBackup";
			this.m_rbExistingBackup.Size = new System.Drawing.Size(157, 17);
			this.m_rbExistingBackup.TabIndex = 1;
			this.m_rbExistingBackup.Text = "Use most recent backup file";
			this.m_rbExistingBackup.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.m_rbExistingBackup.UseVisualStyleBackColor = true;
			// 
			// m_rbNewBackup
			// 
			this.m_rbNewBackup.AutoSize = true;
			this.m_rbNewBackup.Checked = true;
			this.m_rbNewBackup.Location = new System.Drawing.Point(4, 4);
			this.m_rbNewBackup.Name = "m_rbNewBackup";
			this.m_rbNewBackup.Size = new System.Drawing.Size(143, 17);
			this.m_rbNewBackup.TabIndex = 0;
			this.m_rbNewBackup.TabStop = true;
			this.m_rbNewBackup.Text = "Create a new backup file";
			this.m_rbNewBackup.UseVisualStyleBackColor = true;
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
			// ArchiveWithRamp
			// 
			this.AcceptButton = this.m_archive;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_cancel;
			this.ClientSize = new System.Drawing.Size(353, 333);
			this.Controls.Add(this.m_frame);
			this.Controls.Add(this.m_help);
			this.Controls.Add(this.m_cancel);
			this.Controls.Add(this.m_archive);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ArchiveWithRamp";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Archive With RAMP";
			this.m_frame.ResumeLayout(false);
			this.m_frame.PerformLayout();
			this.m_whichBackup.ResumeLayout(false);
			this.m_whichBackup.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button m_help;
		private System.Windows.Forms.Button m_cancel;
		private System.Windows.Forms.Button m_archive;
		private System.Windows.Forms.GroupBox m_frame;
		private System.Windows.Forms.CheckBox m_fieldWorksBackup;
		private System.Windows.Forms.Panel m_whichBackup;
		private System.Windows.Forms.RadioButton m_rbExistingBackup;
		private System.Windows.Forms.RadioButton m_rbNewBackup;
		private System.Windows.Forms.Label m_lblMostRecentBackup;
	}
}