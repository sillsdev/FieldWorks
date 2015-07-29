// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.XWorks
{
	partial class DictionaryConfigurationNodeRenameDlg
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DictionaryConfigurationNodeRenameDlg));
			this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.description = new System.Windows.Forms.Label();
			this.ok = new System.Windows.Forms.Button();
			this.cancel = new System.Windows.Forms.Button();
			this.newSuffix = new System.Windows.Forms.TextBox();
			this.tableLayoutPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// tableLayoutPanel
			// 
			resources.ApplyResources(this.tableLayoutPanel, "tableLayoutPanel");
			this.tableLayoutPanel.Controls.Add(this.description, 0, 0);
			this.tableLayoutPanel.Controls.Add(this.ok, 0, 2);
			this.tableLayoutPanel.Controls.Add(this.cancel, 1, 2);
			this.tableLayoutPanel.Controls.Add(this.newSuffix, 0, 1);
			this.tableLayoutPanel.Name = "tableLayoutPanel";
			// 
			// description
			// 
			resources.ApplyResources(this.description, "description");
			this.tableLayoutPanel.SetColumnSpan(this.description, 2);
			this.description.Name = "description";
			// 
			// ok
			// 
			this.ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			resources.ApplyResources(this.ok, "ok");
			this.ok.Name = "ok";
			this.ok.UseVisualStyleBackColor = true;
			// 
			// cancel
			// 
			this.cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(this.cancel, "cancel");
			this.cancel.Name = "cancel";
			this.cancel.UseVisualStyleBackColor = true;
			// 
			// newSuffix
			// 
			this.tableLayoutPanel.SetColumnSpan(this.newSuffix, 2);
			resources.ApplyResources(this.newSuffix, "newSuffix");
			this.newSuffix.Name = "newSuffix";
			// 
			// DictionaryConfigurationNodeRenameDlg
			// 
			this.AcceptButton = this.ok;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.cancel;
			this.Controls.Add(this.tableLayoutPanel);
			this.Name = "DictionaryConfigurationNodeRenameDlg";
			this.tableLayoutPanel.ResumeLayout(false);
			this.tableLayoutPanel.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
		private System.Windows.Forms.Label description;
		private System.Windows.Forms.Button ok;
		private System.Windows.Forms.Button cancel;
		private System.Windows.Forms.TextBox newSuffix;
	}
}
