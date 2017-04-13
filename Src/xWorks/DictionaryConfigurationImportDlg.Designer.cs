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
			this.browseButton = new System.Windows.Forms.Button();
			this.mainVerticalFlow = new System.Windows.Forms.FlowLayoutPanel();
			this.fileBrowseHorizFlow = new System.Windows.Forms.FlowLayoutPanel();
			this.fileImportLabel = new System.Windows.Forms.Label();
			this.importPathTextBox = new System.Windows.Forms.TextBox();
			this.overwriteHorizFlow = new System.Windows.Forms.FlowLayoutPanel();
			this.overwriteGroupBox = new System.Windows.Forms.GroupBox();
			this.overwriteOptionFlowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
			this.doOverwriteRadioOption = new System.Windows.Forms.RadioButton();
			this.notOverwriteRadioOption = new System.Windows.Forms.RadioButton();
			this.explanationLabel = new System.Windows.Forms.TextBox();
			this.buttonLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
			this.helpButton = new System.Windows.Forms.Button();
			this.cancelButton = new System.Windows.Forms.Button();
			this.importButton = new System.Windows.Forms.Button();
			this.mainVerticalFlow.SuspendLayout();
			this.fileBrowseHorizFlow.SuspendLayout();
			this.overwriteHorizFlow.SuspendLayout();
			this.overwriteGroupBox.SuspendLayout();
			this.overwriteOptionFlowLayoutPanel.SuspendLayout();
			this.buttonLayoutPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// browseButton
			// 
			resources.ApplyResources(this.browseButton, "browseButton");
			this.browseButton.Name = "browseButton";
			this.browseButton.UseVisualStyleBackColor = true;
			// 
			// mainVerticalFlow
			// 
			resources.ApplyResources(this.mainVerticalFlow, "mainVerticalFlow");
			this.mainVerticalFlow.Controls.Add(this.fileBrowseHorizFlow);
			this.mainVerticalFlow.Controls.Add(this.overwriteHorizFlow);
			this.mainVerticalFlow.Controls.Add(this.explanationLabel);
			this.mainVerticalFlow.Controls.Add(this.buttonLayoutPanel);
			this.mainVerticalFlow.Name = "mainVerticalFlow";
			// 
			// fileBrowseHorizFlow
			// 
			resources.ApplyResources(this.fileBrowseHorizFlow, "fileBrowseHorizFlow");
			this.fileBrowseHorizFlow.Controls.Add(this.fileImportLabel);
			this.fileBrowseHorizFlow.Controls.Add(this.importPathTextBox);
			this.fileBrowseHorizFlow.Controls.Add(this.browseButton);
			this.fileBrowseHorizFlow.Name = "fileBrowseHorizFlow";
			// 
			// fileImportLabel
			// 
			resources.ApplyResources(this.fileImportLabel, "fileImportLabel");
			this.fileImportLabel.Name = "fileImportLabel";
			// 
			// importPathTextBox
			// 
			resources.ApplyResources(this.importPathTextBox, "importPathTextBox");
			this.importPathTextBox.Name = "importPathTextBox";
			// 
			// overwriteHorizFlow
			// 
			resources.ApplyResources(this.overwriteHorizFlow, "overwriteHorizFlow");
			this.overwriteHorizFlow.Controls.Add(this.overwriteGroupBox);
			this.overwriteHorizFlow.Name = "overwriteHorizFlow";
			// 
			// overwriteGroupBox
			// 
			resources.ApplyResources(this.overwriteGroupBox, "overwriteGroupBox");
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
			// explanationLabel
			// 
			resources.ApplyResources(this.explanationLabel, "explanationLabel");
			this.explanationLabel.BackColor = System.Drawing.SystemColors.Control;
			this.explanationLabel.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.explanationLabel.Cursor = System.Windows.Forms.Cursors.Default;
			this.explanationLabel.Name = "explanationLabel";
			this.explanationLabel.ReadOnly = true;
			// 
			// buttonLayoutPanel
			// 
			resources.ApplyResources(this.buttonLayoutPanel, "buttonLayoutPanel");
			this.buttonLayoutPanel.Controls.Add(this.helpButton);
			this.buttonLayoutPanel.Controls.Add(this.cancelButton);
			this.buttonLayoutPanel.Controls.Add(this.importButton);
			this.buttonLayoutPanel.Name = "buttonLayoutPanel";
			// 
			// helpButton
			// 
			resources.ApplyResources(this.helpButton, "helpButton");
			this.helpButton.Name = "helpButton";
			this.helpButton.UseVisualStyleBackColor = true;
			this.helpButton.Click += new System.EventHandler(this.helpButton_Click);
			// 
			// cancelButton
			// 
			resources.ApplyResources(this.cancelButton, "cancelButton");
			this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.cancelButton.Name = "cancelButton";
			this.cancelButton.UseVisualStyleBackColor = true;
			// 
			// importButton
			// 
			resources.ApplyResources(this.importButton, "importButton");
			this.importButton.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.importButton.Name = "importButton";
			this.importButton.UseVisualStyleBackColor = true;
			// 
			// DictionaryConfigurationImportDlg
			// 
			this.AcceptButton = this.importButton;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.cancelButton;
			this.Controls.Add(this.mainVerticalFlow);
			this.Name = "DictionaryConfigurationImportDlg";
			this.ShowIcon = false;
			this.mainVerticalFlow.ResumeLayout(false);
			this.mainVerticalFlow.PerformLayout();
			this.fileBrowseHorizFlow.ResumeLayout(false);
			this.fileBrowseHorizFlow.PerformLayout();
			this.overwriteHorizFlow.ResumeLayout(false);
			this.overwriteGroupBox.ResumeLayout(false);
			this.overwriteGroupBox.PerformLayout();
			this.overwriteOptionFlowLayoutPanel.ResumeLayout(false);
			this.overwriteOptionFlowLayoutPanel.PerformLayout();
			this.buttonLayoutPanel.ResumeLayout(false);
			this.buttonLayoutPanel.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		internal System.Windows.Forms.Button browseButton;
		private System.Windows.Forms.FlowLayoutPanel mainVerticalFlow;
		private System.Windows.Forms.FlowLayoutPanel fileBrowseHorizFlow;
		internal System.Windows.Forms.TextBox importPathTextBox;
		private System.Windows.Forms.FlowLayoutPanel overwriteHorizFlow;
		internal System.Windows.Forms.GroupBox overwriteGroupBox;
		private System.Windows.Forms.FlowLayoutPanel overwriteOptionFlowLayoutPanel;
		internal System.Windows.Forms.RadioButton doOverwriteRadioOption;
		internal System.Windows.Forms.RadioButton notOverwriteRadioOption;
		private System.Windows.Forms.FlowLayoutPanel buttonLayoutPanel;
		internal System.Windows.Forms.Button importButton;
		private System.Windows.Forms.Button cancelButton;
		private System.Windows.Forms.Button helpButton;
		internal System.Windows.Forms.Label fileImportLabel;
		internal System.Windows.Forms.TextBox explanationLabel;
	}
}