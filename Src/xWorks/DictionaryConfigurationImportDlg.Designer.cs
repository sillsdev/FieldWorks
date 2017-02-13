// Copyright (c) 2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.XWorks
{
	partial class DictionaryConfigurationImportDlg
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DictionaryConfigurationImportDlg));
			this.mainTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.explanationLabel = new System.Windows.Forms.TextBox();
			this.importButton = new System.Windows.Forms.Button();
			this.cancelButton = new System.Windows.Forms.Button();
			this.importPathTextBox = new System.Windows.Forms.TextBox();
			this.browseButton = new System.Windows.Forms.Button();
			this.overwriteGroupBox = new System.Windows.Forms.GroupBox();
			this.overwriteOptionFlowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
			this.doOverwriteRadioOption = new System.Windows.Forms.RadioButton();
			this.notOverwriteRadioOption = new System.Windows.Forms.RadioButton();
			this.fileImportLabel = new System.Windows.Forms.TextBox();
			this.mainTableLayoutPanel.SuspendLayout();
			this.overwriteGroupBox.SuspendLayout();
			this.overwriteOptionFlowLayoutPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// mainTableLayoutPanel
			// 
			resources.ApplyResources(this.mainTableLayoutPanel, "mainTableLayoutPanel");
			this.mainTableLayoutPanel.Controls.Add(this.explanationLabel, 0, 1);
			this.mainTableLayoutPanel.Controls.Add(this.importButton, 2, 3);
			this.mainTableLayoutPanel.Controls.Add(this.cancelButton, 1, 3);
			this.mainTableLayoutPanel.Controls.Add(this.importPathTextBox, 1, 0);
			this.mainTableLayoutPanel.Controls.Add(this.browseButton, 2, 0);
			this.mainTableLayoutPanel.Controls.Add(this.overwriteGroupBox, 1, 1);
			this.mainTableLayoutPanel.Controls.Add(this.fileImportLabel, 0, 0);
			this.mainTableLayoutPanel.Name = "mainTableLayoutPanel";
			// 
			// explanationLabel
			// 
			this.explanationLabel.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.mainTableLayoutPanel.SetColumnSpan(this.explanationLabel, 3);
			resources.ApplyResources(this.explanationLabel, "explanationLabel");
			this.explanationLabel.Name = "explanationLabel";
			this.explanationLabel.ReadOnly = true;
			// 
			// importButton
			// 
			resources.ApplyResources(this.importButton, "importButton");
			this.importButton.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.importButton.Name = "importButton";
			this.importButton.UseVisualStyleBackColor = true;
			// 
			// cancelButton
			// 
			resources.ApplyResources(this.cancelButton, "cancelButton");
			this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.cancelButton.Name = "cancelButton";
			this.cancelButton.UseVisualStyleBackColor = true;
			this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
			// 
			// importPathTextBox
			// 
			resources.ApplyResources(this.importPathTextBox, "importPathTextBox");
			this.importPathTextBox.Name = "importPathTextBox";
			// 
			// browseButton
			// 
			resources.ApplyResources(this.browseButton, "browseButton");
			this.browseButton.Name = "browseButton";
			this.browseButton.UseVisualStyleBackColor = true;
			// 
			// overwriteGroupBox
			// 
			resources.ApplyResources(this.overwriteGroupBox, "overwriteGroupBox");
			this.mainTableLayoutPanel.SetColumnSpan(this.overwriteGroupBox, 3);
			this.overwriteGroupBox.Controls.Add(this.overwriteOptionFlowLayoutPanel);
			this.overwriteGroupBox.Name = "overwriteGroupBox";
			this.overwriteGroupBox.TabStop = false;
			// 
			// overwriteOptionFlowLayoutPanel
			// 
			resources.ApplyResources(this.overwriteOptionFlowLayoutPanel, "overwriteOptionFlowLayoutPanel");
			this.overwriteOptionFlowLayoutPanel.Controls.Add(this.doOverwriteRadioOption);
			this.overwriteOptionFlowLayoutPanel.Controls.Add(this.notOverwriteRadioOption);
			this.overwriteOptionFlowLayoutPanel.Name = "overwriteOptionFlowLayoutPanel";
			// 
			// doOverwriteRadioOption
			// 
			resources.ApplyResources(this.doOverwriteRadioOption, "doOverwriteRadioOption");
			this.doOverwriteRadioOption.Name = "doOverwriteRadioOption";
			this.doOverwriteRadioOption.UseVisualStyleBackColor = true;
			// 
			// notOverwriteRadioOption
			// 
			resources.ApplyResources(this.notOverwriteRadioOption, "notOverwriteRadioOption");
			this.notOverwriteRadioOption.Checked = true;
			this.notOverwriteRadioOption.Name = "notOverwriteRadioOption";
			this.notOverwriteRadioOption.TabStop = true;
			this.notOverwriteRadioOption.UseVisualStyleBackColor = true;
			// 
			// fileImportLabel
			// 
			this.fileImportLabel.BorderStyle = System.Windows.Forms.BorderStyle.None;
			resources.ApplyResources(this.fileImportLabel, "fileImportLabel");
			this.fileImportLabel.Name = "fileImportLabel";
			this.fileImportLabel.ReadOnly = true;
			// 
			// DictionaryConfigurationImportDlg
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.mainTableLayoutPanel);
			this.Name = "DictionaryConfigurationImportDlg";
			this.ShowIcon = false;
			this.mainTableLayoutPanel.ResumeLayout(false);
			this.mainTableLayoutPanel.PerformLayout();
			this.overwriteGroupBox.ResumeLayout(false);
			this.overwriteGroupBox.PerformLayout();
			this.overwriteOptionFlowLayoutPanel.ResumeLayout(false);
			this.overwriteOptionFlowLayoutPanel.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel mainTableLayoutPanel;
		private System.Windows.Forms.Button cancelButton;
		internal System.Windows.Forms.TextBox explanationLabel;
		internal System.Windows.Forms.TextBox fileImportLabel;
		internal System.Windows.Forms.TextBox importPathTextBox;
		internal System.Windows.Forms.Button browseButton;
		internal System.Windows.Forms.Button importButton;
		private System.Windows.Forms.FlowLayoutPanel overwriteOptionFlowLayoutPanel;
		internal System.Windows.Forms.RadioButton doOverwriteRadioOption;
		internal System.Windows.Forms.RadioButton notOverwriteRadioOption;
		internal System.Windows.Forms.GroupBox overwriteGroupBox;
	}
}