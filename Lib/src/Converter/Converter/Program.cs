using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.IO;
using ConvertLib;

namespace Converter
{
	class Program
	{
		/// <summary>
		/// The main entry point for the windows application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			ConvertLib.Convert cv = new ConvertLib.Convert();
			Form1 frm = new Form1();
			frm.ShowDialog();
			cv.m_FileName = frm.inFile;
			cv.m_OutFileName = frm.outFile;
			frm.Close();
			if (frm.DialogResult != DialogResult.OK)
			{
				Application.Exit();
			}

			try
			{
				cv.Conversion();
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message);
				return;
			}
		}
	}
}
