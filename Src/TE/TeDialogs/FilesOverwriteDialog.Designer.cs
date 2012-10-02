namespace SIL.FieldWorks.TE
{
	partial class FilesOverwriteDialog
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
			System.Windows.Forms.Label lblConfirmText;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FilesOverwriteDialog));
			System.Windows.Forms.Button btnYesToAll;
			System.Windows.Forms.Button btnNo;
			System.Windows.Forms.Button btnNoToAll;
			System.Windows.Forms.Button btnYes;
			this.lblFilename = new System.Windows.Forms.Label();
			lblConfirmText = new System.Windows.Forms.Label();
			btnYesToAll = new System.Windows.Forms.Button();
			btnNo = new System.Windows.Forms.Button();
			btnNoToAll = new System.Windows.Forms.Button();
			btnYes = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// lblConfirmText
			//
			resources.ApplyResources(lblConfirmText, "lblConfirmText");
			lblConfirmText.Name = "lblConfirmText";
			//
			// btnYesToAll
			//
			resources.ApplyResources(btnYesToAll, "btnYesToAll");
			btnYesToAll.Name = "btnYesToAll";
			btnYesToAll.UseVisualStyleBackColor = true;
			btnYesToAll.Click += new System.EventHandler(this.btnYesToAll_Click);
			//
			// btnNo
			//
			resources.ApplyResources(btnNo, "btnNo");
			btnNo.Name = "btnNo";
			btnNo.UseVisualStyleBackColor = true;
			btnNo.Click += new System.EventHandler(this.btnNo_Click);
			//
			// btnNoToAll
			//
			btnNoToAll.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(btnNoToAll, "btnNoToAll");
			btnNoToAll.Name = "btnNoToAll";
			btnNoToAll.UseVisualStyleBackColor = true;
			btnNoToAll.Click += new System.EventHandler(this.btnNoToAll_Click);
			//
			// btnYes
			//
			resources.ApplyResources(btnYes, "btnYes");
			btnYes.Name = "btnYes";
			btnYes.UseVisualStyleBackColor = true;
			btnYes.Click += new System.EventHandler(this.btnYes_Click);
			//
			// lblFilename
			//
			resources.ApplyResources(this.lblFilename, "lblFilename");
			this.lblFilename.Name = "lblFilename";
			//
			// FilesOverwriteDialog
			//
			this.AcceptButton = btnYesToAll;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = btnNoToAll;
			this.ControlBox = false;
			this.Controls.Add(btnYes);
			this.Controls.Add(btnNoToAll);
			this.Controls.Add(btnNo);
			this.Controls.Add(btnYesToAll);
			this.Controls.Add(lblConfirmText);
			this.Controls.Add(this.lblFilename);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FilesOverwriteDialog";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label lblFilename;
	}
}