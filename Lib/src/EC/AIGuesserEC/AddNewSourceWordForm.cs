using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SilEncConverters40
{
	public partial class AddNewSourceWordForm : Form
	{
		public AddNewSourceWordForm()
		{
			InitializeComponent();
		}

		public string WordAdded
		{
			get { return textBoxWordToAdd.Text.Trim(); }
			set { textBoxWordToAdd.Text = value.Trim(); }
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			if (String.IsNullOrEmpty(WordAdded))
			{
				MessageBox.Show(Properties.Resources.IDS_CantHaveEmptySourceWord,
								EncConverters.cstrCaption);
				return;
			}

			DialogResult = DialogResult.OK;
			Close();
		}
	}
}
