// Copyright (c) 2012-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.CoreImpl;

namespace LanguageExplorer.SendReceive
{
	partial class FLExBridgeFirstSendReceiveInstructionsDlg
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FLExBridgeFirstSendReceiveInstructionsDlg));
			this.button_Help = new System.Windows.Forms.Button();
			this.button_Cancel = new System.Windows.Forms.Button();
			this.button_OK = new System.Windows.Forms.Button();
			this.htmlControl_Instructions = new HtmlControl();
			this.SuspendLayout();
			// 
			// button_Help
			// 
			resources.ApplyResources(this.button_Help, "button_Help");
			this.button_Help.Name = "button_Help";
			this.button_Help.UseVisualStyleBackColor = true;
			this.button_Help.Click += new System.EventHandler(this.HelpBtn_Click);
			// 
			// button_Cancel
			// 
			resources.ApplyResources(this.button_Cancel, "button_Cancel");
			this.button_Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.button_Cancel.Name = "button_Cancel";
			this.button_Cancel.UseVisualStyleBackColor = true;
			// 
			// button_OK
			// 
			resources.ApplyResources(this.button_OK, "button_OK");
			this.button_OK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.button_OK.Name = "button_OK";
			this.button_OK.UseVisualStyleBackColor = true;
			// 
			// htmlControl_Instructions
			// Instructions to future editors: *do not* check in if the URL is set to about:blank (or anything else).
			// 
			resources.ApplyResources(this.htmlControl_Instructions, "htmlControl_Instructions");
			this.htmlControl_Instructions.Name = "htmlControl_Instructions";
			// 
			// FLExBridgeFirstSendReceiveInstructionsDlg
			// 
			this.AcceptButton = this.button_Help;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.SystemColors.Window;
			this.CancelButton = this.button_Cancel;
			this.Controls.Add(this.htmlControl_Instructions);
			this.Controls.Add(this.button_OK);
			this.Controls.Add(this.button_Cancel);
			this.Controls.Add(this.button_Help);
			this.Name = "FLExBridgeFirstSendReceiveInstructionsDlg";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button button_Help;
		private System.Windows.Forms.Button button_Cancel;
		private System.Windows.Forms.Button button_OK;
		private HtmlControl htmlControl_Instructions;
	}
}