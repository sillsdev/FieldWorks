// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;

namespace SIL.FieldWorks.FwCoreDlgs
{
	partial class WarningNotUsingDefaultLinkedFilesLocation
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
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + " ******************");
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WarningNotUsingDefaultLinkedFilesLocation));
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.btn_help = new System.Windows.Forms.Button();
			this.btn_cancel = new System.Windows.Forms.Button();
			this.btn_OK = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			this.SuspendLayout();
			// 
			// label1
			// 
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			// 
			// label2
			// 
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			// 
			// pictureBox1
			// 
			this.pictureBox1.Image = global::SIL.FieldWorks.FwCoreDlgs.Properties.Resources.Warning;
			resources.ApplyResources(this.pictureBox1, "pictureBox1");
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.TabStop = false;
			// 
			// btn_help
			// 
			resources.ApplyResources(this.btn_help, "btn_help");
			this.btn_help.Name = "btn_help";
			this.btn_help.UseVisualStyleBackColor = true;
			this.btn_help.Click += new System.EventHandler(this.btn_help_Click);
			// 
			// btn_cancel
			// 
			resources.ApplyResources(this.btn_cancel, "btn_cancel");
			this.btn_cancel.DialogResult = System.Windows.Forms.DialogResult.No;
			this.btn_cancel.Name = "btn_cancel";
			this.btn_cancel.UseVisualStyleBackColor = true;
			// 
			// btn_OK
			// 
			this.btn_OK.AllowDrop = true;
			resources.ApplyResources(this.btn_OK, "btn_OK");
			this.btn_OK.DialogResult = System.Windows.Forms.DialogResult.Yes;
			this.btn_OK.Name = "btn_OK";
			this.btn_OK.UseVisualStyleBackColor = true;
			// 
			// WarningNotUsingDefaultLinkedFilesLocation
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ControlBox = false;
			this.Controls.Add(this.btn_help);
			this.Controls.Add(this.btn_cancel);
			this.Controls.Add(this.btn_OK);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.pictureBox1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "WarningNotUsingDefaultLinkedFilesLocation";
			this.ShowIcon = false;
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.PictureBox pictureBox1;
		private System.Windows.Forms.Button btn_help;
		private System.Windows.Forms.Button btn_cancel;
		private System.Windows.Forms.Button btn_OK;
	}
}