// Copyright (c) 2016-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Windows.Forms;
using SIL.LCModel.Core.Text;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Utils;
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
			FwRegistryHelper.Initialize();
			InitializeIcu();
			Sldr.Initialize();
			using (var form = new FDOBrowserForm())
			{
				Application.Run(form);
			}

			Sldr.Cleanup();
		}

		private static void InitializeIcu()
		{
			if (MiscUtils.IsWindows)
			{
				var arch = Environment.Is64BitProcess ? "x64" : "x86";
				var icuPath = Path.Combine(Path.GetDirectoryName(FwDirectoryFinder.FlexExe), "lib", arch);
				// Append icu dll location to PATH, such as .../lib/x64, to help C# and C++ code find icu.
				Environment.SetEnvironmentVariable("PATH",
					Environment.GetEnvironmentVariable("PATH") + Path.PathSeparator + icuPath);
			}

			Icu.InitIcuDataDir();
		}
	}
}