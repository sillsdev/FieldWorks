// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: Program.cs
// Responsibility: mcconnel
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using SIL.FieldWorks.FixData;
using SIL.FieldWorks.Common.FwUtils;

namespace FixFwData
{
	class Program
	{
		[SuppressMessage("Gendarme.Rules.Portability", "ExitCodeIsLimitedOnUnixRule",
			Justification = "Appears to be a bug in Gendarme...not recognizing that 0 and 1 are in correct range (0..255)")]
		private static int Main(string[] args)
		{
			var pathname = args[0];
			var prog = new ConsoleProgress();
			var data = new FwDataFixer(pathname, prog, logger);
			data.FixErrorsAndSave();
			if (prog.DotsWritten)
				Console.WriteLine();
			if (errorsOccurred)
				return 1;
			return 0;
		}

		private static bool errorsOccurred;
		private static void logger(string guid, string date, string description)
		{
			Console.WriteLine(description);
			errorsOccurred = true;
		}
	}
}
