using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace SilConvertersXML
{
	public partial class QueryFixedValueForm : Form
	{
		public QueryFixedValueForm(string strSampleValue)
		{
			InitializeComponent();
			textBoxSampleValue.Text = strSampleValue;
			textBoxFixedValue.Text = strSampleValue;
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			this.Close();
		}

		private void buttonCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			this.Close();
		}

		public string FixedValue
		{
			get { return textBoxFixedValue.Text; }
		}
	}
}