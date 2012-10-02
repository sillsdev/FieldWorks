/// --------------------------------------------------------------------------------------------
#region /// Copyright (c) 2002, SIL International. All Rights Reserved.
/// <copyright from='2002' to='2002' company='SIL International'>
///		Copyright (c) 2002, SIL International. All Rights Reserved.
///
///		Distributable under the terms of either the Common Public License or the
///		GNU Lesser General Public License, as specified in the LICENSING.txt file.
/// </copyright>
#endregion
///
/// File: SlnOpClose.cs
/// Responsibility: Eberhard Beilharz
/// Last reviewed:
///
/// <remarks>
/// Opens and closes a solution in Visual Studio from a batch file.
/// </remarks>
/// --------------------------------------------------------------------------------------------

using System;
using EnvDTE;
using System.Runtime.InteropServices;
using System.IO;
using System.Windows.Forms;

namespace SIL.FieldWorks.Tools
{
	/// <summary>
	/// Opens and closes a solution in Visual Studio .NET from a batch file.
	/// </summary>
	class SlnOpclose
	{
		private enum Command
		{
			ShowHelp,
			Open,
			Close,
			CloseNet
		}
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static int Main(string[] args)
		{
			try
			{
				Command cmd = Command.ShowHelp;
				if (args.Length > 0)
				{
					string arg0 = args[0].Substring(1).ToLower();
					switch (arg0)
					{
						case "?":
							cmd = Command.ShowHelp;
							break;
						case "open":
							cmd = Command.Open;
							break;
						case "close":
							cmd = Command.Close;
							break;
						case "closenet":
							cmd = Command.CloseNet;
							break;
					}
				}

				if (cmd == Command.ShowHelp)
				{
					Console.WriteLine("\nSlnOpClose. Opens/Closes a solution in Visual Studio .NET from a batch file.");
					Console.WriteLine("Copyright (c) 2002, SIL International. All Rights Reserved.\n");
					Console.WriteLine("Usage: {0} [options]",
						Path.GetFileName(Application.ExecutablePath));
					Console.WriteLine("/open\t\topens a previously opened solution");
					Console.WriteLine("/close\t\tcloses the solution that is open in Visual Studio (saves first)");
					Console.WriteLine("/closenet\tcloses the solution only if it contains .NET projects");
					return 0;
				}

				bool fCloseSolution = false;
				DTE dte;
				try
				{
					dte = (DTE)Marshal.GetActiveObject("VisualStudio.DTE.7");
				}
				catch(COMException e)
				{
					if (e.ErrorCode == -2147221021) // Operation unavailable
					{
						// we can ignore this exception - it simply means that Visual Studio isn't running
						Console.WriteLine("VisualStudio isn't running.");
						return 0;
					}

					System.Console.WriteLine("Internal program error in program {0}", e.Source);
					System.Console.WriteLine("\nDetails:\n{0}\nin method {1}.{2}\nStack trace:\n{3}",
						e.Message, e.TargetSite.DeclaringType.Name, e.TargetSite.Name, e.StackTrace);

					return 1;
				}
				if (dte != null)
				{
					if (cmd == Command.Open)
					{
						if (dte.Globals.get_VariableExists("LastOpenSolution"))
						{
							string lastOpenSolution = (string)dte.Globals["LastOpenSolution"];
							if (lastOpenSolution != null && lastOpenSolution != string.Empty)
							{
								Console.WriteLine("Opening solution {0}...", lastOpenSolution);
								dte.Solution.Open(lastOpenSolution);
							}
						}
					}
					else if (cmd == Command.CloseNet)
					{
						foreach(Project prj in dte.Solution.Projects)
						{
							if (prj.ConfigurationManager != null)
							{
								Array platformNames = (Array)prj.ConfigurationManager.PlatformNames;
								Array.Sort(platformNames);

								if (Array.BinarySearch(platformNames, ".NET") >= 0)
								{
									fCloseSolution = true;
									break;
								}
							}
						}
					}
					else
					{
						fCloseSolution = true;
					}

					if (fCloseSolution)
					{
						Console.WriteLine("Closing solution {0}...", dte.Solution.FullName);
						dte.Globals["LastOpenSolution"] = dte.Solution.FullName;
						dte.Solution.Close(true);
					}
					else
						dte.Globals["LastOpenSolution"] = null;
				}
			}
			catch(Exception e)
			{
				System.Console.WriteLine("Internal program error in program {0}", e.Source);
				System.Console.WriteLine("\nDetails:\n{0}\nin method {1}.{2}\nStack trace:\n{3}",
					e.Message, e.TargetSite.DeclaringType.Name, e.TargetSite.Name, e.StackTrace);

				return 1;
			}

			return 0;
		}
	}
}
