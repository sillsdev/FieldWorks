// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
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
