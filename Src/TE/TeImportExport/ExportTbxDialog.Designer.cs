namespace SIL.FieldWorks.TE
{
	partial class ExportTbxDialog : ExportUsfmDialog
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ExportTbxDialog));
			this.chkScripture = new System.Windows.Forms.CheckBox();
			this.chkBackTranslation = new System.Windows.Forms.CheckBox();
			this.txtOutputFile = new System.Windows.Forms.TextBox();
			this.lblOutputFile = new System.Windows.Forms.Label();
			this.btnFileBrowse = new System.Windows.Forms.Button();
			this.rdoOneFilePerbook = new System.Windows.Forms.RadioButton();
			this.rdoSingleFile = new System.Windows.Forms.RadioButton();
			this.chkArabicNumbers = new System.Windows.Forms.CheckBox();
			this.pnlExportWhat.SuspendLayout();
			this.grpOutputTo.SuspendLayout();
			this.grpExportWhat.SuspendLayout();
			this.SuspendLayout();
			//
			// btnOk
			//
			resources.ApplyResources(this.btnOk, "btnOk");
			//
			// lblOutputFolder
			//
			resources.ApplyResources(this.lblOutputFolder, "lblOutputFolder");
			//
			// txtOutputFolder
			//
			resources.ApplyResources(this.txtOutputFolder, "txtOutputFolder");
			//
			// fileNameSchemeCtrl
			//
			resources.ApplyResources(this.fileNameSchemeCtrl, "fileNameSchemeCtrl");
			this.fileNameSchemeCtrl.Markup = SIL.FieldWorks.TE.MarkupType.Toolbox;
			//
			// pnlExportWhat
			//
			this.pnlExportWhat.Controls.Add(this.chkArabicNumbers);
			this.pnlExportWhat.Controls.Add(this.chkBackTranslation);
			this.pnlExportWhat.Controls.Add(this.chkScripture);
			//
			// btnCancel
			//
			resources.ApplyResources(this.btnCancel, "btnCancel");
			//
			// m_btnHelp
			//
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			//
			// grpOutputTo
			//
			this.grpOutputTo.Controls.Add(this.rdoOneFilePerbook);
			this.grpOutputTo.Controls.Add(this.rdoSingleFile);
			this.grpOutputTo.Controls.Add(this.txtOutputFile);
			this.grpOutputTo.Controls.Add(this.btnFileBrowse);
			this.grpOutputTo.Controls.Add(this.lblOutputFile);
			resources.ApplyResources(this.grpOutputTo, "grpOutputTo");
			this.grpOutputTo.Controls.SetChildIndex(this.lblOutputFile, 0);
			this.grpOutputTo.Controls.SetChildIndex(this.btnFileBrowse, 0);
			this.grpOutputTo.Controls.SetChildIndex(this.txtOutputFile, 0);
			this.grpOutputTo.Controls.SetChildIndex(this.rdoSingleFile, 0);
			this.grpOutputTo.Controls.SetChildIndex(this.rdoOneFilePerbook, 0);
			this.grpOutputTo.Controls.SetChildIndex(this.btnFolderBrowse, 0);
			this.grpOutputTo.Controls.SetChildIndex(this.txtOutputFolder, 0);
			this.grpOutputTo.Controls.SetChildIndex(this.lblOutputFolder, 0);
			this.grpOutputTo.Controls.SetChildIndex(this.fileNameSchemeCtrl, 0);
			//
			// chkScripture
			//
			resources.ApplyResources(this.chkScripture, "chkScripture");
			this.chkScripture.Name = "chkScripture";
			this.chkScripture.UseVisualStyleBackColor = true;
			this.chkScripture.CheckedChanged += new System.EventHandler(this.chkScripture_CheckedChanged);
			//
			// chkBackTranslation
			//
			resources.ApplyResources(this.chkBackTranslation, "chkBackTranslation");
			this.chkBackTranslation.Name = "chkBackTranslation";
			this.chkBackTranslation.UseVisualStyleBackColor = true;
			this.chkBackTranslation.CheckedChanged += new System.EventHandler(this.chkBackTranslation_CheckedChanged);
			//
			// txtOutputFile
			//
			resources.ApplyResources(this.txtOutputFile, "txtOutputFile");
			this.txtOutputFile.Name = "txtOutputFile";
			this.txtOutputFile.TextChanged += new System.EventHandler(this.txtOutputFile_TextChanged);
			//
			// lblOutputFile
			//
			resources.ApplyResources(this.lblOutputFile, "lblOutputFile");
			this.lblOutputFile.Name = "lblOutputFile";
			//
			// btnFileBrowse
			//
			resources.ApplyResources(this.btnFileBrowse, "btnFileBrowse");
			this.btnFileBrowse.Name = "btnFileBrowse";
			this.btnFileBrowse.UseVisualStyleBackColor = true;
			this.btnFileBrowse.Click += new System.EventHandler(this.btnFileBrowse_Click);
			//
			// rdoOneFilePerbook
			//
			resources.ApplyResources(this.rdoOneFilePerbook, "rdoOneFilePerbook");
			this.rdoOneFilePerbook.Name = "rdoOneFilePerbook";
			this.rdoOneFilePerbook.TabStop = true;
			this.rdoOneFilePerbook.UseVisualStyleBackColor = true;
			this.rdoOneFilePerbook.CheckedChanged += new System.EventHandler(this.OutputToCheckedChanged);
			//
			// rdoSingleFile
			//
			resources.ApplyResources(this.rdoSingleFile, "rdoSingleFile");
			this.rdoSingleFile.Name = "rdoSingleFile";
			this.rdoSingleFile.TabStop = true;
			this.rdoSingleFile.UseVisualStyleBackColor = true;
			this.rdoSingleFile.CheckedChanged += new System.EventHandler(this.OutputToCheckedChanged);
			//
			// chkArabicNumbers
			//
			resources.ApplyResources(this.chkArabicNumbers, "chkArabicNumbers");
			this.chkArabicNumbers.Name = "chkArabicNumbers";
			this.chkArabicNumbers.UseVisualStyleBackColor = true;
			//
			// ExportTbxDialog
			//
			resources.ApplyResources(this, "$this");
			this.Name = "ExportTbxDialog";
			this.Load += new System.EventHandler(this.ExportTbxDialog_Load);
			this.pnlExportWhat.ResumeLayout(false);
			this.pnlExportWhat.PerformLayout();
			this.grpOutputTo.ResumeLayout(false);
			this.grpOutputTo.PerformLayout();
			this.grpExportWhat.ResumeLayout(false);
			this.grpExportWhat.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.CheckBox chkScripture;
		private System.Windows.Forms.CheckBox chkBackTranslation;
		private System.Windows.Forms.Button btnFileBrowse;
		private System.Windows.Forms.Label lblOutputFile;
		private System.Windows.Forms.TextBox txtOutputFile;
		private System.Windows.Forms.RadioButton rdoSingleFile;
		private System.Windows.Forms.RadioButton rdoOneFilePerbook;
		private System.Windows.Forms.CheckBox chkArabicNumbers;
	}
}
