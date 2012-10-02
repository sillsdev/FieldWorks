namespace SIL.FieldWorks.TE
{
	partial class ExportPtxDialog : ExportUsfmDialog
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ExportPtxDialog));
			this.rdoScripture = new System.Windows.Forms.RadioButton();
			this.rdoBackTranslation = new System.Windows.Forms.RadioButton();
			this.lblShortName = new System.Windows.Forms.Label();
			this.cboShortName = new System.Windows.Forms.ComboBox();
			this.pnlExportWhat.SuspendLayout();
			this.grpOutputTo.SuspendLayout();
			this.grpExportWhat.SuspendLayout();
			this.SuspendLayout();
			//
			// btnFolderBrowse
			//
			resources.ApplyResources(this.btnFolderBrowse, "btnFolderBrowse");
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
			this.txtOutputFolder.ReadOnly = true;
			//
			// fileNameSchemeCtrl
			//
			resources.ApplyResources(this.fileNameSchemeCtrl, "fileNameSchemeCtrl");
			//
			// pnlExportWhat
			//
			this.pnlExportWhat.Controls.Add(this.rdoBackTranslation);
			this.pnlExportWhat.Controls.Add(this.rdoScripture);
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
			this.grpOutputTo.Controls.Add(this.cboShortName);
			this.grpOutputTo.Controls.Add(this.lblShortName);
			resources.ApplyResources(this.grpOutputTo, "grpOutputTo");
			this.grpOutputTo.Controls.SetChildIndex(this.txtOutputFolder, 0);
			this.grpOutputTo.Controls.SetChildIndex(this.btnFolderBrowse, 0);
			this.grpOutputTo.Controls.SetChildIndex(this.lblOutputFolder, 0);
			this.grpOutputTo.Controls.SetChildIndex(this.fileNameSchemeCtrl, 0);
			this.grpOutputTo.Controls.SetChildIndex(this.lblShortName, 0);
			this.grpOutputTo.Controls.SetChildIndex(this.cboShortName, 0);
			//
			// rdoScripture
			//
			resources.ApplyResources(this.rdoScripture, "rdoScripture");
			this.rdoScripture.Name = "rdoScripture";
			this.rdoScripture.TabStop = true;
			this.rdoScripture.UseVisualStyleBackColor = true;
			this.rdoScripture.CheckedChanged += new System.EventHandler(this.rdoScripture_CheckedChanged);
			//
			// rdoBackTranslation
			//
			resources.ApplyResources(this.rdoBackTranslation, "rdoBackTranslation");
			this.rdoBackTranslation.Name = "rdoBackTranslation";
			this.rdoBackTranslation.TabStop = true;
			this.rdoBackTranslation.UseVisualStyleBackColor = true;
			//
			// lblShortName
			//
			resources.ApplyResources(this.lblShortName, "lblShortName");
			this.lblShortName.Name = "lblShortName";
			//
			// cboShortName
			//
			this.cboShortName.FormattingEnabled = true;
			resources.ApplyResources(this.cboShortName, "cboShortName");
			this.cboShortName.Name = "cboShortName";
			this.cboShortName.Sorted = true;
			this.cboShortName.SelectedIndexChanged += new System.EventHandler(this.cboShortName_SelectedIndexChanged);
			this.cboShortName.Leave += new System.EventHandler(this.cboShortName_Leave);
			this.cboShortName.TextChanged += new System.EventHandler(this.cboShortName_TextChanged);
			//
			// ExportPtxDialog
			//
			resources.ApplyResources(this, "$this");
			this.Name = "ExportPtxDialog";
			this.pnlExportWhat.ResumeLayout(false);
			this.pnlExportWhat.PerformLayout();
			this.grpOutputTo.ResumeLayout(false);
			this.grpOutputTo.PerformLayout();
			this.grpExportWhat.ResumeLayout(false);
			this.grpExportWhat.PerformLayout();
			this.ResumeLayout(false);

		}
		#endregion

		/// <summary></summary>
		protected System.Windows.Forms.RadioButton rdoBackTranslation;
		/// <summary></summary>
		protected System.Windows.Forms.RadioButton rdoScripture;
		private System.Windows.Forms.Label lblShortName;
		/// <summary></summary>
		protected System.Windows.Forms.ComboBox cboShortName;
	}
}
