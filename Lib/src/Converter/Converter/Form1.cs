using System;
using System.Diagnostics;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Converter
{
	public partial class Form1 : Form
	{
		public string inFile = "";
		public string outFile = "";
		ConvertLib.Convert cv = new ConvertLib.Convert();

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
			cv.m_FileName = this.InputText.Text;
			cv.m_OutFileName = this.OutputText.Text;
			try
			{
				Thread t = new Thread (cv.Conversion);
				t.Start();							// Run the conversion process as a thread.
				t.Join();							// Wait until the conversion process (thread) is complete.
				this.DialogResult = DialogResult.OK;
				return;
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message);
				return;
			}
		}

		private void ExitBtn_Click(object sender, EventArgs e)
		{
			Application.Exit();
		}

		public bool IsProcessOpen(string name)
		{
			// all running processes on the computer
			foreach (Process clsProcess in Process.GetProcesses())
			{
				if (clsProcess.ProcessName.Contains(name))
				{
					//if the process is running, return true
					return true;
				}
			}
			//otherwise we return a false
			return false;
		}
	}
}
