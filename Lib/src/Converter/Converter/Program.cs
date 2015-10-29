// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

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