using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace DChartHelper
{
	public partial class FindWhatDialog : Form
	{
		public FindWhatDialog()
		{
			InitializeComponent();
		}

		public string FindWhat
		{
			get { return this.textBoxFindWhat.Text; }
			set { this.textBoxFindWhat.Text = value; }
		}

		private void buttonCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			this.Close();
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			this.Close();
		}
	}
}