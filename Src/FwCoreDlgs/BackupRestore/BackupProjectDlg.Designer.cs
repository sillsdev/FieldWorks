namespace SIL.FieldWorks.FwCoreDlgs.BackupRestore
{
	partial class BackupProjectDlg
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BackupProjectDlg));
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.m_spellCheckAdditions = new System.Windows.Forms.CheckBox();
			this.m_supportingFiles = new System.Windows.Forms.CheckBox();
			this.m_linkedFiles = new System.Windows.Forms.CheckBox();
			this.m_configurationSettings = new System.Windows.Forms.CheckBox();
			this.label2 = new System.Windows.Forms.Label();
			this.m_comment = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.m_destinationFolder = new System.Windows.Forms.TextBox();
			this.m_browse = new System.Windows.Forms.Button();
			this.m_backUp = new System.Windows.Forms.Button();
			this.m_cancel = new System.Windows.Forms.Button();
			this.m_help = new System.Windows.Forms.Button();
			this.toolTip = new System.Windows.Forms.ToolTip(this.components);
			this.helpProvider = new System.Windows.Forms.HelpProvider();
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			//
			// groupBox1
			//
			this.groupBox1.Controls.Add(this.m_spellCheckAdditions);
			this.groupBox1.Controls.Add(this.m_supportingFiles);
			this.groupBox1.Controls.Add(this.m_linkedFiles);
			this.groupBox1.Controls.Add(this.m_configurationSettings);
			resources.ApplyResources(this.groupBox1, "groupBox1");
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.TabStop = false;
			//
			// m_spellCheckAdditions
			//
			resources.ApplyResources(this.m_spellCheckAdditions, "m_spellCheckAdditions");
			this.m_spellCheckAdditions.ForeColor = System.Drawing.SystemColors.ControlText;
			this.m_spellCheckAdditions.Name = "m_spellCheckAdditions";
			this.helpProvider.SetShowHelp(this.m_spellCheckAdditions, ((bool)(resources.GetObject("m_spellCheckAdditions.ShowHelp"))));
			this.m_spellCheckAdditions.UseVisualStyleBackColor = true;
			//
			// m_supportingFiles
			//
			resources.ApplyResources(this.m_supportingFiles, "m_supportingFiles");
			this.m_supportingFiles.ForeColor = System.Drawing.SystemColors.ControlText;
			this.m_supportingFiles.Name = "m_supportingFiles";
			this.m_supportingFiles.UseVisualStyleBackColor = true;
			//
			// m_linkedFiles
			//
			resources.ApplyResources(this.m_linkedFiles, "m_linkedFiles");
			this.m_linkedFiles.ForeColor = System.Drawing.SystemColors.ControlText;
			this.m_linkedFiles.Name = "m_linkedFiles";
			this.m_linkedFiles.UseVisualStyleBackColor = true;
			//
			// m_configurationSettings
			//
			resources.ApplyResources(this.m_configurationSettings, "m_configurationSettings");
			this.m_configurationSettings.Checked = true;
			this.m_configurationSettings.CheckState = System.Windows.Forms.CheckState.Checked;
			this.m_configurationSettings.ForeColor = System.Drawing.SystemColors.ControlText;
			this.m_configurationSettings.Name = "m_configurationSettings";
			this.m_configurationSettings.UseVisualStyleBackColor = true;
			//
			// label2
			//
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			//
			// m_comment
			//
			resources.ApplyResources(this.m_comment, "m_comment");
			this.m_comment.Name = "m_comment";
			//
			// label3
			//
			resources.ApplyResources(this.label3, "label3");
			this.label3.Name = "label3";
			//
			// m_destinationFolder
			//
			resources.ApplyResources(this.m_destinationFolder, "m_destinationFolder");
			this.m_destinationFolder.Name = "m_destinationFolder";
			this.m_destinationFolder.TextChanged += new System.EventHandler(this.m_destinationFolder_TextChanged);
			//
			// m_browse
			//
			resources.ApplyResources(this.m_browse, "m_browse");
			this.m_browse.Name = "m_browse";
			this.m_browse.UseVisualStyleBackColor = true;
			this.m_browse.Click += new System.EventHandler(this.m_browse_Click);
			//
			// m_backUp
			//
			resources.ApplyResources(this.m_backUp, "m_backUp");
			this.m_backUp.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_backUp.Name = "m_backUp";
			this.m_backUp.UseVisualStyleBackColor = true;
			this.m_backUp.Click += new System.EventHandler(this.m_backUp_Click);
			//
			// m_cancel
			//
			resources.ApplyResources(this.m_cancel, "m_cancel");
			this.m_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_cancel.Name = "m_cancel";
			this.m_cancel.UseVisualStyleBackColor = true;
			//
			// m_help
			//
			resources.ApplyResources(this.m_help, "m_help");
			this.m_help.Name = "m_help";
			this.m_help.UseVisualStyleBackColor = true;
			this.m_help.Click += new System.EventHandler(this.m_help_Click);
			//
			// BackupProjectDlg
			//
			this.AcceptButton = this.m_backUp;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_cancel;
			this.Controls.Add(this.m_help);
			this.Controls.Add(this.m_cancel);
			this.Controls.Add(this.m_backUp);
			this.Controls.Add(this.m_browse);
			this.Controls.Add(this.m_destinationFolder);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.m_comment);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.groupBox1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "BackupProjectDlg";
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.CheckBox m_linkedFiles;
		private System.Windows.Forms.CheckBox m_configurationSettings;
		private System.Windows.Forms.CheckBox m_supportingFiles;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox m_comment;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.TextBox m_destinationFolder;
		private System.Windows.Forms.Button m_browse;
		private System.Windows.Forms.Button m_backUp;
		private System.Windows.Forms.Button m_cancel;
		private System.Windows.Forms.Button m_help;
		private System.Windows.Forms.ToolTip toolTip;
		private System.Windows.Forms.HelpProvider helpProvider;
		private System.Windows.Forms.CheckBox m_spellCheckAdditions;
	}
}