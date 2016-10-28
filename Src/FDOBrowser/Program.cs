// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwKernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.WritingSystems;

namespace FDOBrowser
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			// initialize ICU
			Icu.InitIcuDataDir();
			Sldr.Initialize();
			RegistryHelper.ProductName = "FieldWorks"; // inorder to find correct Registry keys
			using (var form = new FDOBrowserForm())
			{
				Application.Run(form);
			}

			Sldr.Cleanup();
		}
	}
}