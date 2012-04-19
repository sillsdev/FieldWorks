namespace SIL.FieldWorks.FDO.Infrastructure.Impl
{
	partial class ConnectionLostDlg
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
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + ". ****** ");
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConnectionLostDlg));
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.m_btnYes = new System.Windows.Forms.Button();
			this.m_btnExit = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// label1
			//
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			//
			// label2
			//
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			//
			// m_btnYes
			//
			this.m_btnYes.DialogResult = System.Windows.Forms.DialogResult.Yes;
			resources.ApplyResources(this.m_btnYes, "m_btnYes");
			this.m_btnYes.Name = "m_btnYes";
			this.m_btnYes.UseVisualStyleBackColor = true;
			//
			// m_btnExit
			//
			this.m_btnExit.DialogResult = System.Windows.Forms.DialogResult.Abort;
			resources.ApplyResources(this.m_btnExit, "m_btnExit");
			this.m_btnExit.Name = "m_btnExit";
			this.m_btnExit.UseVisualStyleBackColor = true;
			//
			// ConnectionLostDlg
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ControlBox = false;
			this.Controls.Add(this.m_btnExit);
			this.Controls.Add(this.m_btnYes);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ConnectionLostDlg";
			this.ShowIcon = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Button m_btnYes;
		private System.Windows.Forms.Button m_btnExit;
	}
}