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
	public partial class DictionaryConfigurationNodeRenameDlg : Form
	{
		public string NewName
		{
			get { return newName.Text; }
			set { newName.Text = value; }
		}

		public DictionaryConfigurationNodeRenameDlg()
		{
			InitializeComponent();
		}
	}
}
