namespace SIL.FieldWorks.FwCoreDlgs
{
	partial class ProjectLocationDlg
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ProjectLocationDlg));
			this.m_btnBrowseProjectFolder = new System.Windows.Forms.Button();
			this.label3 = new System.Windows.Forms.Label();
			this.m_tbProjectsFolder = new System.Windows.Forms.TextBox();
			this.m_btnHelp = new System.Windows.Forms.Button();
			this.m_btnCancel = new System.Windows.Forms.Button();
			this.m_btnOK = new System.Windows.Forms.Button();
			this.m_lbCurrentProject = new System.Windows.Forms.Label();
			this.m_tbCurrentProjectPath = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// m_btnBrowseProjectFolder
			// 
			resources.ApplyResources(this.m_btnBrowseProjectFolder, "m_btnBrowseProjectFolder");
			this.m_btnBrowseProjectFolder.Name = "m_btnBrowseProjectFolder";
			this.m_btnBrowseProjectFolder.UseVisualStyleBackColor = true;
			this.m_btnBrowseProjectFolder.Click += new System.EventHandler(this.m_btnBrowseProjectFolder_Click);
			// 
			// label3
			// 
			resources.ApplyResources(this.label3, "label3");
			this.label3.Name = "label3";
			// 
			// m_tbProjectsFolder
			// 
			resources.ApplyResources(this.m_tbProjectsFolder, "m_tbProjectsFolder");
			this.m_tbProjectsFolder.Name = "m_tbProjectsFolder";
			// 
			// m_btnHelp
			// 
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			this.m_btnHelp.Name = "m_btnHelp";
			this.m_btnHelp.UseVisualStyleBackColor = true;
			this.m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			// 
			// m_btnCancel
			// 
			resources.ApplyResources(this.m_btnCancel, "m_btnCancel");
			this.m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btnCancel.Name = "m_btnCancel";
			this.m_btnCancel.UseVisualStyleBackColor = true;
			this.m_btnCancel.Click += new System.EventHandler(this.m_btnCancel_Click);
			// 
			// m_btnOK
			// 
			resources.ApplyResources(this.m_btnOK, "m_btnOK");
			this.m_btnOK.Name = "m_btnOK";
			this.m_btnOK.UseVisualStyleBackColor = true;
			this.m_btnOK.Click += new System.EventHandler(this.m_btnOK_Click);
			// 
			// m_lbCurrentProject
			// 
			resources.ApplyResources(this.m_lbCurrentProject, "m_lbCurrentProject");
			this.m_lbCurrentProject.Name = "m_lbCurrentProject";
			// 
			// m_tbCurrentProjectPath
			// 
			resources.ApplyResources(this.m_tbCurrentProjectPath, "m_tbCurrentProjectPath");
			this.m_tbCurrentProjectPath.BackColor = System.Drawing.SystemColors.Control;
			this.m_tbCurrentProjectPath.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.m_tbCurrentProjectPath.Name = "m_tbCurrentProjectPath";
			// 
			// ProjectLocationDlg
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_tbCurrentProjectPath);
			this.Controls.Add(this.m_lbCurrentProject);
			this.Controls.Add(this.m_btnHelp);
			this.Controls.Add(this.m_btnCancel);
			this.Controls.Add(this.m_btnOK);
			this.Controls.Add(this.m_btnBrowseProjectFolder);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.m_tbProjectsFolder);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ProjectLocationDlg";
			this.ShowInTaskbar = false;
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button m_btnBrowseProjectFolder;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.TextBox m_tbProjectsFolder;
		private System.Windows.Forms.Button m_btnHelp;
		private System.Windows.Forms.Button m_btnCancel;
		private System.Windows.Forms.Button m_btnOK;
		private System.Windows.Forms.Label m_lbCurrentProject;
		private System.Windows.Forms.TextBox m_tbCurrentProjectPath;


	}
}