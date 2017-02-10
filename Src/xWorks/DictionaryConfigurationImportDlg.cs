// Copyright (c) 2017 SIL International
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
	public partial class DictionaryConfigurationImportDlg : Form
	{
		public DictionaryConfigurationImportDlg()
		{
			InitializeComponent();
		}

		private void cancelButton_Click(object sender, EventArgs e)
		{
			Close();
		}
	}
}
