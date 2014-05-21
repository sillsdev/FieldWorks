// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SIL.FieldWorks.XWorks
{
	public partial class PublishToWebonaryDlg : Form
	{
		public PublishToWebonaryDlg()
		{
			InitializeComponent();
		}

		private void showPasswordCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			if (showPasswordCheckBox.Checked)
				webonaryPasswordTextbox.PasswordChar = '\0';
			else
				webonaryPasswordTextbox.PasswordChar = '*';
		}
	}
}
