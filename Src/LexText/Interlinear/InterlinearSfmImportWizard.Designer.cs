// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics.CodeAnalysis;

namespace SIL.FieldWorks.IText
{
	partial class InterlinearSfmImportWizard
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
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification = "TODO-Linux: LinkLabel.TabStop is missing from Mono")]
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(InterlinearSfmImportWizard));
			this.tabPage1 = new System.Windows.Forms.TabPage();
			this.m_useDefaultSettingsLink = new System.Windows.Forms.LinkLabel();
			this.label6 = new System.Windows.Forms.Label();
			this.m_browseLoadSettingsFileButon = new System.Windows.Forms.Button();
			this.m_loadSettingsFileBox = new System.Windows.Forms.TextBox();
			this.label5 = new System.Windows.Forms.Label();
			this.m_fileListBox = new System.Windows.Forms.TextBox();
			this.m_browseInputFilesButton = new System.Windows.Forms.Button();
			this.label4 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.tabPage2 = new System.Windows.Forms.TabPage();
			this.m_modifyMappingButton = new System.Windows.Forms.Button();
			this.label7 = new System.Windows.Forms.Label();
			this.m_mappingsList = new System.Windows.Forms.ListView();
			this.Marker = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.Counts = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.Destination = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.WritingSystem = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.Converter = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.tabPage4 = new System.Windows.Forms.TabPage();
			this.numberOfTextsLabel = new System.Windows.Forms.Label();
			this.secretShiftText = new System.Windows.Forms.Label();
			this.label8 = new System.Windows.Forms.Label();
			this.m_browseSaveSettingsFileButon = new System.Windows.Forms.Button();
			this.m_saveSettingsFileBox = new System.Windows.Forms.TextBox();
			this.label11 = new System.Windows.Forms.Label();
			this.label10 = new System.Windows.Forms.Label();
			this.label9 = new System.Windows.Forms.Label();
			this.tabSteps.SuspendLayout();
			this.tabPage1.SuspendLayout();
			this.tabPage2.SuspendLayout();
			this.tabPage4.SuspendLayout();
			this.SuspendLayout();
			//
			// panSteps
			//
			resources.ApplyResources(this.panSteps, "panSteps");
			//
			// lblSteps
			//
			resources.ApplyResources(this.lblSteps, "lblSteps");
			//
			// m_btnBack
			//
			resources.ApplyResources(this.m_btnBack, "m_btnBack");
			//
			// m_btnCancel
			//
			resources.ApplyResources(this.m_btnCancel, "m_btnCancel");
			this.m_btnCancel.Click += new System.EventHandler(this.m_btnCancel_Click);
			//
			// m_btnNext
			//
			resources.ApplyResources(this.m_btnNext, "m_btnNext");
			//
			// m_btnHelp
			//
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			this.m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// tabSteps
			//
			this.tabSteps.Controls.Add(this.tabPage1);
			this.tabSteps.Controls.Add(this.tabPage2);
			this.tabSteps.Controls.Add(this.tabPage4);
			resources.ApplyResources(this.tabSteps, "tabSteps");
			//
			// tabPage1
			//
			this.tabPage1.Controls.Add(this.m_useDefaultSettingsLink);
			this.tabPage1.Controls.Add(this.label6);
			this.tabPage1.Controls.Add(this.m_browseLoadSettingsFileButon);
			this.tabPage1.Controls.Add(this.m_loadSettingsFileBox);
			this.tabPage1.Controls.Add(this.label5);
			this.tabPage1.Controls.Add(this.m_fileListBox);
			this.tabPage1.Controls.Add(this.m_browseInputFilesButton);
			this.tabPage1.Controls.Add(this.label4);
			this.tabPage1.Controls.Add(this.label3);
			this.tabPage1.Controls.Add(this.label2);
			this.tabPage1.Controls.Add(this.label1);
			resources.ApplyResources(this.tabPage1, "tabPage1");
			this.tabPage1.Name = "tabPage1";
			this.tabPage1.UseVisualStyleBackColor = true;
			//
			// m_useDefaultSettingsLink
			//
			resources.ApplyResources(this.m_useDefaultSettingsLink, "m_useDefaultSettingsLink");
			this.m_useDefaultSettingsLink.Name = "m_useDefaultSettingsLink";
			this.m_useDefaultSettingsLink.TabStop = true;
			this.m_useDefaultSettingsLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.m_useDefaultSettingsLink_LinkClicked);
			//
			// label6
			//
			resources.ApplyResources(this.label6, "label6");
			this.label6.Name = "label6";
			//
			// m_browseLoadSettingsFileButon
			//
			resources.ApplyResources(this.m_browseLoadSettingsFileButon, "m_browseLoadSettingsFileButon");
			this.m_browseLoadSettingsFileButon.Name = "m_browseLoadSettingsFileButon";
			this.m_browseLoadSettingsFileButon.UseVisualStyleBackColor = true;
			this.m_browseLoadSettingsFileButon.Click += new System.EventHandler(this.m_browseLoadSettingsFileButon_Click);
			//
			// m_loadSettingsFileBox
			//
			resources.ApplyResources(this.m_loadSettingsFileBox, "m_loadSettingsFileBox");
			this.m_loadSettingsFileBox.Name = "m_loadSettingsFileBox";
			//
			// label5
			//
			resources.ApplyResources(this.label5, "label5");
			this.label5.Name = "label5";
			//
			// m_fileListBox
			//
			resources.ApplyResources(this.m_fileListBox, "m_fileListBox");
			this.m_fileListBox.Name = "m_fileListBox";
			//
			// m_browseInputFilesButton
			//
			resources.ApplyResources(this.m_browseInputFilesButton, "m_browseInputFilesButton");
			this.m_browseInputFilesButton.Name = "m_browseInputFilesButton";
			this.m_browseInputFilesButton.UseVisualStyleBackColor = true;
			this.m_browseInputFilesButton.Click += new System.EventHandler(this.m_browseInputFilesButton_Click);
			//
			// label4
			//
			resources.ApplyResources(this.label4, "label4");
			this.label4.Name = "label4";
			//
			// label3
			//
			resources.ApplyResources(this.label3, "label3");
			this.label3.Name = "label3";
			//
			// label2
			//
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			//
			// label1
			//
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			//
			// tabPage2
			//
			this.tabPage2.Controls.Add(this.m_modifyMappingButton);
			this.tabPage2.Controls.Add(this.label7);
			this.tabPage2.Controls.Add(this.m_mappingsList);
			resources.ApplyResources(this.tabPage2, "tabPage2");
			this.tabPage2.Name = "tabPage2";
			this.tabPage2.UseVisualStyleBackColor = true;
			//
			// m_modifyMappingButton
			//
			resources.ApplyResources(this.m_modifyMappingButton, "m_modifyMappingButton");
			this.m_modifyMappingButton.Name = "m_modifyMappingButton";
			this.m_modifyMappingButton.UseVisualStyleBackColor = true;
			this.m_modifyMappingButton.Click += new System.EventHandler(this.m_modifyMappingButton_Click);
			//
			// label7
			//
			resources.ApplyResources(this.label7, "label7");
			this.label7.Name = "label7";
			//
			// m_mappingsList
			//
			resources.ApplyResources(this.m_mappingsList, "m_mappingsList");
			this.m_mappingsList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
			this.Marker,
			this.Counts,
			this.Destination,
			this.WritingSystem,
			this.Converter});
			this.m_mappingsList.FullRowSelect = true;
			this.m_mappingsList.HideSelection = false;
			this.m_mappingsList.MultiSelect = false;
			this.m_mappingsList.Name = "m_mappingsList";
			this.m_mappingsList.UseCompatibleStateImageBehavior = false;
			this.m_mappingsList.View = System.Windows.Forms.View.Details;
			this.m_mappingsList.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.m_mappingsList_MouseDoubleClick);
			//
			// Marker
			//
			resources.ApplyResources(this.Marker, "Marker");
			//
			// Counts
			//
			resources.ApplyResources(this.Counts, "Counts");
			//
			// Destination
			//
			resources.ApplyResources(this.Destination, "Destination");
			//
			// WritingSystem
			//
			resources.ApplyResources(this.WritingSystem, "WritingSystem");
			//
			// Converter
			//
			resources.ApplyResources(this.Converter, "Converter");
			//
			// tabPage4
			//
			this.tabPage4.Controls.Add(this.numberOfTextsLabel);
			this.tabPage4.Controls.Add(this.secretShiftText);
			this.tabPage4.Controls.Add(this.label8);
			this.tabPage4.Controls.Add(this.m_browseSaveSettingsFileButon);
			this.tabPage4.Controls.Add(this.m_saveSettingsFileBox);
			this.tabPage4.Controls.Add(this.label11);
			this.tabPage4.Controls.Add(this.label10);
			this.tabPage4.Controls.Add(this.label9);
			resources.ApplyResources(this.tabPage4, "tabPage4");
			this.tabPage4.Name = "tabPage4";
			this.tabPage4.UseVisualStyleBackColor = true;
			//
			// numberOfTextsLabel
			//
			resources.ApplyResources(this.numberOfTextsLabel, "numberOfTextsLabel");
			this.numberOfTextsLabel.Name = "numberOfTextsLabel";
			//
			// secretShiftText
			//
			resources.ApplyResources(this.secretShiftText, "secretShiftText");
			this.secretShiftText.ForeColor = System.Drawing.Color.DarkRed;
			this.secretShiftText.MaximumSize = new System.Drawing.Size(330, 0);
			this.secretShiftText.Name = "secretShiftText";
			//
			// label8
			//
			resources.ApplyResources(this.label8, "label8");
			this.label8.Name = "label8";
			//
			// m_browseSaveSettingsFileButon
			//
			resources.ApplyResources(this.m_browseSaveSettingsFileButon, "m_browseSaveSettingsFileButon");
			this.m_browseSaveSettingsFileButon.Name = "m_browseSaveSettingsFileButon";
			this.m_browseSaveSettingsFileButon.UseVisualStyleBackColor = true;
			this.m_browseSaveSettingsFileButon.Click += new System.EventHandler(this.m_browseSaveSettingsFileButon_Click);
			//
			// m_saveSettingsFileBox
			//
			resources.ApplyResources(this.m_saveSettingsFileBox, "m_saveSettingsFileBox");
			this.m_saveSettingsFileBox.Name = "m_saveSettingsFileBox";
			//
			// label11
			//
			resources.ApplyResources(this.label11, "label11");
			this.label11.Name = "label11";
			//
			// label10
			//
			resources.ApplyResources(this.label10, "label10");
			this.label10.Name = "label10";
			//
			// label9
			//
			resources.ApplyResources(this.label9, "label9");
			this.label9.Name = "label9";
			//
			// InterlinearSfmImportWizard
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = null;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
			this.Name = "InterlinearSfmImportWizard";
			this.ShowIcon = false;
			this.StepNames = new string[] {
		resources.GetString("$this.StepNames"),
		resources.GetString("$this.StepNames1"),
		resources.GetString("$this.StepNames2")};
			this.StepPageCount = 3;
			this.tabSteps.ResumeLayout(false);
			this.tabPage1.ResumeLayout(false);
			this.tabPage1.PerformLayout();
			this.tabPage2.ResumeLayout(false);
			this.tabPage4.ResumeLayout(false);
			this.tabPage4.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TabPage tabPage1;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button m_browseLoadSettingsFileButon;
		private System.Windows.Forms.TextBox m_loadSettingsFileBox;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.TextBox m_fileListBox;
		private System.Windows.Forms.Button m_browseInputFilesButton;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.TabPage tabPage2;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.ListView m_mappingsList;
		private System.Windows.Forms.Button m_modifyMappingButton;
		private System.Windows.Forms.ColumnHeader Marker;
		private System.Windows.Forms.ColumnHeader Destination;
		private System.Windows.Forms.ColumnHeader WritingSystem;
		private System.Windows.Forms.TabPage tabPage4;
		private System.Windows.Forms.Label label10;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.ColumnHeader Converter;
		private System.Windows.Forms.ColumnHeader Counts;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.Button m_browseSaveSettingsFileButon;
		private System.Windows.Forms.TextBox m_saveSettingsFileBox;
		private System.Windows.Forms.Label label11;
		private System.Windows.Forms.Label secretShiftText;
		private System.Windows.Forms.Label numberOfTextsLabel;
		protected System.Windows.Forms.Label label2;
		private System.Windows.Forms.LinkLabel m_useDefaultSettingsLink;
	}
}