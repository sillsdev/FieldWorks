using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace SilEncConverters40
{
	public partial class ElementPicker : Form
	{
		public ElementPicker(string strHtmlSource, string strFindPattern, string strDefaultValue)
		{
			InitializeComponent();

			Regex regFindElements = new Regex(strFindPattern);
			MatchCollection mc = regFindElements.Matches(strHtmlSource);
			if (mc.Count > 0)
			{
				foreach (Match match in mc)
				{
					listBoxElements.Items.Add(match.Groups[1]);
				}

				listBoxElements.SelectedIndex = listBoxElements.FindStringExact(strDefaultValue);
			}
		}

		public string SelectedElement;

		private void buttonOK_Click(object sender, EventArgs e)
		{
			if (listBoxElements.SelectedItem == null)
			{
				MessageBox.Show("First select an element", EncConverters.cstrCaption);
				return;
			}

			SelectedElement = listBoxElements.SelectedItem.ToString();
			DialogResult = DialogResult.OK;
			Close();
		}

		private void listBoxElements_DoubleClick(object sender, EventArgs e)
		{
			buttonOK_Click(sender, e);
		}

		private void listBoxElements_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (listBoxElements.SelectedItem != null)
				buttonOK.Enabled = true;
		}
	}
}
