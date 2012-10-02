namespace SIL.FieldWorks.LexText.Controls
{
#if WANTPORT // FWR-2845; this was not enabled in 6.0 and may be superseded by Randy's LiftBridge.
	partial class LiftSynchronizeDlg
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LiftSynchronizeDlg));
			this.m_tbLiftFile = new System.Windows.Forms.TextBox();
			this.m_btnBrowse = new System.Windows.Forms.Button();
			this.m_saveFileDialog = new System.Windows.Forms.SaveFileDialog();
			this.m_btnSynch = new System.Windows.Forms.Button();
			this.m_label1 = new System.Windows.Forms.Label();
			this.m_btnCancel = new System.Windows.Forms.Button();
			this.m_label2 = new System.Windows.Forms.Label();
			this.m_tbSynchSource = new System.Windows.Forms.TextBox();
			this.m_btnBrowse2 = new System.Windows.Forms.Button();
			this.m_btnHelp = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// m_tbLiftFile
			//
			resources.ApplyResources(this.m_tbLiftFile, "m_tbLiftFile");
			this.m_tbLiftFile.Name = "m_tbLiftFile";
			//
			// m_btnBrowse
			//
			resources.ApplyResources(this.m_btnBrowse, "m_btnBrowse");
			this.m_btnBrowse.Name = "m_btnBrowse";
			this.m_btnBrowse.UseVisualStyleBackColor = true;
			this.m_btnBrowse.Click += new System.EventHandler(this.m_btnBrowse_Click);
			//
			// m_btnSynch
			//
			resources.ApplyResources(this.m_btnSynch, "m_btnSynch");
			this.m_btnSynch.Name = "m_btnSynch";
			this.m_btnSynch.UseVisualStyleBackColor = true;
			this.m_btnSynch.Click += new System.EventHandler(this.m_btnSynch_Click);
			//
			// m_label1
			//
			resources.ApplyResources(this.m_label1, "m_label1");
			this.m_label1.Name = "m_label1";
			//
			// m_btnCancel
			//
			this.m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(this.m_btnCancel, "m_btnCancel");
			this.m_btnCancel.Name = "m_btnCancel";
			this.m_btnCancel.UseVisualStyleBackColor = true;
			this.m_btnCancel.Click += new System.EventHandler(this.m_btnCancel_Click);
			//
			// m_label2
			//
			resources.ApplyResources(this.m_label2, "m_label2");
			this.m_label2.Name = "m_label2";
			//
			// m_tbSynchSource
			//
			resources.ApplyResources(this.m_tbSynchSource, "m_tbSynchSource");
			this.m_tbSynchSource.Name = "m_tbSynchSource";
			//
			// m_btnBrowse2
			//
			resources.ApplyResources(this.m_btnBrowse2, "m_btnBrowse2");
			this.m_btnBrowse2.Name = "m_btnBrowse2";
			this.m_btnBrowse2.UseVisualStyleBackColor = true;
			this.m_btnBrowse2.Click += new System.EventHandler(this.m_btnBrowse2_Click);
			//
			// m_btnHelp
			//
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			this.m_btnHelp.Name = "m_btnHelp";
			this.m_btnHelp.UseVisualStyleBackColor = true;
			this.m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// LiftSynchronizeDlg
			//
			this.AcceptButton = this.m_btnSynch;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btnCancel;
			this.Controls.Add(this.m_btnHelp);
			this.Controls.Add(this.m_btnBrowse2);
			this.Controls.Add(this.m_tbSynchSource);
			this.Controls.Add(this.m_label2);
			this.Controls.Add(this.m_btnCancel);
			this.Controls.Add(this.m_label1);
			this.Controls.Add(this.m_btnSynch);
			this.Controls.Add(this.m_btnBrowse);
			this.Controls.Add(this.m_tbLiftFile);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "LiftSynchronizeDlg";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox m_tbLiftFile;
		private System.Windows.Forms.Button m_btnBrowse;
		private System.Windows.Forms.SaveFileDialog m_saveFileDialog;
		private System.Windows.Forms.Button m_btnSynch;
		private System.Windows.Forms.Label m_label1;
		private System.Windows.Forms.Button m_btnCancel;
		private System.Windows.Forms.Label m_label2;
		private System.Windows.Forms.TextBox m_tbSynchSource;
		private System.Windows.Forms.Button m_btnBrowse2;
		private System.Windows.Forms.Button m_btnHelp;
	}
#endif
}