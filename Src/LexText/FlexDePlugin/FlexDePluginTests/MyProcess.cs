// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: MyProcess.cs
// Responsibility: Greg Trihus
// Last reviewed:
//
// <remarks>
//		MyProcess - Methods to handle processes launched by tests
// </remarks>
// --------------------------------------------------------------------------------------------
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace FlexDePluginTests
{
	class MyProcess
	{
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification="See TODO-Linux comment")]
		public static bool KillProcess(string search)
		{
			bool foundProc = false;

			do
			{
				Process[] procs = Process.GetProcesses();
				foreach (Process proc in procs)
				{
					// TODO-Linux: MainWindowTitle always returns null on Mono which means this
					// code doesn't work on Linux.
					Match match = Regex.Match(proc.MainWindowTitle, search);
					if (match.Success)
					{
						proc.Kill();
						foundProc = true;
						break;
					}
				}
			} while (!foundProc);

			return foundProc;
		}
	}
}
