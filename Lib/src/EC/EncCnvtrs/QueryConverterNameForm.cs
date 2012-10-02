using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace SilEncConverters31
{
	public partial class QueryConverterNameForm : Form
	{
		public QueryConverterNameForm(string strFriendlyName)
		{
			InitializeComponent();

			textBoxFriendlyName.Text = strFriendlyName;
		}

		public string FriendlyName
		{
			get { return textBoxFriendlyName.Text; }
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			this.Close();
		}
	}
}