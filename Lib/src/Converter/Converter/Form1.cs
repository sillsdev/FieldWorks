using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Converter
{
	public partial class Form1 : Form
	{
		public string inFile = "";
		public string outFile = "";

		public Form1()
		{
			InitializeComponent();
		}

		private void Form1_Load(object sender, EventArgs e)
		{

		}

		private void BrowseBtn_Click(object sender, EventArgs e)
		{
			Int32 pos;

			OpenFileDialog fdlg = new OpenFileDialog();
			fdlg.Title = "C# Open File Dialog";
			fdlg.InitialDirectory = @"My Documents";
			fdlg.Filter = "XML file|*.xml";
			fdlg.FilterIndex = 2;
			fdlg.RestoreDirectory = true;
			if (fdlg.ShowDialog() == DialogResult.OK)
			{
				InputText.Text = fdlg.FileName;
				// If the output filename is empty, fill in a default.
				pos = fdlg.FileName.IndexOf(".xml");

				if (fdlg.FileName.Length == pos + 4)
				{
					OutputText.Text = fdlg.FileName.Substring(0, pos) + "Out.fwdata";
				}
			}
		}

		private void ConvertBtn_Click(object sender, EventArgs e) //Do the Conversion
		{
			inFile = this.InputText.Text;
			outFile = this.OutputText.Text;
			this.DialogResult = DialogResult.OK;
			return;
		}

		private void ExitBtn_Click(object sender, EventArgs e)
		{
			Application.Exit();
		}
	}
}
