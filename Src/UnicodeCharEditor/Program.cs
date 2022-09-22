// Copyright (c) 2010-2021 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using CommandLineParser.Arguments;
using CommandLineParser.Exceptions;
using CommandLineParser.Validation;
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
			var commandLineParser = new CommandLineParser.CommandLineParser
			{
				AcceptHyphen = true, AcceptSlash = true, AcceptEqualSignSyntaxForValueArguments = true, IgnoreCase = true
			};
			var installArg = new SwitchArgument('i', "install", false)
			{
				Description =
					"Install the data from the CustomChars.xml file into the ICU data folder"
			};
			var loggingArg = new SwitchArgument('l', "log", false);
			var verboseArg = new SwitchArgument('v', "verbose", false);
			var cleanupArg = new ValueArgument<int>('c', "cleanup",
				"Cleans up icu files that were probably locked, usually not to be called manually.");
			commandLineParser.Arguments.Add(installArg);
			commandLineParser.Arguments.Add(loggingArg);
			commandLineParser.Arguments.Add(verboseArg);
			commandLineParser.Arguments.Add(cleanupArg);
			commandLineParser.Certifications.Add(new ArgumentGroupCertification(new Argument[] { cleanupArg, installArg },
				EArgumentGroupCondition.OneOreNoneUsed));
			Form window = null;
			var needCleanup = true;
			try
			{
				commandLineParser.ParseCommandLine(args);
				if (!commandLineParser.ParsingSucceeded)
				{
					using (var stringWriter = new StringWriter())
					{
						commandLineParser.PrintUsage(stringWriter);
						MessageBoxUtils.Show(stringWriter.ToString());
						return;
					}
				}
				if (loggingArg.Parsed)
				{
					LogFile.IsLogging = true;
				}

				if (verboseArg.Parsed)
				{
					LogFile.IsLogging = true;
					LogFile.IsVerbose = true;
				}

				// needed to access proper registry values
				FwRegistryHelper.Initialize();
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);
				if (installArg.Parsed)
				{
					// If we have any custom character data, install it!
					FwUtils.InitializeIcu();
					var customCharsFile = CharEditorWindow.CustomCharsFile;
					if (File.Exists(customCharsFile))
					{
						new PUAInstaller().InstallPUACharacters(customCharsFile);
					}
				}
				else if (cleanupArg.Parsed)
				{
					// If the second argument is a Process ID (int), wait up to five minutes for the process to exit and then clean up;
					// otherwise, silently do nothing.
					needCleanup = false;
					var iterationCount = 0;
					while (Process.GetProcesses().Any(p => p.Id == cleanupArg.Value) &&
						   iterationCount < 300)
					{
						// wait 1s then try again
						Thread.Sleep(1000);
						iterationCount++;
					}

					if (iterationCount < 300)
					{
						DeleteTemporaryFiles();
					}
				}
				else
				{
					// There were no arguments (the program was double-clicked or opened through the Start menu); run the graphical interface
					FwUtils.InitializeIcu();
					window = new CharEditorWindow();
					Application.Run(window);
				}
			}
			catch (CommandLineException cle)
			{
				MessageBoxUtils.Show(cle.Message, "Unicode Character Properties Editor");
			}
			catch (ApplicationException ex)
			{
				MessageBoxUtils.Show(ex.Message, "Unicode Character Properties Editor");
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
					MessageBoxUtils.Show(ex.Message, "Unicode Character Properties Editor");
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
			var tempFilesToDelete = Path.Combine(PUAInstaller.IcuDir,
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
