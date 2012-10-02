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
using SIL.FieldWorks.FixData;
using SIL.FieldWorks.Common.FwUtils;

namespace FixFwData
{
	class Program
	{
		static void Main(string[] args)
		{
			var pathname = args[0];
			var prog = new ConsoleProgress();
			var data = new FwData(pathname, prog);
			data.FixErrorsAndSave();
			if (prog.DotsWritten)
				Console.WriteLine();
			foreach (var err in data.Errors)
				Console.WriteLine(err);
		}
	}
}
