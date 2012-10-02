using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace DChartHelper
{
	public partial class AddKBEntry : Form
	{
		public AddKBEntry(string strSource, string strTarget, Font fontSource, Font fontTarget)
		{
			InitializeComponent();

			textBoxSource.Font = new Font(fontSource.FontFamily, 14);
			textBoxTarget.Font = new Font(fontTarget.FontFamily, 14);
			TrimWhiteSpaceAndPunctuation(ref strSource);
			Source = strSource;
		}

		protected void TrimWhiteSpaceAndPunctuation(ref string strWord)
		{
			char ch = (char)0;
			while ((strWord.Length > 0)
				&& (((ch = strWord[0]) <= 0x0020)
					|| Char.IsDigit(ch)
					|| Char.IsPunctuation(ch)))
				strWord = strWord.Remove(0, 1);

			if (!String.IsNullOrEmpty(strWord))
			{
				int i = strWord.Length - 1;
				while (((ch = strWord[i]) <= 0x0020)
					|| Char.IsDigit(ch)
					|| Char.IsPunctuation(ch))
					strWord = strWord.Remove(i--);
			}
		}

		public string Source
		{
			get { return this.textBoxSource.Text; }
			set { this.textBoxSource.Text = value; }
		}

		public string Target
		{
			get { return this.textBoxTarget.Text; }
			set { this.textBoxTarget.Text = value; }
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			this.Close();
		}
	}
}