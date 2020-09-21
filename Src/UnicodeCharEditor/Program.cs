// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using SIL.LCModel.Core.Text;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;

// ReSharper disable LocalizableElement -- Justification: we're cheap, and the messages in this file are displayed only in an error state.

namespace SIL.FieldWorks.UnicodeCharEditor
{
	/// <summary>
	/// The main program for UnicodeCharEditor.
	/// </summary>
	static class Program
	{
		/// <summary/>
		[STAThread]
		static void Main(string[] args)
		{
			Form window = null;
			var needCleanup = true;
			try
			{
				// needed to access proper registry values
				FwRegistryHelper.Initialize();
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);
				switch (args.FirstOrDefault())
				{
					case "-i":
					case "-install":
					case "--install":
						// If we have any custom character data, install it!
						FwUtils.InitializeIcu();
						var customCharsFile = CharEditorWindow.CustomCharsFile;
						if (File.Exists(customCharsFile))
						{
							new PUAInstaller().InstallPUACharacters(customCharsFile);
						}
						break;
					case "--cleanup":
						// If the second argument is a Process ID (int), wait up to five minutes for the proces to exit and then clean up;
						// otherwise, silently do nothing.
						needCleanup = false;
						int pid;
						if (int.TryParse(args.LastOrDefault(), out pid))
						{
							var iterationCount = 0;
							while (Process.GetProcesses().Any(p => p.Id == pid) && iterationCount < 300)
							{
								// wait 1s then try again
								Thread.Sleep(1000);
								iterationCount++;
							}

							if (iterationCount < 300)
								DeleteTemporaryFiles();
						}
						break;
					case null:
						// There were no arguments (the program was double-clicked or opened through the Start menu); run the graphical interface
						FwUtils.InitializeIcu();
						window = new CharEditorWindow();
						Application.Run(window);
						break;
					default:
						// An unrecognized argument was passed
						MessageBox.Show("Only one command line argument is recognized:" + Environment.NewLine +
										"\t-i means to install the custom character definitions (as a command line program).",
							"Unicode Character Editor");
						break;
				}
			}
			catch (ApplicationException ex)
			{
				MessageBox.Show(ex.Message, "Unicode Character Properties Editor");
			}
			catch (Exception ex)
			{
				// Be very, very careful about changing stuff here. Code here MUST not throw exceptions,
				// even when the application is in a crashed state.
				try
				{
					ErrorReporter.ReportException(ex, null, null, window, true);
				}
				catch
				{
					MessageBox.Show(ex.Message, "Unicode Character Properties Editor");
				}
			}
			finally
			{
				window?.Dispose();
				LogFile.Release();
				if (needCleanup)
				{
					StartCleanup();
				}
			}
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
					Arguments = $"--cleanup {currentProcess.Id}",
					CreateNoWindow = true,
					FileName = typeof(Program).Assembly.Location,
					UseShellExecute = false
				};
				p.Start();
			}
		}

		private static void DeleteTemporaryFiles()
		{
			// Delete the files we previously renamed. Couldn't do that before because they were locked.
			var tempFilesToDelete = Path.Combine(CustomIcu.DefaultDataDirectory,
				$"icudt{CustomIcu.Version}l", "TempFilesToDelete");

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
