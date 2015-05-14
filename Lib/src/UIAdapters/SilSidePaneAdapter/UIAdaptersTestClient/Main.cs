// --------------------------------------------------------------------------------------------
// Copyright (c) 2009-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File:
// Responsibility:
// --------------------------------------------------------------------------------------------

using System;
using System.Windows.Forms;

namespace UIAdaptersTestClient
{
	/// <summary>
	/// Test program to aid in the development of SIBAdapter
	/// </summary>
	class MainClass
	{
		public static void Main(string[] args)
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new MainWindow());
		}
	}
}
