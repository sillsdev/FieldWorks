namespace SIL.FieldWorks.TE.TeEditorialChecks
{
	partial class CheckingErrorCommentDlg
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
			System.Windows.Forms.Button btnHelp;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CheckingErrorCommentDlg));
			System.Windows.Forms.Button btnCancel;
			System.Windows.Forms.Button btnOK;
			this.lblComment = new System.Windows.Forms.Label();
			this.pnlTextBox = new System.Windows.Forms.Panel();
			this.label1 = new System.Windows.Forms.Label();
			btnHelp = new System.Windows.Forms.Button();
			btnCancel = new System.Windows.Forms.Button();
			btnOK = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// btnHelp
			//
			resources.ApplyResources(btnHelp, "btnHelp");
			btnHelp.Name = "btnHelp";
			btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
			//
			// btnCancel
			//
			resources.ApplyResources(btnCancel, "btnCancel");
			btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			btnCancel.Name = "btnCancel";
			//
			// btnOK
			//
			resources.ApplyResources(btnOK, "btnOK");
			btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			btnOK.Name = "btnOK";
			//
			// lblComment
			//
			resources.ApplyResources(this.lblComment, "lblComment");
			this.lblComment.Name = "lblComment";
			//
			// pnlTextBox
			//
			resources.ApplyResources(this.pnlTextBox, "pnlTextBox");
			this.pnlTextBox.Name = "pnlTextBox";
			//
			// label1
			//
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			//
			// CheckingErrorCommentDlg
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = btnCancel;
			this.Controls.Add(this.label1);
			this.Controls.Add(this.pnlTextBox);
			this.Controls.Add(this.lblComment);
			this.Controls.Add(btnOK);
			this.Controls.Add(btnCancel);
			this.Controls.Add(btnHelp);
			this.DoubleBuffered = true;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "CheckingErrorCommentDlg";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label lblComment;
		private System.Windows.Forms.Panel pnlTextBox;
		private System.Windows.Forms.Label label1;
	}
}