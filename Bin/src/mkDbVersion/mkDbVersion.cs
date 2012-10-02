/// --------------------------------------------------------------------------------------------
#region /// Copyright (c) 2003, SIL International. All Rights Reserved.
/// <copyright from='2003' to='2003' company='SIL International'>
///		Copyright (c) 2003, SIL International. All Rights Reserved.
///
///		Distributable under the terms of either the Common Public License or the
///		GNU Lesser General Public License, as specified in the LICENSING.txt file.
/// </copyright>
#endregion
///
/// File: mkDbVersion.cs
/// Responsibility: TE Team
/// Last reviewed:
///
/// <remarks>
/// Creates a file from a template and expands variables.
/// </remarks>
/// --------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace SIL.FieldWorks.Tools
{
	/// -----------------------------------------------------------------------------------------
	/// <summary>
	/// Creates a file from a template and expands variables.
	/// </summary>
	/// -----------------------------------------------------------------------------------------
	class mkDbVersion
	{
		static void ShowHelp()
		{
			Console.WriteLine("\nmkDbVersion. Wraps a file with a namespace to create");
			Console.WriteLine("\na C# file that contains the current database version.");
			Console.WriteLine("Copyright (c) 2003, SIL International. All Rights Reserved.\n");
			Console.WriteLine("Syntax: {0} infile outfile",
				Path.GetFileName(Application.ExecutablePath));
			Console.WriteLine("\nExample:\n{0} c:\\fw\\src\\appcore\\DbVersion.h c:\\fw\\src" +
				"\\Common\\Framework\\DbVersion.cs",
				Path.GetFileName(Application.ExecutablePath));
		}

		/// -----------------------------------------------------------------------------------------
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		/// -----------------------------------------------------------------------------------------
		[STAThread]
		static int Main(string[] args)
		{
			try
			{
				if (args.Length < 2 || args[0] == "/?" || args[0] == "-?")
				{
					ShowHelp();
					return 0;
				}

				string inFile = args[0];
				string outFile = args[1];

				string fileContents = string.Format("// This file is generated from {0}. Do NOT modify!\n",
					Path.GetFileName(inFile));

				StreamReader stream = new StreamReader(inFile);
				fileContents += "namespace SIL.FieldWorks.Common.Framework\n{\n/// <summary></summary>\npublic " +
					stream.ReadToEnd() + "\n}";
				stream.Close();

				StreamWriter outStream = new StreamWriter(outFile);
				outStream.Write(fileContents);
				outStream.Close();
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
