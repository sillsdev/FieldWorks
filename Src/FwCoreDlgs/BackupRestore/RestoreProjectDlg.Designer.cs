// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using SIL.FieldWorks.FDO.DomainServices.BackupRestore;
using SIL.Utils;

namespace SIL.FieldWorks.FwCoreDlgs.BackupRestore
{
	/// <summary>
	///
	/// </summary>
	partial class RestoreProjectDlg
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (disposing)
			{
				if (m_openFileDlg != null)
					m_openFileDlg.Dispose();
				if (components != null)
					components.Dispose();
			}
			m_openFileDlg = null;
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			Button m_btnHelp;
			ComponentResourceManager resources = new ComponentResourceManager(typeof(RestoreProjectDlg));
			Button m_btnCancel;
			Label label2;
			Label label3;
			Label label4;
			Label label5;
			Label label13;
			Label label16;
			this.m_btnBrowse = new Button();
			this.m_txtOtherProjectName = new TextBox();
			this.m_configurationSettings = new CheckBox();
			this.m_linkedFiles = new CheckBox();
			this.m_supportingFiles = new CheckBox();
			this.m_gbAlsoRestore = new GroupBox();
			this.m_spellCheckAdditions = new CheckBox();
			this.m_btnOk = new Button();
			this.m_lblOtherBackupIncludes = new Label();
			this.m_gbBackupProperties = new GroupBox();
			this.m_pnlDefaultBackupFolder = new Panel();
			this.m_lstVersions = new ListView();
			this.colDate = new ColumnHeader();
			this.colComment = new ColumnHeader();
			this.m_cboProjects = new ComboBox();
			this.m_lblDefaultBackupIncludes = new Label();
			this.label14 = new Label();
			this.m_pnlAnotherLocation = new Panel();
			this.m_lblBackupComment = new Label();
			this.m_lblBackupZipFile = new Label();
			this.m_lblBackupDate = new Label();
			this.m_lblBackupProjectName = new Label();
			this.label6 = new Label();
			this.m_gbRestoreAs = new GroupBox();
			this.m_rdoUseOriginalName = new RadioButton();
			this.m_rdoRestoreToName = new RadioButton();
			this.m_rdoDefaultFolder = new RadioButton();
			this.m_rdoAnotherLocation = new RadioButton();
			this.label1 = new Label();
			m_btnHelp = new Button();
			m_btnCancel = new Button();
			label2 = new Label();
			label3 = new Label();
			label4 = new Label();
			label5 = new Label();
			label13 = new Label();
			label16 = new Label();
			this.m_gbAlsoRestore.SuspendLayout();
			this.m_gbBackupProperties.SuspendLayout();
			this.m_pnlDefaultBackupFolder.SuspendLayout();
			this.m_pnlAnotherLocation.SuspendLayout();
			this.m_gbRestoreAs.SuspendLayout();
			this.SuspendLayout();
			//
			// m_btnHelp
			//
			resources.ApplyResources(m_btnHelp, "m_btnHelp");
			m_btnHelp.Name = "m_btnHelp";
			m_btnHelp.UseVisualStyleBackColor = true;
			m_btnHelp.Click += new EventHandler(this.m_btnHelp_Click);
			//
			// m_btnCancel
			//
			resources.ApplyResources(m_btnCancel, "m_btnCancel");
			m_btnCancel.DialogResult = DialogResult.Cancel;
			m_btnCancel.Name = "m_btnCancel";
			m_btnCancel.UseVisualStyleBackColor = true;
			//
			// label2
			//
			resources.ApplyResources(label2, "label2");
			label2.Name = "label2";
			//
			// label3
			//
			resources.ApplyResources(label3, "label3");
			label3.Name = "label3";
			//
			// label4
			//
			resources.ApplyResources(label4, "label4");
			label4.Name = "label4";
			//
			// label5
			//
			resources.ApplyResources(label5, "label5");
			label5.Name = "label5";
			//
			// label13
			//
			resources.ApplyResources(label13, "label13");
			label13.Name = "label13";
			//
			// label16
			//
			resources.ApplyResources(label16, "label16");
			label16.Name = "label16";
			//
			// m_btnBrowse
			//
			resources.ApplyResources(this.m_btnBrowse, "m_btnBrowse");
			this.m_btnBrowse.Name = "m_btnBrowse";
			this.m_btnBrowse.UseVisualStyleBackColor = true;
			this.m_btnBrowse.Click += new EventHandler(this.m_btnBrowse_Click);
			//
			// m_txtOtherProjectName
			//
			resources.ApplyResources(this.m_txtOtherProjectName, "m_txtOtherProjectName");
			this.m_txtOtherProjectName.Name = "m_txtOtherProjectName";
			//
			// m_configurationSettings
			//
			resources.ApplyResources(this.m_configurationSettings, "m_configurationSettings");
			this.m_configurationSettings.Name = "m_configurationSettings";
			this.m_configurationSettings.UseVisualStyleBackColor = true;
			//
			// m_linkedFiles
			//
			resources.ApplyResources(this.m_linkedFiles, "m_linkedFiles");
			this.m_linkedFiles.Name = "m_linkedFiles";
			this.m_linkedFiles.UseVisualStyleBackColor = true;
			//
			// m_supportingFiles
			//
			resources.ApplyResources(this.m_supportingFiles, "m_supportingFiles");
			this.m_supportingFiles.Name = "m_supportingFiles";
			this.m_supportingFiles.UseVisualStyleBackColor = true;
			//
			// m_gbAlsoRestore
			//
			resources.ApplyResources(this.m_gbAlsoRestore, "m_gbAlsoRestore");
			this.m_gbAlsoRestore.Controls.Add(this.m_spellCheckAdditions);
			this.m_gbAlsoRestore.Controls.Add(this.m_supportingFiles);
			this.m_gbAlsoRestore.Controls.Add(this.m_linkedFiles);
			this.m_gbAlsoRestore.Controls.Add(this.m_configurationSettings);
			this.m_gbAlsoRestore.Name = "m_gbAlsoRestore";
			this.m_gbAlsoRestore.TabStop = false;
			//
			// m_spellCheckAdditions
			//
			resources.ApplyResources(this.m_spellCheckAdditions, "m_spellCheckAdditions");
			this.m_spellCheckAdditions.Name = "m_spellCheckAdditions";
			this.m_spellCheckAdditions.UseVisualStyleBackColor = true;
			//
			// m_btnOk
			//
			resources.ApplyResources(this.m_btnOk, "m_btnOk");
			this.m_btnOk.Name = "m_btnOk";
			this.m_btnOk.UseVisualStyleBackColor = true;
			this.m_btnOk.Click += new EventHandler(this.m_btnOk_Click);
			//
			// m_lblOtherBackupIncludes
			//
			resources.ApplyResources(this.m_lblOtherBackupIncludes, "m_lblOtherBackupIncludes");
			this.m_lblOtherBackupIncludes.Name = "m_lblOtherBackupIncludes";
			//
			// m_gbBackupProperties
			//
			resources.ApplyResources(this.m_gbBackupProperties, "m_gbBackupProperties");
			this.m_gbBackupProperties.Controls.Add(this.m_pnlDefaultBackupFolder);
			this.m_gbBackupProperties.Controls.Add(this.m_pnlAnotherLocation);
			this.m_gbBackupProperties.Name = "m_gbBackupProperties";
			this.m_gbBackupProperties.TabStop = false;
			//
			// m_pnlDefaultBackupFolder
			//
			this.m_pnlDefaultBackupFolder.Controls.Add(this.m_lstVersions);
			this.m_pnlDefaultBackupFolder.Controls.Add(this.m_cboProjects);
			this.m_pnlDefaultBackupFolder.Controls.Add(this.m_lblDefaultBackupIncludes);
			this.m_pnlDefaultBackupFolder.Controls.Add(label13);
			this.m_pnlDefaultBackupFolder.Controls.Add(this.label14);
			this.m_pnlDefaultBackupFolder.Controls.Add(label16);
			resources.ApplyResources(this.m_pnlDefaultBackupFolder, "m_pnlDefaultBackupFolder");
			this.m_pnlDefaultBackupFolder.Name = "m_pnlDefaultBackupFolder";
			//
			// m_lstVersions
			//
			resources.ApplyResources(this.m_lstVersions, "m_lstVersions");
			this.m_lstVersions.Columns.AddRange(new ColumnHeader[] {
			this.colDate,
			this.colComment});
			this.m_lstVersions.FullRowSelect = true;
			this.m_lstVersions.HeaderStyle = ColumnHeaderStyle.None;
			this.m_lstVersions.HideSelection = false;
			this.m_lstVersions.MultiSelect = false;
			this.m_lstVersions.Name = "m_lstVersions";
			this.m_lstVersions.ShowGroups = false;
			this.m_lstVersions.UseCompatibleStateImageBehavior = false;
			this.m_lstVersions.View = View.Details;
			this.m_lstVersions.SelectedIndexChanged += new EventHandler(this.m_lstVersions_SelectedIndexChanged);
			this.m_lstVersions.SizeChanged += new EventHandler(this.m_lstVersions_SizeChanged);
			//
			// colDate
			//
			resources.ApplyResources(this.colDate, "colDate");
			//
			// colComment
			//
			resources.ApplyResources(this.colComment, "colComment");
			//
			// m_cboProjects
			//
			resources.ApplyResources(this.m_cboProjects, "m_cboProjects");
			this.m_cboProjects.DropDownStyle = ComboBoxStyle.DropDownList;
			this.m_cboProjects.FormattingEnabled = true;
			this.m_cboProjects.Name = "m_cboProjects";
			this.m_cboProjects.SelectedIndexChanged += new EventHandler(this.m_cboProjects_SelectedIndexChanged);
			//
			// m_lblDefaultBackupIncludes
			//
			resources.ApplyResources(this.m_lblDefaultBackupIncludes, "m_lblDefaultBackupIncludes");
			this.m_lblDefaultBackupIncludes.Name = "m_lblDefaultBackupIncludes";
			//
			// label14
			//
			resources.ApplyResources(this.label14, "label14");
			this.label14.Name = "label14";
			//
			// m_pnlAnotherLocation
			//
			this.m_pnlAnotherLocation.Controls.Add(this.m_lblBackupComment);
			this.m_pnlAnotherLocation.Controls.Add(this.m_lblBackupZipFile);
			this.m_pnlAnotherLocation.Controls.Add(this.m_lblBackupDate);
			this.m_pnlAnotherLocation.Controls.Add(this.m_lblOtherBackupIncludes);
			this.m_pnlAnotherLocation.Controls.Add(this.m_lblBackupProjectName);
			this.m_pnlAnotherLocation.Controls.Add(label2);
			this.m_pnlAnotherLocation.Controls.Add(label3);
			this.m_pnlAnotherLocation.Controls.Add(this.label6);
			this.m_pnlAnotherLocation.Controls.Add(label4);
			this.m_pnlAnotherLocation.Controls.Add(label5);
			resources.ApplyResources(this.m_pnlAnotherLocation, "m_pnlAnotherLocation");
			this.m_pnlAnotherLocation.Name = "m_pnlAnotherLocation";
			//
			// m_lblBackupComment
			//
			resources.ApplyResources(this.m_lblBackupComment, "m_lblBackupComment");
			this.m_lblBackupComment.Name = "m_lblBackupComment";
			//
			// m_lblBackupZipFile
			//
			resources.ApplyResources(this.m_lblBackupZipFile, "m_lblBackupZipFile");
			this.m_lblBackupZipFile.Name = "m_lblBackupZipFile";
			//
			// m_lblBackupDate
			//
			resources.ApplyResources(this.m_lblBackupDate, "m_lblBackupDate");
			this.m_lblBackupDate.Name = "m_lblBackupDate";
			//
			// m_lblBackupProjectName
			//
			resources.ApplyResources(this.m_lblBackupProjectName, "m_lblBackupProjectName");
			this.m_lblBackupProjectName.Name = "m_lblBackupProjectName";
			//
			// label6
			//
			resources.ApplyResources(this.label6, "label6");
			this.label6.Name = "label6";
			//
			// m_gbRestoreAs
			//
			resources.ApplyResources(this.m_gbRestoreAs, "m_gbRestoreAs");
			this.m_gbRestoreAs.Controls.Add(this.m_rdoUseOriginalName);
			this.m_gbRestoreAs.Controls.Add(this.m_rdoRestoreToName);
			this.m_gbRestoreAs.Controls.Add(this.m_txtOtherProjectName);
			this.m_gbRestoreAs.Name = "m_gbRestoreAs";
			this.m_gbRestoreAs.TabStop = false;
			//
			// m_rdoUseOriginalName
			//
			resources.ApplyResources(this.m_rdoUseOriginalName, "m_rdoUseOriginalName");
			this.m_rdoUseOriginalName.Checked = true;
			this.m_rdoUseOriginalName.Name = "m_rdoUseOriginalName";
			this.m_rdoUseOriginalName.TabStop = true;
			this.m_rdoUseOriginalName.UseVisualStyleBackColor = true;
			//
			// m_rdoRestoreToName
			//
			resources.ApplyResources(this.m_rdoRestoreToName, "m_rdoRestoreToName");
			this.m_rdoRestoreToName.Name = "m_rdoRestoreToName";
			this.m_rdoRestoreToName.TabStop = true;
			this.m_rdoRestoreToName.UseVisualStyleBackColor = true;
			this.m_rdoRestoreToName.CheckedChanged += new EventHandler(this.m_rdoRestoreToName_CheckedChanged);
			//
			// m_rdoDefaultFolder
			//
			resources.ApplyResources(this.m_rdoDefaultFolder, "m_rdoDefaultFolder");
			this.m_rdoDefaultFolder.Checked = true;
			this.m_rdoDefaultFolder.Name = "m_rdoDefaultFolder";
			this.m_rdoDefaultFolder.TabStop = true;
			this.m_rdoDefaultFolder.UseVisualStyleBackColor = true;
			this.m_rdoDefaultFolder.CheckedChanged += new EventHandler(this.m_rdoDefaultFolder_CheckedChanged);
			//
			// m_rdoAnotherLocation
			//
			resources.ApplyResources(this.m_rdoAnotherLocation, "m_rdoAnotherLocation");
			this.m_rdoAnotherLocation.Name = "m_rdoAnotherLocation";
			this.m_rdoAnotherLocation.UseVisualStyleBackColor = true;
			//
			// label1
			//
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			//
			// RestoreProjectDlg
			//
			this.AcceptButton = this.m_btnOk;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = AutoScaleMode.Font;
			this.CancelButton = m_btnCancel;
			this.Controls.Add(this.label1);
			this.Controls.Add(this.m_gbRestoreAs);
			this.Controls.Add(this.m_rdoDefaultFolder);
			this.Controls.Add(m_btnCancel);
			this.Controls.Add(this.m_rdoAnotherLocation);
			this.Controls.Add(this.m_btnOk);
			this.Controls.Add(this.m_btnBrowse);
			this.Controls.Add(m_btnHelp);
			this.Controls.Add(this.m_gbAlsoRestore);
			this.Controls.Add(this.m_gbBackupProperties);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "RestoreProjectDlg";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.m_gbAlsoRestore.ResumeLayout(false);
			this.m_gbAlsoRestore.PerformLayout();
			this.m_gbBackupProperties.ResumeLayout(false);
			this.m_pnlDefaultBackupFolder.ResumeLayout(false);
			this.m_pnlDefaultBackupFolder.PerformLayout();
			this.m_pnlAnotherLocation.ResumeLayout(false);
			this.m_pnlAnotherLocation.PerformLayout();
			this.m_gbRestoreAs.ResumeLayout(false);
			this.m_gbRestoreAs.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private Button m_btnBrowse;
		private TextBox m_txtOtherProjectName;
		private CheckBox m_configurationSettings;
		private CheckBox m_linkedFiles;
		private CheckBox m_supportingFiles;
		private GroupBox m_gbAlsoRestore;
		private Button m_btnOk;
		private Label m_lblOtherBackupIncludes;
		private GroupBox m_gbBackupProperties;
		private CheckBox m_spellCheckAdditions;
		private GroupBox m_gbRestoreAs;
		private RadioButton m_rdoUseOriginalName;
		private RadioButton m_rdoRestoreToName;
		private RadioButton m_rdoDefaultFolder;
		private RadioButton m_rdoAnotherLocation;
		private Label label6;
		private Label m_lblBackupComment;
		private Label m_lblBackupDate;
		private Label m_lblBackupProjectName;
		private Label m_lblBackupZipFile;
		private Panel m_pnlAnotherLocation;
		private Label label1;
		private Panel m_pnlDefaultBackupFolder;
		private Label m_lblDefaultBackupIncludes;
		private Label label14;
		private ComboBox m_cboProjects;
		private ListView m_lstVersions;
		private ColumnHeader colDate;
		private ColumnHeader colComment;
	}
}