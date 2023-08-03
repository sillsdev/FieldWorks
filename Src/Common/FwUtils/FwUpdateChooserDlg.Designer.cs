// Copyright (c) 2021-2021 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.Common.FwUtils
{
	partial class FwUpdateChooserDlg
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
			this.lbChooseVersion = new System.Windows.Forms.ListBox();
			this.btnDownload = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.tbInstructions = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// lbChooseVersion
			// 
			this.lbChooseVersion.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.lbChooseVersion.FormattingEnabled = true;
			this.lbChooseVersion.Location = new System.Drawing.Point(15, 79);
			this.lbChooseVersion.Name = "lbChooseVersion";
			this.lbChooseVersion.Size = new System.Drawing.Size(773, 329);
			this.lbChooseVersion.TabIndex = 1;
			this.lbChooseVersion.DoubleClick += new System.EventHandler(this.lbChooseVersion_DoubleClick);
			// 
			// btnDownload
			// 
			this.btnDownload.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnDownload.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnDownload.Location = new System.Drawing.Point(713, 415);
			this.btnDownload.Name = "btnDownload";
			this.btnDownload.Size = new System.Drawing.Size(75, 23);
			this.btnDownload.TabIndex = 2;
			this.btnDownload.Text = "&Download";
			this.btnDownload.UseVisualStyleBackColor = true;
			// 
			// btnCancel
			// 
			this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(632, 415);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(75, 23);
			this.btnCancel.TabIndex = 3;
			this.btnCancel.Text = "&Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			// 
			// tbInstructions
			// 
			this.tbInstructions.Location = new System.Drawing.Point(15, 12);
			this.tbInstructions.Multiline = true;
			this.tbInstructions.Name = "tbInstructions";
			this.tbInstructions.ReadOnly = true;
			this.tbInstructions.Size = new System.Drawing.Size(773, 61);
			this.tbInstructions.TabIndex = 4;
			this.tbInstructions.Text = "do not localize—only testers see this";
			// 
			// FwUpdateChooserDlg
			// 
			this.AcceptButton = this.btnDownload;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnCancel;
			this.ClientSize = new System.Drawing.Size(800, 450);
			this.Controls.Add(this.tbInstructions);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnDownload);
			this.Controls.Add(this.lbChooseVersion);
			this.Name = "FwUpdateChooserDlg";
			this.ShowIcon = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
			this.Text = "Choose an Update";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
		private System.Windows.Forms.ListBox lbChooseVersion;
		private System.Windows.Forms.Button btnDownload;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.TextBox tbInstructions;
	}
}