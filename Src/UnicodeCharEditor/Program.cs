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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using SIL.LCModel.Core.Text;
using SIL.FieldWorks.Common.FwUtils;

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
			// needed to access proper registry values
			FwRegistryHelper.Initialize();
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			bool fBadArg = false;
			bool fInstall = false;
			for (int i = 0; i < args.Length; ++i)
			{
				if (args[i] == "-i" || args[i] == "-install" || args[i] == "--install")
					fInstall = true;
				else if (args[i] == "--cleanup")
				{
					if (i + 1 >= args.Length)
						return;

					var iterationCount = 0;
					var pid = int.Parse(args[i + 1]);
					while (Process.GetProcesses().Any(p => p.Id == pid) && iterationCount < 300)
					{
						// wait 1s then try again
						Thread.Sleep(1000);
						iterationCount++;
					}
					if (iterationCount < 300)
						DeleteTemporaryFiles();
					return;
				}
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

			LogFile.Release();

			StartCleanup();
		}

		private static void StartCleanup()
		{
			// Kick off cleanup. We run the same executable again with parameter "--cleanup".
			// This is necessary because the current process has locked some files so that we
			// can't delete them, and ICU doesn't release the locks while the process runs.
			using (var p = new Process())
			using (var currentProcess = Process.GetCurrentProcess())
			{
				p.StartInfo = new ProcessStartInfo
				{
					Arguments = string.Format("--cleanup {0}", currentProcess.Id),
					CreateNoWindow = true,
					FileName = typeof(Program).Assembly.Location,
					UseShellExecute = false
				};
				p.Start();
			}
		}

		private static void DeleteTemporaryFiles()
		{
			// Delete the files we previously renamed. Couldn't do that before because
			// they were locked.
			var tempFilesToDelete = Path.Combine(Icu.DefaultDirectory,
				string.Format("icudt{0}l", Icu.Version),
				"TempFilesToDelete");

			if (!File.Exists(tempFilesToDelete))
				return;

			var filesToDelete = File.ReadAllText(tempFilesToDelete);
			var lines = filesToDelete.Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
			foreach (var line in lines)
			{
				try
				{
					if (File.Exists(line))
						File.Delete(line);
				}
				catch
				{
					// just ignore
				}
			}
			File.Delete(tempFilesToDelete);
		}
	}
}
