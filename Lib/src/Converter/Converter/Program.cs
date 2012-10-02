using System;
using System.Collections.Generic;
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
			Form1 frm = new Form1();
			frm.ShowDialog();
			if (frm.DialogResult == DialogResult.OK)
			{
				Application.Exit();
			}
		}
	}
}