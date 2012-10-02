using System;
using System.Security;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Security.Principal;

namespace BackupScheduler
{
	public partial class BackupSchedulePasswordDlg : Form
	{
		public SecureString Password
		{
			get { return PasswordEditBox.SecureText; }
		}

		public BackupSchedulePasswordDlg()
		{
			InitializeComponent();
			this.LogonNameText.Text = WindowsIdentity.GetCurrent().Name.ToString();
		}

		private void OnOK(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			Close();
		}

		private void OnCancel(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void PasswordEditBox_TextChanged(object sender, EventArgs e)
		{

		}
	}
}
