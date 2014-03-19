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
		/// <summary>
		/// Insert value into description in dialog.
		/// </summary>
		public string DisplayLabel
		{
			set { description.Text = description.Text.Replace("%s", value); }
		}

		public string NewSuffix
		{
			get { return newSuffix.Text; }
			set { newSuffix.Text = value; }
		}

		public DictionaryConfigurationNodeRenameDlg()
		{
			InitializeComponent();
		}
	}
}
