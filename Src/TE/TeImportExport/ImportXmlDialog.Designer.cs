namespace SIL.FieldWorks.TE
{
	partial class ImportXmlDialog
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ImportXmlDialog));
			this.m_lblOXESFile = new System.Windows.Forms.Label();
			this.m_tbFilename = new System.Windows.Forms.TextBox();
			this.m_btnBrowse = new System.Windows.Forms.Button();
			this.m_btnImport = new System.Windows.Forms.Button();
			this.m_btnCancel = new System.Windows.Forms.Button();
			this.m_btnHelp = new System.Windows.Forms.Button();
			this.m_lblDescription = new System.Windows.Forms.Label();
			this.SuspendLayout();
			//
			// m_lblOXESFile
			//
			resources.ApplyResources(this.m_lblOXESFile, "m_lblOXESFile");
			this.m_lblOXESFile.Name = "m_lblOXESFile";
			//
			// m_tbFilename
			//
			resources.ApplyResources(this.m_tbFilename, "m_tbFilename");
			this.m_tbFilename.Name = "m_tbFilename";
			this.m_tbFilename.TextChanged += new System.EventHandler(this.m_tbFilename_TextChanged);
			//
			// m_btnBrowse
			//
			resources.ApplyResources(this.m_btnBrowse, "m_btnBrowse");
			this.m_btnBrowse.Name = "m_btnBrowse";
			this.m_btnBrowse.UseVisualStyleBackColor = true;
			this.m_btnBrowse.Click += new System.EventHandler(this.m_btnBrowse_Click);
			//
			// m_btnImport
			//
			resources.ApplyResources(this.m_btnImport, "m_btnImport");
			this.m_btnImport.Name = "m_btnImport";
			this.m_btnImport.UseVisualStyleBackColor = true;
			this.m_btnImport.Click += new System.EventHandler(this.m_btnImport_Click);
			//
			// m_btnCancel
			//
			resources.ApplyResources(this.m_btnCancel, "m_btnCancel");
			this.m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btnCancel.Name = "m_btnCancel";
			this.m_btnCancel.UseVisualStyleBackColor = true;
			this.m_btnCancel.Click += new System.EventHandler(this.m_btnCancel_Click);
			//
			// m_btnHelp
			//
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			this.m_btnHelp.Name = "m_btnHelp";
			this.m_btnHelp.UseVisualStyleBackColor = true;
			this.m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// m_lblDescription
			//
			resources.ApplyResources(this.m_lblDescription, "m_lblDescription");
			this.m_lblDescription.AutoEllipsis = true;
			this.m_lblDescription.Name = "m_lblDescription";
			//
			// ImportXmlDialog
			//
			this.AcceptButton = this.m_btnImport;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btnCancel;
			this.ControlBox = false;
			this.Controls.Add(this.m_lblDescription);
			this.Controls.Add(this.m_btnHelp);
			this.Controls.Add(this.m_btnCancel);
			this.Controls.Add(this.m_btnImport);
			this.Controls.Add(this.m_btnBrowse);
			this.Controls.Add(this.m_tbFilename);
			this.Controls.Add(this.m_lblOXESFile);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ImportXmlDialog";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label m_lblOXESFile;
		private System.Windows.Forms.TextBox m_tbFilename;
		private System.Windows.Forms.Button m_btnBrowse;
		private System.Windows.Forms.Button m_btnImport;
		private System.Windows.Forms.Button m_btnCancel;
		private System.Windows.Forms.Button m_btnHelp;
		private System.Windows.Forms.Label m_lblDescription;
	}
}