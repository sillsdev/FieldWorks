using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace SilConvertersXML
{
	public partial class XPathFilterForm : Form
	{
		public XPathFilterForm(string strFilterExpression)
		{
			InitializeComponent();
			textBoxXPathExpression.Text = strFilterExpression;
		}

		public string FilterExpression
		{
			get { return textBoxXPathExpression.Text; }
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

		private void recentToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
		{
			recentToolStripMenuItem.DropDownItems.Clear();
			foreach (string strRecentExpression in Properties.Settings.Default.RecentFilters)
				recentToolStripMenuItem.DropDownItems.Add(strRecentExpression, null, recentToolStripMenuItem_Click);

		}

		void recentToolStripMenuItem_Click(object sender, System.EventArgs e)
		{
			ToolStripDropDownItem aRecentExpression = (ToolStripDropDownItem)sender;
			textBoxXPathExpression.Text = aRecentExpression.Text;
		}
	}
}