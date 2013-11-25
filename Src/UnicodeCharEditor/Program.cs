// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: Program.cs
// Responsibility: mcconnel
//
// <remarks>
// </remarks>

using System;
using System.IO;
using System.Windows.Forms;
using SIL.Utils;

namespace SIL.FieldWorks.UnicodeCharEditor
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// The main program for UnicodeCharEditor.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	static class Program
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[STAThread]
		static void Main(string[] args)
		{
			RegistryHelper.ProductName = "FieldWorks";
			// needed to access proper registry values
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			bool fBadArg = false;
			bool fInstall = false;
			for (int i = 0; i < args.Length; ++i)
			{
				if (args[i] == "-i" || args[i] == "-install" || args[i] == "--install")
					fInstall = true;
				else
					fBadArg = true;
			}
			if (fInstall)
			{
				PUAMigrator migrator = new PUAMigrator();
				migrator.Run();
			}
			else if (fBadArg)
			{
				MessageBox.Show("Only one command line argument is recognized:" + Environment.NewLine +
					"\t-i means to install the custom character definitions (as a command line program).",
					"Unicode Character Editor");
			}
			else
			{
				using (var window = new CharEditorWindow())
					Application.Run(window);
			}
		}
	}
}