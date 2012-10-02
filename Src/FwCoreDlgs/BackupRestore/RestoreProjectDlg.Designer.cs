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
			System.Windows.Forms.Button m_btnHelp;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RestoreProjectDlg));
			System.Windows.Forms.Button m_btnCancel;
			System.Windows.Forms.Label label2;
			System.Windows.Forms.Label label3;
			System.Windows.Forms.Label label4;
			System.Windows.Forms.Label label5;
			System.Windows.Forms.Label label13;
			System.Windows.Forms.Label label16;
			this.m_btnBrowse = new System.Windows.Forms.Button();
			this.m_txtOtherProjectName = new System.Windows.Forms.TextBox();
			this.m_configurationSettings = new System.Windows.Forms.CheckBox();
			this.m_linkedFiles = new System.Windows.Forms.CheckBox();
			this.m_supportingFiles = new System.Windows.Forms.CheckBox();
			this.m_gbAlsoRestore = new System.Windows.Forms.GroupBox();
			this.m_spellCheckAdditions = new System.Windows.Forms.CheckBox();
			this.m_btnOk = new System.Windows.Forms.Button();
			this.m_lblOtherBackupIncludes = new System.Windows.Forms.Label();
			this.m_gbBackupProperties = new System.Windows.Forms.GroupBox();
			this.m_pnlDefaultBackupFolder = new System.Windows.Forms.Panel();
			this.m_lstVersions = new System.Windows.Forms.ListView();
			this.colDate = new System.Windows.Forms.ColumnHeader();
			this.colComment = new System.Windows.Forms.ColumnHeader();
			this.m_cboProjects = new System.Windows.Forms.ComboBox();
			this.m_lblDefaultBackupIncludes = new System.Windows.Forms.Label();
			this.label14 = new System.Windows.Forms.Label();
			this.m_pnlAnotherLocation = new System.Windows.Forms.Panel();
			this.m_lblBackupComment = new System.Windows.Forms.Label();
			this.m_lblBackupZipFile = new System.Windows.Forms.Label();
			this.m_lblBackupDate = new System.Windows.Forms.Label();
			this.m_lblBackupProjectName = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.m_gbRestoreAs = new System.Windows.Forms.GroupBox();
			this.m_rdoUseOriginalName = new System.Windows.Forms.RadioButton();
			this.m_rdoRestoreToName = new System.Windows.Forms.RadioButton();
			this.m_rdoDefaultFolder = new System.Windows.Forms.RadioButton();
			this.m_rdoAnotherLocation = new System.Windows.Forms.RadioButton();
			this.label1 = new System.Windows.Forms.Label();
			m_btnHelp = new System.Windows.Forms.Button();
			m_btnCancel = new System.Windows.Forms.Button();
			label2 = new System.Windows.Forms.Label();
			label3 = new System.Windows.Forms.Label();
			label4 = new System.Windows.Forms.Label();
			label5 = new System.Windows.Forms.Label();
			label13 = new System.Windows.Forms.Label();
			label16 = new System.Windows.Forms.Label();
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
			m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// m_btnCancel
			//
			resources.ApplyResources(m_btnCancel, "m_btnCancel");
			m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
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
			this.m_btnBrowse.Click += new System.EventHandler(this.m_btnBrowse_Click);
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
			this.m_btnOk.Click += new System.EventHandler(this.m_btnOk_Click);
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
			this.m_lstVersions.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
			this.colDate,
			this.colComment});
			this.m_lstVersions.FullRowSelect = true;
			this.m_lstVersions.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
			this.m_lstVersions.HideSelection = false;
			this.m_lstVersions.MultiSelect = false;
			this.m_lstVersions.Name = "m_lstVersions";
			this.m_lstVersions.ShowGroups = false;
			this.m_lstVersions.UseCompatibleStateImageBehavior = false;
			this.m_lstVersions.View = System.Windows.Forms.View.Details;
			this.m_lstVersions.SelectedIndexChanged += new System.EventHandler(this.m_lstVersions_SelectedIndexChanged);
			this.m_lstVersions.SizeChanged += new System.EventHandler(this.m_lstVersions_SizeChanged);
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
			this.m_cboProjects.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cboProjects.FormattingEnabled = true;
			this.m_cboProjects.Name = "m_cboProjects";
			this.m_cboProjects.SelectedIndexChanged += new System.EventHandler(this.m_cboProjects_SelectedIndexChanged);
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
			this.m_rdoRestoreToName.CheckedChanged += new System.EventHandler(this.m_rdoRestoreToName_CheckedChanged);
			//
			// m_rdoDefaultFolder
			//
			resources.ApplyResources(this.m_rdoDefaultFolder, "m_rdoDefaultFolder");
			this.m_rdoDefaultFolder.Checked = true;
			this.m_rdoDefaultFolder.Name = "m_rdoDefaultFolder";
			this.m_rdoDefaultFolder.TabStop = true;
			this.m_rdoDefaultFolder.UseVisualStyleBackColor = true;
			this.m_rdoDefaultFolder.CheckedChanged += new System.EventHandler(this.m_rdoDefaultFolder_CheckedChanged);
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
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
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

		private System.Windows.Forms.Button m_btnBrowse;
		private System.Windows.Forms.TextBox m_txtOtherProjectName;
		private System.Windows.Forms.CheckBox m_configurationSettings;
		private System.Windows.Forms.CheckBox m_linkedFiles;
		private System.Windows.Forms.CheckBox m_supportingFiles;
		private System.Windows.Forms.GroupBox m_gbAlsoRestore;
		private System.Windows.Forms.Button m_btnOk;
		private System.Windows.Forms.Label m_lblOtherBackupIncludes;
		private System.Windows.Forms.GroupBox m_gbBackupProperties;
		private System.Windows.Forms.CheckBox m_spellCheckAdditions;
		private System.Windows.Forms.GroupBox m_gbRestoreAs;
		private System.Windows.Forms.RadioButton m_rdoUseOriginalName;
		private System.Windows.Forms.RadioButton m_rdoRestoreToName;
		private System.Windows.Forms.RadioButton m_rdoDefaultFolder;
		private System.Windows.Forms.RadioButton m_rdoAnotherLocation;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label m_lblBackupComment;
		private System.Windows.Forms.Label m_lblBackupDate;
		private System.Windows.Forms.Label m_lblBackupProjectName;
		private System.Windows.Forms.Label m_lblBackupZipFile;
		private System.Windows.Forms.Panel m_pnlAnotherLocation;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Panel m_pnlDefaultBackupFolder;
		private System.Windows.Forms.Label m_lblDefaultBackupIncludes;
		private System.Windows.Forms.Label label14;
		private System.Windows.Forms.ComboBox m_cboProjects;
		private System.Windows.Forms.ListView m_lstVersions;
		private System.Windows.Forms.ColumnHeader colDate;
		private System.Windows.Forms.ColumnHeader colComment;
	}
}