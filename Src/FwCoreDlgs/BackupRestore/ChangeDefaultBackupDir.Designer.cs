namespace SIL.FieldWorks.FwCoreDlgs.BackupRestore
{
	partial class ChangeDefaultBackupDir
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ChangeDefaultBackupDir));
			this.label_message = new System.Windows.Forms.Label();
			this.button_Help = new System.Windows.Forms.Button();
			this.button_No = new System.Windows.Forms.Button();
			this.button_Yes = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// label_message
			//
			resources.ApplyResources(this.label_message, "label_message");
			this.label_message.Name = "label_message";
			//
			// button_Help
			//
			resources.ApplyResources(this.button_Help, "button_Help");
			this.button_Help.Name = "button_Help";
			this.button_Help.UseVisualStyleBackColor = true;
			this.button_Help.Click += new System.EventHandler(this.button_Help_Click);
			//
			// button_No
			//
			this.button_No.DialogResult = System.Windows.Forms.DialogResult.No;
			resources.ApplyResources(this.button_No, "button_No");
			this.button_No.Name = "button_No";
			this.button_No.UseVisualStyleBackColor = true;
			//
			// button_Yes
			//
			this.button_Yes.DialogResult = System.Windows.Forms.DialogResult.Yes;
			resources.ApplyResources(this.button_Yes, "button_Yes");
			this.button_Yes.Name = "button_Yes";
			this.button_Yes.UseVisualStyleBackColor = true;
			//
			// ChangeDefaultBackupDir
			//
			this.AcceptButton = this.button_Yes;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.button_No;
			this.Controls.Add(this.button_Yes);
			this.Controls.Add(this.label_message);
			this.Controls.Add(this.button_Help);
			this.Controls.Add(this.button_No);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ChangeDefaultBackupDir";
			this.ShowIcon = false;
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Label label_message;
		private System.Windows.Forms.Button button_Help;
		private System.Windows.Forms.Button button_No;
		private System.Windows.Forms.Button button_Yes;
	}
}