using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace DChartHelper
{
	public partial class QueryFreeTr : Form
	{
		public QueryFreeTr(string strLastValue)
		{
			InitializeComponent();
			this.textBoxFreeTr.Text = strLastValue;
		}

		public string FreeTranslation
		{
			get { return this.textBoxFreeTr.Text; }
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			this.Close();
		}
	}
}