using System.Security.Principal;
using SecurePasswordTextBox;

namespace BackupScheduler
{
	partial class BackupSchedulePasswordDlg
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
			System.Windows.Forms.Label PwdReqdLabel;
			System.Windows.Forms.Label LogonNameLabel;
			System.Windows.Forms.Label EnterPwdText;
			System.Windows.Forms.Label WarningText;
			System.Windows.Forms.Label WarningTitleText;
			System.Security.SecureString secureString1 = new System.Security.SecureString();
			this.LogonNameText = new System.Windows.Forms.Label();
			this.PasswordEditBox = new SecurePasswordTextBox.SecureTextBox();
			this.ButtonOk = new System.Windows.Forms.Button();
			this.ButtonCancel = new System.Windows.Forms.Button();
			this.ButtonHelp = new System.Windows.Forms.Button();
			PwdReqdLabel = new System.Windows.Forms.Label();
			LogonNameLabel = new System.Windows.Forms.Label();
			EnterPwdText = new System.Windows.Forms.Label();
			WarningText = new System.Windows.Forms.Label();
			WarningTitleText = new System.Windows.Forms.Label();
			this.SuspendLayout();
			//
			// PwdReqdLabel
			//
			PwdReqdLabel.CausesValidation = false;
			PwdReqdLabel.Location = new System.Drawing.Point(9, 11);
			PwdReqdLabel.Name = "PwdReqdLabel";
			PwdReqdLabel.Size = new System.Drawing.Size(277, 38);
			PwdReqdLabel.TabIndex = 0;
			PwdReqdLabel.Text = "Windows requires that you supply your logon password in order to run a scheduled " +
				"task.";
			//
			// LogonNameLabel
			//
			LogonNameLabel.AutoSize = true;
			LogonNameLabel.Location = new System.Drawing.Point(16, 54);
			LogonNameLabel.Name = "LogonNameLabel";
			LogonNameLabel.Size = new System.Drawing.Size(111, 13);
			LogonNameLabel.TabIndex = 1;
			LogonNameLabel.Text = "You are logged on as:";
			//
			// EnterPwdText
			//
			EnterPwdText.Location = new System.Drawing.Point(12, 81);
			EnterPwdText.Name = "EnterPwdText";
			EnterPwdText.Size = new System.Drawing.Size(139, 18);
			EnterPwdText.TabIndex = 2;
			EnterPwdText.Text = "Enter your logon password:";
			EnterPwdText.TextAlign = System.Drawing.ContentAlignment.TopRight;
			//
			// WarningText
			//
			WarningText.Location = new System.Drawing.Point(63, 109);
			WarningText.Name = "WarningText";
			WarningText.Size = new System.Drawing.Size(223, 45);
			WarningText.TabIndex = 4;
			WarningText.Text = "If you change your Windows password, the scheduled backup will no longer run. You" +
				" will need to reschedule your automatic backup.";
			//
			// WarningTitleText
			//
			WarningTitleText.AutoSize = true;
			WarningTitleText.ForeColor = System.Drawing.Color.Red;
			WarningTitleText.Location = new System.Drawing.Point(12, 109);
			WarningTitleText.Name = "WarningTitleText";
			WarningTitleText.Size = new System.Drawing.Size(50, 13);
			WarningTitleText.TabIndex = 8;
			WarningTitleText.Text = "Warning:";
			WarningTitleText.TextAlign = System.Drawing.ContentAlignment.TopRight;
			//
			// LogonNameText
			//
			this.LogonNameText.Location = new System.Drawing.Point(126, 54);
			this.LogonNameText.Name = "LogonNameText";
			this.LogonNameText.Size = new System.Drawing.Size(160, 14);
			this.LogonNameText.TabIndex = 2;
			//
			// PasswordEditBox
			//
			this.PasswordEditBox.Location = new System.Drawing.Point(152, 77);
			this.PasswordEditBox.Name = "PasswordEditBox";
			this.PasswordEditBox.PasswordChar = '*';
			this.PasswordEditBox.SecureText = secureString1;
			this.PasswordEditBox.Size = new System.Drawing.Size(118, 20);
			this.PasswordEditBox.TabIndex = 3;
			this.PasswordEditBox.UseSystemPasswordChar = true;
			this.PasswordEditBox.TextChanged += new System.EventHandler(this.PasswordEditBox_TextChanged);
			//
			// ButtonOk
			//
			this.ButtonOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.ButtonOk.Location = new System.Drawing.Point(23, 168);
			this.ButtonOk.Name = "ButtonOk";
			this.ButtonOk.Size = new System.Drawing.Size(75, 23);
			this.ButtonOk.TabIndex = 5;
			this.ButtonOk.Text = "OK";
			this.ButtonOk.UseVisualStyleBackColor = true;
			this.ButtonOk.Click += new System.EventHandler(this.OnOK);
			//
			// ButtonCancel
			//
			this.ButtonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.ButtonCancel.Location = new System.Drawing.Point(112, 168);
			this.ButtonCancel.Name = "ButtonCancel";
			this.ButtonCancel.Size = new System.Drawing.Size(75, 23);
			this.ButtonCancel.TabIndex = 6;
			this.ButtonCancel.Text = "Cancel";
			this.ButtonCancel.UseVisualStyleBackColor = true;
			this.ButtonCancel.Click += new System.EventHandler(this.OnCancel);
			//
			// ButtonHelp
			//
			this.ButtonHelp.Location = new System.Drawing.Point(201, 168);
			this.ButtonHelp.Name = "ButtonHelp";
			this.ButtonHelp.Size = new System.Drawing.Size(75, 23);
			this.ButtonHelp.TabIndex = 7;
			this.ButtonHelp.Text = "Help";
			this.ButtonHelp.UseVisualStyleBackColor = true;
			//
			// BackupSchedulePasswordDlg
			//
			this.AcceptButton = this.ButtonOk;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(298, 199);
			this.Controls.Add(WarningTitleText);
			this.Controls.Add(this.ButtonHelp);
			this.Controls.Add(this.ButtonCancel);
			this.Controls.Add(this.ButtonOk);
			this.Controls.Add(WarningText);
			this.Controls.Add(this.PasswordEditBox);
			this.Controls.Add(EnterPwdText);
			this.Controls.Add(this.LogonNameText);
			this.Controls.Add(LogonNameLabel);
			this.Controls.Add(PwdReqdLabel);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "BackupSchedulePasswordDlg";
			this.ShowIcon = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Backup Schedule Password";
			this.TopMost = true;
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label LogonNameText;
		private SecureTextBox PasswordEditBox;
		private System.Windows.Forms.Button ButtonOk;
		private System.Windows.Forms.Button ButtonCancel;
		private System.Windows.Forms.Button ButtonHelp;
	}
}
